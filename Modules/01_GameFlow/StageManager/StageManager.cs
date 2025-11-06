// НАЗНАЧЕНИЕ: Является центральным фасадом для управления игровым процессом на уровне этапов. Делегирует задачи по генерации, выполнению и отображению UI специализированным компонентам.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: StageFlowManager, StageSequenceGenerator, StageUIController, LevelManager.
// ПРИМЕЧАНИЕ: Этот класс оркестрирует взаимодействие между основными системами, не выполняя сложной логики самостоятельно.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public enum StageType { Battle, Shop, Horde, MiniBoss, Unknown, Hospital, Treasure, BossFight, Award, GameOver, HighLevelBattle, MixedBattle, DoubleMiniBoss, TripleMiniBoss }

public class StageManager : MonoBehaviour
{

    public event UnityAction<StageType> OnStageChanged;
    public event UnityAction<BackgroundVariant> OnBackgroundVariantChanged;

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private LevelManager _levelManager;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StageFlowManager _flowManager;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StageSequenceGenerator _sequenceGenerator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StageUIController _uiController;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private bool _skipIntroOnStart = false;
    [BoxGroup("SETTINGS"), SerializeField] private bool _freezeTheGame = false;
    [BoxGroup("SETTINGS"), SerializeField] private bool _debugSkipStages = false;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<StageType> _currentStages;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private static StageManager _instance;
    #endregion Поля

    #region Свойства
    /// <summary>
    /// Предоставляет глобальный доступ к экземпляру StageManager.
    /// </summary>
    public static StageManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<StageManager>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Возвращает текущий тип этапа.
    /// </summary>
    public StageType CurrentStageType => _flowManager.CurrentStageType;

    /// <summary>
    /// Возвращает текущий объект LevelData.
    /// </summary>
    public LevelData CurrentLevelData => _levelManager.GetCurrentLevelData();

    /// <summary>
    /// Возвращает, включен ли режим пропуска этапов для отладки.
    /// </summary>
    public bool IsDebugSkipStages => _debugSkipStages;
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this) { DebugUtils.LogInstanceAlreadyExists(this, _instance); Destroy(gameObject); return; } else _instance = this;
        if (_levelManager == null) DebugUtils.LogMissingReference(this, nameof(_levelManager));
        if (_flowManager == null) DebugUtils.LogMissingReference(this, nameof(_flowManager));
        if (_sequenceGenerator == null) DebugUtils.LogMissingReference(this, nameof(_sequenceGenerator));
        if (_uiController == null) DebugUtils.LogMissingReference(this, nameof(_uiController));
    }

    private void Start()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StageManager:</color> Start. Инициализация систем...", _ColoredDebug);
        _flowManager.Initialize(this);

        GenerateAndInitializeStages();

        MusicManager.Instance.PlayWanderingAroundMusic();

        var currentData = _levelManager.GetCurrentLevelData();
        OnBackgroundVariantChanged?.Invoke(currentData.backgroundVariant);
        ColoredDebug.CLog(gameObject, "<color=cyan>StageManager:</color> Вызвано начальное событие <color=yellow>OnBackgroundVariantChanged</color> с вариантом <color=lime>{0}</color>.", _ColoredDebug, currentData.backgroundVariant);

        ColoredDebug.CLog(gameObject, "<color=cyan>StageManager:</color> Устанавливаю начальный режим камеры на <color=yellow>HeroFocus</color>.", _ColoredDebug);
        DynamicDuelCamera.Instance.SwitchToHeroFocusMode();
        DynamicDuelCamera.Instance.SetCameraInstant();

        if (_freezeTheGame)
        {
            _flowManager.HideLoadingScreenInstantly();
            return;
        }

        if (_skipIntroOnStart)
        {
            _flowManager.HideLoadingScreenInstantly();
            ColoredDebug.CLog(gameObject, "<color=yellow>StageManager:</color> <color=orange>Интро</color> пропущено.", _ColoredDebug);
            NextStage();
        }
        else
        {
            _flowManager.StartIntro();
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Запускает переход к следующему этапу в последовательности.
    /// </summary>
    public void NextStage()
    {
        bool hasNext = _flowManager.MoveNext();
        if (!hasNext)
        {
            OnAllStagesCompleted();
            return;
        }

        OnStageChanged?.Invoke(_flowManager.CurrentStageType);
        _uiController.UpdateUI(_levelManager.CurrentLevelIndex + 1, _flowManager.CurrentIndex + 1, _currentStages.Count);
        ColoredDebug.CLog(gameObject, "<color=cyan>StageManager:</color> Переход к этапу <color=lime>{0}</color> | Stage: <color=orange>{1}</color>.", _ColoredDebug, _flowManager.CurrentStageType, _flowManager.GetStageInfo());

        _flowManager.ProcessCurrentStage();
    }

    /// <summary>
    /// Запускает новый "матч" (следующий уровень в прогрессии), генерируя новую последовательность этапов.
    /// </summary>
    public void StartNewMatch()
    {
        MusicManager.Instance.PlayWanderingAroundMusic(true);
        _levelManager.MoveToNextLevel();
        ColoredDebug.CLog(gameObject, "<color=cyan>StageManager:</color> Начат новый матч. <color=lime>Уровень: {0}</color>.", _ColoredDebug, _levelManager.CurrentLevelIndex + 1);

        GenerateAndInitializeStages();

        var currentData = _levelManager.GetCurrentLevelData();
        OnBackgroundVariantChanged?.Invoke(currentData.backgroundVariant);
        ColoredDebug.CLog(gameObject, "<color=cyan>StageManager:</color> Вызвано событие <color=yellow>OnBackgroundVariantChanged</color> с вариантом <color=lime>{0}</color>.", _ColoredDebug, currentData.backgroundVariant);

        NextStage();
    }

    /// <summary>
    /// Обрабатывает завершение текущего этапа, запуская переход к следующему.
    /// </summary>
    public void OnStageCompleted()
    {
        BattleUnit.Hero.Health.AutoHeal();
        BattleUnit.Hero.Abilities.RechargeShieldAfterStage();
        ColoredDebug.CLog(gameObject, "<color=lime>StageManager:</color> Этап <color=orange>{0}</color> завершен. Запускаю переход на следующий.", _ColoredDebug, _flowManager.CurrentStageType);
        _flowManager.StartDelayedNextStage();
    }
    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Обрабатывает завершение всех этапов на уровне, запуская новый матч.
    /// </summary>
    private void OnAllStagesCompleted()
    {
        ColoredDebug.CLog(gameObject, "<color=lime>StageManager:</color> <color=orange>Все этапы пройдены</color>! Начинаю новый матч.", _ColoredDebug);
        StartNewMatch();
    }

    /// <summary>
    /// Генерирует и инициализирует новую последовательность этапов.
    /// </summary>
    private void GenerateAndInitializeStages()
    {
        _currentStages = _sequenceGenerator.GenerateStages(_levelManager.CurrentLevelIndex, _levelManager.IsLastLevel());
        _flowManager.Init(_currentStages);
        _uiController.Initialize(_currentStages);
        _uiController.UpdateUI(_levelManager.CurrentLevelIndex + 1, 0, _currentStages.Count);
    }
    #endregion Личные методы
}