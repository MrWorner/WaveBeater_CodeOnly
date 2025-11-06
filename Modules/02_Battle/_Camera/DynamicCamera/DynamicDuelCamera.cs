using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class DynamicDuelCamera : MonoBehaviour
{
    public enum CameraMode { Travel, Combat, HeroFocus, GridFocus }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _leftSide;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _rightSide;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _upperSide;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _bottomSide;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Camera _cam;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _leftSide_Hero;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _rightSide_FarEnemy;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, ReadOnly] private Transform _heroFocusPoint;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, MinValue(0.1f)] private float _minSize = 5f;
    [BoxGroup("SETTINGS"), SerializeField] private float _maxSize = 12f;
    [BoxGroup("SETTINGS"), SerializeField] private float _sidePadding = 2f;
    [BoxGroup("SETTINGS"), SerializeField] private float _topBottomPadding = 1.5f;
    [BoxGroup("SETTINGS"), SerializeField] private float _heroCameraOffset = 2f;
    [BoxGroup("SETTINGS"), SerializeField] private float _enemyCameraOffset = 2f;
    [BoxGroup("SETTINGS"), SerializeField] private float _verticalOffset = 1f;
    [BoxGroup("SETTINGS"), SerializeField] private float _heroFocusSize = 3.5f;
    [BoxGroup("SETTINGS"), SerializeField] private float _gridFocusSidePadding = 8f;

    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector3 _velocity = Vector3.zero;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentVelocitySize = 0f;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private CameraMode _currentMode;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private BattleUnit _farEnemy;
    private static DynamicDuelCamera _instance;
    #endregion

    #region Свойства
    public static DynamicDuelCamera Instance => _instance;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (_leftSide == null) DebugUtils.LogMissingReference(this, nameof(_leftSide));
        if (_rightSide == null) DebugUtils.LogMissingReference(this, nameof(_rightSide));
        if (_upperSide == null) DebugUtils.LogMissingReference(this, nameof(_upperSide));
        if (_bottomSide == null) DebugUtils.LogMissingReference(this, nameof(_bottomSide));
        if (_cam == null) DebugUtils.LogMissingReference(this, nameof(_cam));
        if (_leftSide_Hero == null) DebugUtils.LogMissingReference(this, nameof(_leftSide_Hero));
        if (_rightSide_FarEnemy == null) DebugUtils.LogMissingReference(this, nameof(_rightSide_FarEnemy));
    }


    private void Start()
    {
        if (BattleUnit.Hero != null)
        {
            _heroFocusPoint = BattleUnit.Hero.DamagePoint;
            ColoredDebug.CLog(gameObject, "<color=cyan>DynamicDuelCamera:</color> Ссылка на героя (<color=yellow>{0}</color>) для фокусировки получена.", _ColoredDebug, _heroFocusPoint.name);
        }
        if (_heroFocusPoint == null) DebugUtils.LogMissingReference(this, nameof(_heroFocusPoint));

    }


    private void LateUpdate()
    {
        if (_cam == null || _heroFocusPoint == null) return;

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        float finalTargetSize;

        switch (_currentMode)
        {
            case CameraMode.HeroFocus:
                UpdateBoundsWithTransform(_heroFocusPoint, ref minX, ref maxX, ref minY, ref maxY);
                break;
            case CameraMode.Travel:
                UpdateBoundsWithTransform(_leftSide, ref minX, ref maxX, ref minY, ref maxY);
                UpdateBoundsWithTransform(_rightSide, ref minX, ref maxX, ref minY, ref maxY);
                UpdateBoundsWithTransform(_upperSide, ref minX, ref maxX, ref minY, ref maxY);
                UpdateBoundsWithTransform(_bottomSide, ref minX, ref maxX, ref minY, ref maxY);
                break;
            case CameraMode.GridFocus:
                UpdateBoundsWithTransform(_heroFocusPoint, ref minX, ref maxX, ref minY, ref maxY);
                if (BattleGrid.Instance != null)
                {
                    foreach (var cell in BattleGrid.Instance.AllCells)
                    {
                        if (cell != null) UpdateBoundsWithPosition(cell.WorldPosition, ref minX, ref maxX, ref minY, ref maxY);
                    }
                }
                break;
            case CameraMode.Combat:
                UpdateFarEnemy();
                if (BattleUnit.Hero != null)
                {
                    Vector3 heroPos = BattleUnit.Hero.transform.position;
                    _leftSide_Hero.position = new Vector3(heroPos.x - _heroCameraOffset, heroPos.y, heroPos.z);
                    UpdateBoundsWithTransform(_leftSide_Hero, ref minX, ref maxX, ref minY, ref maxY);
                }
                if (_farEnemy != null)
                {
                    Vector3 enemyPos = _farEnemy.transform.position;
                    _rightSide_FarEnemy.position = new Vector3(enemyPos.x + _enemyCameraOffset, enemyPos.y, enemyPos.z);
                    UpdateBoundsWithTransform(_rightSide_FarEnemy, ref minX, ref maxX, ref minY, ref maxY);
                }
                else if (BattleUnit.Hero != null)
                {
                    UpdateBoundsWithTransform(BattleUnit.Hero.transform, ref minX, ref maxX, ref minY, ref maxY);
                }

                if (ObjectPoolProjectiles.Instance != null)
                {
                    foreach (var projectile in ObjectPoolProjectiles.Instance.ActiveProjectiles)
                    {
                        if (projectile != null && projectile.gameObject.activeInHierarchy)
                            UpdateBoundsWithTransform(projectile.transform, ref minX, ref maxX, ref minY, ref maxY);
                    }
                }
                break;
        }

        if (minX == float.MaxValue) return;

        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, _cam.transform.position.z);
        if (_currentMode == CameraMode.Combat) center.y += _verticalOffset;

        float currentSidePadding = _sidePadding;
        if (_currentMode == CameraMode.GridFocus)
        {
            currentSidePadding = _gridFocusSidePadding;
            //ColoredDebug.CLog(gameObject, "<color=#87CEEB>DynamicDuelCamera:</color> Используется специальный отступ для сетки: <color=yellow>{0}</color>.", _ColoredDebug, currentSidePadding);
        }

        float distanceX = (maxX - minX) + currentSidePadding;
        float distanceY = (maxY - minY) + _topBottomPadding;
        float sizeX = distanceX / _cam.aspect / 2f;
        float sizeY = distanceY / 2f;
        float targetSize = Mathf.Max(sizeX, sizeY, _minSize);

        if (_currentMode == CameraMode.HeroFocus)
        {
            targetSize = _heroFocusSize;
        }

        finalTargetSize = Mathf.Min(targetSize, _maxSize);
        _cam.transform.position = Vector3.SmoothDamp(_cam.transform.position, center, ref _velocity, Settings.DynamicDuelCamera_smoothTime);
        _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, finalTargetSize, ref _currentVelocitySize, Settings.DynamicDuelCamera_smoothTime);
    }
    #endregion

    #region Публичные методы
    public void SwitchToCombatMode()
    {
        ColoredDebug.CLog(gameObject, "<color=#FF6347>DynamicDuelCamera:</color> Переключение в режим <color=orange>Combat</color>.", _ColoredDebug);
        _currentMode = CameraMode.Combat;
    }

    public void SwitchToTravelMode()
    {
        ColoredDebug.CLog(gameObject, "<color=#FFD700>DynamicDuelCamera:</color> Переключение в режим <color=yellow>Travel</color>.", _ColoredDebug);
        _currentMode = CameraMode.Travel;
    }

    public void SwitchToHeroFocusMode()
    {
        ColoredDebug.CLog(gameObject, "<color=#90EE90>DynamicDuelCamera:</color> Переключение в режим <color=lime>HeroFocus</color>.", _ColoredDebug);
        _currentMode = CameraMode.HeroFocus;
    }

    public void SwitchToGridFocusMode()
    {
        ColoredDebug.CLog(gameObject, "<color=#87CEEB>DynamicDuelCamera:</color> Переключение в режим <color=cyan>GridFocus</color>.", _ColoredDebug);
        _currentMode = CameraMode.GridFocus;
    }

    public void SetCameraInstant()
    {
        _cam.DOKill();
        transform.DOKill();
        LateUpdate();
        _cam.transform.position = new Vector3(_cam.transform.position.x, _cam.transform.position.y, -10);
        _velocity = Vector3.zero;
        _currentVelocitySize = 0f;
        ColoredDebug.CLog(gameObject, "<color=cyan>DynamicDuelCamera:</color> Камера мгновенно установлена в целевую позицию и размер.", _ColoredDebug);
    }
    #endregion

    #region Личные методы
    private void UpdateBoundsWithTransform(Transform t, ref float minX, ref float maxX, ref float minY, ref float maxY)
    {
        if (t == null) return;
        UpdateBoundsWithPosition(t.position, ref minX, ref maxX, ref minY, ref maxY);
    }

    private void UpdateBoundsWithPosition(Vector3 pos, ref float minX, ref float maxX, ref float minY, ref float maxY)
    {
        minX = Mathf.Min(minX, pos.x);
        maxX = Mathf.Max(maxX, pos.x);
        minY = Mathf.Min(minY, pos.y);
        maxY = Mathf.Max(maxY, pos.y);
    }

    private void UpdateFarEnemy()
    {
        if (EnemyManager.Instance == null || BattleUnit.Hero == null)
        {
            _farEnemy = null;
            return;
        }

        var enemies = EnemyManager.Instance.Enemies;
        if (enemies == null || enemies.Count == 0)
        {
            _farEnemy = null;
            return;
        }

        _farEnemy = enemies
            .Where(e => e != null && e.gameObject.activeInHierarchy)
            .OrderByDescending(e => Vector3.Distance(e.transform.position, BattleUnit.Hero.transform.position))
            .FirstOrDefault();
    }
    #endregion
}

