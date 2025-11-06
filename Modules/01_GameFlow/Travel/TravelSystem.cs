// НАЗНАЧЕНИЕ: Управляет перемещением "сценических" объектов (локаций) и фоновых монументов между точками за экраном и центром. Синхронизирует движение фонов.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: LoopingBackground, StageManager, DOTween.
// ПРИМЕЧАНИЕ: Является центральной системой для создания иллюзии путешествия между этапами.
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;
using System;
using System.Collections;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TravelSystem : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _targetPoint;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _poolParent;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _bridgeGameObjectA;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _bridgeGameObjectB;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _distance = 65f;
    [BoxGroup("SETTINGS"), SerializeField] private Ease _easeType = Ease.InOutSine;
    [BoxGroup("SETTINGS"), Tooltip("An object that is already at the TargetPoint at the start of the game."), SerializeField] private GameObject _initialStageObject;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<LoopingBackground> _backgroundsToSync = new List<LoopingBackground>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private GameObject _currentStageObject;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private GameObject _previousStageObject;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private GameObject _currentBridge;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private StageType _currentStageType;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _startPositionX;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _endPositionX;

    private static TravelSystem _instance;
    private Dictionary<StageType, List<GameObject>> _stagePool = new Dictionary<StageType, List<GameObject>>();
    private Coroutine _previewCoroutine;
    #endregion Поля

    #region Свойства
    public static TravelSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<TravelSystem>();
            }
            return _instance;
        }
    }

    public GameObject CurrentStageObject { get => _currentStageObject; }
    public Transform TargetPoint => _targetPoint;
    public float StartPositionX => _startPositionX;
    public float Distance => _distance;
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        _startPositionX = _targetPoint.transform.position.x + _distance;
        _endPositionX = _targetPoint.transform.position.x - _distance;

        if (_instance != null && _instance != this)
        {
            DebugUtils.LogInstanceAlreadyExists(this, _instance);
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (_targetPoint == null) DebugUtils.LogMissingReference(this, nameof(_targetPoint));
        if (_poolParent == null) DebugUtils.LogMissingReference(this, nameof(_poolParent));
        if (_bridgeGameObjectA == null) DebugUtils.LogMissingReference(this, nameof(_bridgeGameObjectA));
        if (_bridgeGameObjectB == null) DebugUtils.LogMissingReference(this, nameof(_bridgeGameObjectB));

        InitializePool();
        InitializeBridges();
        if (_initialStageObject != null)
        {
            _currentStageObject = _initialStageObject;
            _currentStageObject.transform.position = new Vector3(_targetPoint.position.x, _currentStageObject.transform.position.y, _initialStageObject.transform.position.z);
            _currentStageObject.SetActive(true);
            ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Initial stage object <color=lime>{0}</color> is set at TargetPoint.", _ColoredDebug, _currentStageObject.name);
        }
    }

    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged += OnStageChanged;
            ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> <color=green>Успешно подписался</color> на событие OnStageChanged.", _ColoredDebug);
        }
        else
        {
            Debug.LogError("[TravelSystem] StageManager не найден! Система не будет работать автоматически.");
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged -= OnStageChanged;
            ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> <color=orange>Отписался</color> от события OnStageChanged.", _ColoredDebug);
        }
        transform.DOKill();
        if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void RegisterSyncedBackground(LoopingBackground background)
    {
        if (background != null && !_backgroundsToSync.Contains(background))
        {
            _backgroundsToSync.Add(background);
            ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Фон <color=yellow>{0}</color> был автоматически зарегистрирован для синхронизации.", _ColoredDebug, background.name);
        }
    }
    #endregion Публичные методы

    #region Личные методы
    private void OnStageChanged(StageType newStageType)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Получено событие смены этапа на <color=yellow>{0}</color>.", _ColoredDebug, newStageType);
        MoveBridges();

        if (_currentStageObject != null)
        {
            _previousStageObject = _currentStageObject;
            ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Предыдущий объект <color=orange>{0}</color> начинает движение к EndPoint.", _ColoredDebug, _previousStageObject.name);
            _previousStageObject.transform.DOMoveX(_endPositionX, Settings.MovementDurationBetweenStages)
                .SetEase(_easeType)
                .OnComplete(() =>
                {
                    if (_previousStageObject != null)
                    {
                        BattleGridPropManager.Instance.ClearPropsFromParent(_previousStageObject.transform);
                        ColoredDebug.CLog(gameObject, "<color=green>TravelSystem:</color> Объект <color=orange>{0}</color> достиг EndPoint и деактивирован.", _ColoredDebug, _previousStageObject.name);
                        _previousStageObject.SetActive(false);
                        _previousStageObject = null;
                    }
                });
        }

        GameObject newObject = GetPooledObject(newStageType);
        if (newObject == null)
        {
            Debug.LogError($"[TravelSystem] Не удалось получить объект для этапа '{newStageType}' из пула!");
            return;
        }

        _currentStageObject = newObject;
        _currentStageType = newStageType;
        _currentStageObject.transform.position = new Vector3(_startPositionX, _currentStageObject.transform.position.y, _currentStageObject.transform.position.z);
        _currentStageObject.SetActive(true);

        ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Новый объект <color=lime>{0}</color> размещен на StartPoint.", _ColoredDebug, _currentStageObject.name);

        foreach (var bg in _backgroundsToSync)
        {
            if (bg != null) bg.SetSyncTarget(_currentStageObject.transform);
        }
        ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Начата синхронизация <color=yellow>{0}</color> фонов с объектом <color=lime>{1}</color>.", _ColoredDebug, _backgroundsToSync.Count, _currentStageObject.name);
        _currentStageObject.transform.DOMoveX(_targetPoint.position.x, Settings.MovementDurationBetweenStages)
            .SetEase(_easeType)
            .OnUpdate(() =>
            {
                foreach (var bg in _backgroundsToSync)
                {
                    if (bg != null) bg.UpdateSyncedMovement();
                }
            })
            .OnComplete(() =>
            {
                ColoredDebug.CLog(gameObject, "<color=green>TravelSystem:</color> Новый объект <color=lime>{0}</color> достиг TargetPoint.", _ColoredDebug, _currentStageObject.name);
                foreach (var bg in _backgroundsToSync)
                {
                    if (bg != null) bg.SetSyncTarget(null);
                }
                ColoredDebug.CLog(gameObject, "<color=green>TravelSystem:</color> Синхронизация фонов с <color=lime>{0}</color> завершена.", _ColoredDebug, _currentStageObject.name);
            });
    }

    private void InitializeBridges()
    {
        if (_bridgeGameObjectA != null && _bridgeGameObjectB != null)
        {
            _bridgeGameObjectA.transform.position = new Vector3(0, _bridgeGameObjectA.transform.position.y, _bridgeGameObjectA.transform.position.z);
            _bridgeGameObjectB.transform.position = new Vector3(_distance, _bridgeGameObjectB.transform.position.y, _bridgeGameObjectB.transform.position.z);
            _currentBridge = _bridgeGameObjectA;
            _bridgeGameObjectA.SetActive(true);
            _bridgeGameObjectB.SetActive(true);
            ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Монументы инициализированы. Текущий: <color=lime>{0}</color>.", _ColoredDebug, _currentBridge.name);
        }
    }

    private void MoveBridges()
    {
        if (_currentBridge == null) return;
        GameObject bridgeToMoveOut = _currentBridge;
        GameObject bridgeToMoveIn = (_currentBridge == _bridgeGameObjectA) ? _bridgeGameObjectB : _bridgeGameObjectA;
        ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Смена мостов. Входит: <color=lime>{0}</color>, Уходит: <color=orange>{1}</color>.", _ColoredDebug, bridgeToMoveIn.name, bridgeToMoveOut.name);

        bridgeToMoveOut.transform.DOMoveX(_distance * (-1), Settings.MovementDurationBetweenStages).SetEase(_easeType);
        bridgeToMoveIn.transform.position = new Vector3(_distance, bridgeToMoveIn.transform.position.y, bridgeToMoveIn.transform.position.z);
        bridgeToMoveIn.transform.DOMoveX(0, Settings.MovementDurationBetweenStages).SetEase(_easeType);

        _currentBridge = bridgeToMoveIn;
    }

    private bool IsBattleStage(StageType stageType)
    {
        switch (stageType)
        {
            case StageType.Battle:
            case StageType.Horde:
            case StageType.MiniBoss:
            case StageType.BossFight:
            case StageType.HighLevelBattle:
            case StageType.MixedBattle:
            case StageType.DoubleMiniBoss:
            case StageType.TripleMiniBoss:
                return true;
            default:
                return false;
        }
    }

    private void InitializePool()
    {
        ColoredDebug.CLog(gameObject, "<color=yellow>TravelSystem:</color> Инициализация пула объектов...", _ColoredDebug);
        _stagePool.Clear();

        foreach (Transform child in _poolParent)
        {
            if (TryParseStageType(child.name, out StageType type))
            {
                if (!_stagePool.ContainsKey(type))
                {
                    _stagePool[type] = new List<GameObject>();
                }
                _stagePool[type].Add(child.gameObject);
                child.gameObject.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=grey>TravelSystem:</color> Добавлен объект <color=white>{0}</color> в пул для типа <color=yellow>{1}</color>.", _ColoredDebug, child.name, type);
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=red>TravelSystem:</color> Не удалось распознать StageType из имени объекта <color=orange>{0}</color>.", _ColoredDebug, child.name);
            }
        }
        ColoredDebug.CLog(gameObject, "<color=green>TravelSystem:</color> Инициализация пула завершена. Найдено <color=yellow>{0}</color> типов объектов.", _ColoredDebug, _stagePool.Count);
    }

    private GameObject GetPooledObject(StageType type)
    {
        GameObject availableObject = _stagePool[type].FirstOrDefault(obj => !obj.activeInHierarchy && obj != _previousStageObject);
        if (availableObject != null)
        {
            ColoredDebug.CLog(gameObject, "<color=green>TravelSystem:</color> Найден и взят из пула объект <color=lime>{0}</color> для типа <color=yellow>{1}</color>.", _ColoredDebug, availableObject.name, type);
            return availableObject;
        }
        Debug.LogError($"[TravelSystem] Не найдено свободных объектов в пуле для типа '{type}'!");
        return null;
    }


    private bool TryParseStageType(string objectName, out StageType type)
    {
        type = StageType.Unknown;
        string typeName = objectName.Split(' ')[0];
        try
        {
            type = (StageType)System.Enum.Parse(typeof(StageType), typeName, true);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    #endregion Личные методы

    #region Инструменты Разработчика
#if UNITY_EDITOR
    [BoxGroup("DEBUG"), Button("Превью анимации", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
    private void DebugPreviewAnimation()
    {
        StopDebugPreviewAnimation();
        _previewCoroutine = StartCoroutine(PreviewRoutine());
        ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> <color=lime>Запуск симуляции смены этапов...</color>", _ColoredDebug);
    }

    private IEnumerator PreviewRoutine()
    {
        var stageTypes = System.Enum.GetValues(typeof(StageType)).Cast<StageType>().ToList();
        stageTypes.Remove(StageType.Unknown);
        stageTypes.Remove(StageType.GameOver);

        if (stageTypes.Count == 0)
        {
            Debug.LogError("[TravelSystem] Нет доступных этапов для симуляции!");
            yield break;
        }

        InitializeBridges();
        int currentIndex = 0;
        while (true)
        {
            StageType nextStage = stageTypes[currentIndex];
            OnStageChanged(nextStage);

            yield return new WaitForSeconds(Settings.MovementDurationBetweenStages + 0.1f);
            currentIndex = (currentIndex + 1) % stageTypes.Count;
        }
    }

    [BoxGroup("DEBUG"), Button("Стоп превью", ButtonSizes.Large), GUIColor(1f, 0.4f, 0.4f)]
    private void StopDebugPreviewAnimation()
    {
        if (_previewCoroutine != null)
        {
            StopCoroutine(_previewCoroutine);
            _previewCoroutine = null;
            ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> <color=orange>Симуляция остановлена.</color>", _ColoredDebug);
        }

        if (_currentStageObject != null) _currentStageObject.transform.DOKill();
        if (_previousStageObject != null) _previousStageObject.transform.DOKill();
        if (_bridgeGameObjectA != null) _bridgeGameObjectA.transform.DOKill();
        if (_bridgeGameObjectB != null) _bridgeGameObjectB.transform.DOKill();
    }

    [BoxGroup("DEBUG"), Button("Сгенерировать Объекты для Пула", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 1f)]
    private void GeneratePoolObjects()
    {
        ColoredDebug.CLog(gameObject, "<color=yellow>TravelSystem:</color> <color=orange>Запуск генерации объектов для пула...</color>", _ColoredDebug);
        if (_poolParent == null)
        {
            Debug.LogError("[TravelSystem] _poolParent не назначен! Генерация невозможна.");
            return;
        }

        for (int i = _poolParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(_poolParent.GetChild(i).gameObject);
        }
        ColoredDebug.CLog(gameObject, "<color=cyan>TravelSystem:</color> Старые объекты в пуле удалены.", _ColoredDebug);

        var stageTypes = System.Enum.GetValues(typeof(StageType)).Cast<StageType>();
        foreach (var type in stageTypes)
        {
            if (type == StageType.Unknown || type == StageType.GameOver)
            {
                continue;
            }

            for (int i = 1; i <= 2; i++)
            {
                GameObject newObj = new GameObject($"{type} {i}");
                newObj.transform.SetParent(_poolParent);
                newObj.AddComponent<SpriteRenderer>();
                newObj.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=grey>TravelSystem:</color> Создан объект <color=white>{0}</color>.", _ColoredDebug, newObj.name);
            }
        }

        InitializePool();
        ColoredDebug.CLog(gameObject, "<color=green>TravelSystem:</color> Генерация объектов для пула завершена.", _ColoredDebug);

        EditorUtility.SetDirty(this);
    }
#endif
    #endregion Инструменты Разработчика
}