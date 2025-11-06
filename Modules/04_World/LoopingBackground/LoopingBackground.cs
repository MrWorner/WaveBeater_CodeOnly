using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class LoopingBackground : SerializedMonoBehaviour
{
    #region Enums
    public enum MovementMode { Independent, Synced }
    #endregion Enums

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private MovementMode _movementMode = MovementMode.Independent;
    [BoxGroup("SETTINGS"), SerializeField, ShowIf("_movementMode", MovementMode.Independent)] private float _speed = 1f;
    [BoxGroup("SETTINGS"), SerializeField, Tooltip("Множитель скорости относительно цели. <1 = медленнее, >1 = быстрее."), ShowIf("_movementMode", MovementMode.Synced)] private float _speedMultiplier = 1f;
    [InfoBox("Значение > 1 заставит фон телепортироваться раньше. Например, 2 = в два раза раньше.")]
    [BoxGroup("SETTINGS"), SerializeField, MinValue(0.1f)] private float _loopEarlyTriggerFactor = 1.5f;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _localLoopThresholdX;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _repositionOffset;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Transform[] _backgroundParts;
    [BoxGroup("DEBUG"), ShowIf("_movementMode", MovementMode.Synced), SerializeField, ReadOnly] private Transform _syncTarget;

    private Dictionary<Transform, float> _partLocalHalfWidths;
    private float _initialWorldLoopThresholdX;
    private Vector3 _lastSyncTargetPosition;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_movementMode == MovementMode.Synced)
        {
            if (TravelSystem.Instance != null)
            {
                TravelSystem.Instance.RegisterSyncedBackground(this);
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=red>LoopingBackground:</color> Не удалось найти TravelSystem на сцене для автоматической регистрации!", _ColoredDebug);
            }
        }

        InitializeBackground();
    }

    private void LateUpdate()
    {
        if (_backgroundParts == null || _backgroundParts.Length == 0) return;

        if (_movementMode == MovementMode.Independent)
        {
            float scaledSpeed = _speed * transform.lossyScale.x;
            transform.position += Vector3.left * (scaledSpeed * Time.deltaTime);
        }

        foreach (Transform part in _backgroundParts)
        {
            float partWorldHalfWidth = _partLocalHalfWidths[part] * transform.lossyScale.x;
            if (part.position.x + partWorldHalfWidth < _initialWorldLoopThresholdX)
            {
                part.localPosition += new Vector3(_repositionOffset, 0f, 0f);
                ///ColoredDebug.CLog(gameObject, "<color=cyan>LoopingBackground:</color> Перемещение части <color=yellow>{0}</color>. Новая локальная позиция X: <color=yellow>{1}</color>.", _ColoredDebug, part.name, part.localPosition.x);
            }
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void SetSyncTarget(Transform target)
    {
        if (_movementMode != MovementMode.Synced)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>LoopingBackground:</color> Попытка синхронизации в режиме <color=yellow>{0}</color> проигнорирована.", _ColoredDebug, _movementMode);
            return;
        }

        _syncTarget = target;
        if (_syncTarget != null)
        {
            _lastSyncTargetPosition = _syncTarget.position;
            ColoredDebug.CLog(gameObject, "<color=cyan>LoopingBackground:</color> Начата синхронизация с <color=yellow>{0}</color>.", _ColoredDebug, target.name);
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>LoopingBackground:</color> Синхронизация остановлена.", _ColoredDebug);
        }
    }

    public void UpdateSyncedMovement()
    {
        if (_movementMode != MovementMode.Synced || _syncTarget == null) return;

        Vector3 currentTargetPos = _syncTarget.position;
        Vector3 targetDelta = currentTargetPos - _lastSyncTargetPosition;

        if (Mathf.Abs(targetDelta.x) > 0.0001f)
        {
            transform.position += new Vector3(targetDelta.x * _speedMultiplier, 0f, 0f);
        }

        _lastSyncTargetPosition = currentTargetPos;
    }
    #endregion Публичные методы

    #region Личные методы
    [Button]
    private void InitializeBackground()
    {
        int childCount = transform.childCount;
        if (childCount == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>LoopingBackground:</color> Нет дочерних объектов для инициализации.", _ColoredDebug);
            return;
        }

        _backgroundParts = new Transform[childCount];
        _partLocalHalfWidths = new Dictionary<Transform, float>();
        float totalLocalWidth = 0f;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            _backgroundParts[i] = child;

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
            {
                DebugUtils.LogMissingReference(this, $"SpriteRenderer/Sprite на дочернем объекте {child.name}");
                continue;
            }

            float spriteLocalWidth = (sr.sprite.rect.width / sr.sprite.pixelsPerUnit) * child.transform.localScale.x;
            totalLocalWidth += spriteLocalWidth;
            _partLocalHalfWidths[child] = spriteLocalWidth / 2f;
        }

        _repositionOffset = totalLocalWidth;
        float startLocalX = -totalLocalWidth / 2f;
        _localLoopThresholdX = startLocalX;

        float currentLocalX = startLocalX;
        foreach (Transform part in _backgroundParts)
        {
            if (!_partLocalHalfWidths.ContainsKey(part)) continue;

            float partHalfWidth = _partLocalHalfWidths[part];
            float partCenterLocalX = currentLocalX + partHalfWidth;
            part.localPosition = new Vector3(partCenterLocalX, part.localPosition.y, part.localPosition.z);
            currentLocalX += partHalfWidth * 2f;
        }

        float effectiveLocalThreshold = _localLoopThresholdX / _loopEarlyTriggerFactor;
        _initialWorldLoopThresholdX = transform.position.x + (effectiveLocalThreshold * transform.lossyScale.x);
        ColoredDebug.CLog(gameObject, "<color=cyan>LoopingBackground:</color> Авто-настройка завершена. Режим: <color=lime>{0}</color>. Начальный мировой порог: <color=lime>{1}</color>.", _ColoredDebug, _movementMode, _initialWorldLoopThresholdX);
    }
    #endregion Личные методы
}