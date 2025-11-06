// НАЗНАЧЕНИЕ: Управляет логикой и последовательностью этапов на уровне. Оркестрирует работу дочерних обработчиков и систем генерации.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: StageManager, ScreenFader, PlayerTravelController, BattleStageHandler, ShopStageHandler, ArenaManager.
// ПРИМЕЧАНИЕ: Является "дирижером" для всего игрового процесса на уровне, делегируя конкретные задачи специализированным компонентам.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class StageFlowManager : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ScreenFader _screenFader;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private PlayerTravelController _playerTravel;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleStageHandler _battleHandler;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ShopStageHandler _shopHandler;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameOverHandler _gameOverHandler;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ArenaManager _arenaManager;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private bool _DEPRECATED_skipMovingToNextStage = false;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<StageType> _stages;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentIndex = -1;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private StageManager _stageManager;
    #endregion Поля

    #region Свойства
    /// <summary>
    /// Возвращает текущий индекс этапа в последовательности.
    /// </summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>
    /// Возвращает тип текущего этапа.
    /// </summary>
    public StageType CurrentStageType
    {
        get
        {
            if (_stages == null || _currentIndex < 0 || _currentIndex >= _stages.Count) return StageType.Unknown;
            return _stages[_currentIndex];
        }
    }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_screenFader == null) DebugUtils.LogMissingReference(this, nameof(_screenFader));
        if (_playerTravel == null) DebugUtils.LogMissingReference(this, nameof(_playerTravel));
        if (_battleHandler == null) DebugUtils.LogMissingReference(this, nameof(_battleHandler));
        if (_shopHandler == null) DebugUtils.LogMissingReference(this, nameof(_shopHandler));
        if (_gameOverHandler == null) DebugUtils.LogMissingReference(this, nameof(_gameOverHandler));
        if (_arenaManager == null) DebugUtils.LogMissingReference(this, nameof(_arenaManager));
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Инициализирует менеджер потока.
    /// </summary>
    /// <param name="stageManager">Ссылка на главный StageManager.</param>
    public void Initialize(StageManager stageManager)
    {
        _stageManager = stageManager;
    }

    /// <summary>
    /// Инициализирует новую последовательность этапов.
    /// </summary>
    /// <param name="stages">Список типов этапов.</param>
    public void Init(List<StageType> stages)
    {
        _stages = stages;
        _currentIndex = -1;
        ColoredDebug.CLog(gameObject, "<color=cyan>StageFlowManager:</color> Инициализирован новой последовательностью из <color=yellow>{0}</color> этапов.", _ColoredDebug, stages.Count);
    }

    /// <summary>
    /// Переходит к следующему этапу в списке.
    /// </summary>
    /// <returns>True, если следующий этап существует, иначе false.</returns>
    public bool MoveNext()
    {
        _currentIndex++;
        return _stages != null && _currentIndex < _stages.Count;
    }

    /// <summary>
    /// Возвращает отладочную информацию о текущем состоянии.
    /// </summary>
    public string GetStageInfo()
    {
        if (_stages == null || _stages.Count == 0) return "Empty";
        if (_currentIndex < 0) return "NotStarted";
        if (_currentIndex >= _stages.Count) return "Completed";
        return $"{_stages[_currentIndex]} ({_currentIndex + 1}/{_stages.Count})";
    }

    /// <summary>
    /// Запускает корутину игрового интро.
    /// </summary>
    public void StartIntro()
    {
        StartCoroutine(IntroCoroutine());
    }

    /// <summary>
    /// Мгновенно скрывает экран загрузки.
    /// </summary>
    public void HideLoadingScreenInstantly()
    {
        _screenFader.HideLoadingScreen(0f);
    }

    [BoxGroup("DEBUG"), Button("Сгенерировать следующий вариант поля боя", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
    public void GenerateNextBattleStageLayout()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StageFlowManager:</color> Запрос на генерацию новой арены к ArenaManager.", _ColoredDebug);
        _arenaManager.GenerateArena();
    }


    /// <summary>
    /// Запускает обработку текущего этапа, включая перемещение игрока.
    /// </summary>
    public void ProcessCurrentStage()
    {
        if (IsBattleStage(CurrentStageType))
        {
            GenerateNextBattleStageLayout();
        }

        if (_DEPRECATED_skipMovingToNextStage)
        {
            if (_stageManager.IsDebugSkipStages)
            {
                ColoredDebug.CLog(gameObject, "<color=yellow>StageFlowManager:</color> <color=red>DEBUG</color> режим — этап <color=lime>{0}</color> пропущен.", _ColoredDebug, CurrentStageType);
                StartCoroutine(DelayedNextStageCoroutine());
                return;
            }
            StartCoroutine(StartStageWithDelayCoroutine(CurrentStageType));
        }
        else
        {
            _playerTravel.StartTravel(_currentIndex, () =>
            {
                if (_stageManager.IsDebugSkipStages)
                {
                    ColoredDebug.CLog(gameObject, "<color=yellow>StageFlowManager:</color> <color=red>DEBUG</color> режим — этап <color=lime>{0}</color> пропущен.", _ColoredDebug, CurrentStageType);
                    StartCoroutine(DelayedNextStageCoroutine());
                    return;
                }
                StartCoroutine(StartStageWithDelayCoroutine(CurrentStageType));
            });
        }
    }


    /// <summary>
    /// Запускает корутину для перехода к следующему этапу с задержкой.
    /// </summary>
    public void StartDelayedNextStage()
    {
        StartCoroutine(DelayedNextStageCoroutine());
    }
    #endregion Публичные методы

    #region Личные методы
    private IEnumerator IntroCoroutine()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StageFlowManager:</color> Запуск интро. Длительность: <color=orange>{0}</color> сек. Ожидание: <color=orange>{1}</color> сек.", _ColoredDebug, Settings.FadeDuration, Settings.IntroWait);
        yield return new WaitForSeconds(Settings.FadeDuration);
        _screenFader.HideLoadingScreen();
        yield return new WaitForSeconds(Settings.IntroWait);
        ColoredDebug.CLog(gameObject, "<color=cyan>StageFlowManager:</color> Интро завершено. Запускаю первый этап.", _ColoredDebug);
        _stageManager.NextStage();
    }

    private IEnumerator StartStageWithDelayCoroutine(StageType stage)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>StageFlowManager:</color> Задержка <color=orange>{0}</color> сек. перед началом этапа <color=lime>{1}</color>.", _ColoredDebug, Settings.StageStartDelay, stage);
        yield return new WaitForSeconds(Settings.StageStartDelay);
        HandleStage(stage);
    }

    private IEnumerator DelayedNextStageCoroutine()
    {
        ColoredDebug.CLog(gameObject, "<color=orange>StageFlowManager:</color> Переход на следующий этап через <color=cyan>{0}</color> сек.", _ColoredDebug, Settings.WaitForNewStage);
        yield return new WaitForSeconds(Settings.WaitForNewStage);
        _stageManager.NextStage();
    }

    private void HandleStage(StageType stage)
    {
        LevelData currentLevelData = _stageManager.CurrentLevelData;
        if (currentLevelData == null) return;

        switch (stage)
        {
            case StageType.Battle:
            case StageType.Horde:
            case StageType.MiniBoss:
            case StageType.HighLevelBattle:
            case StageType.MixedBattle:
            case StageType.DoubleMiniBoss:
            case StageType.TripleMiniBoss:
            case StageType.BossFight:
                ColoredDebug.CLog(gameObject, "<color=cyan>StageFlowManager:</color> Обработка этапа <color=lime>" + stage + "</color>. Запускаю <color=orange>битву</color>.", _ColoredDebug);
                _battleHandler.StartBattle(_stageManager.OnStageCompleted, currentLevelData, stage);
                break;
            case StageType.Shop:
                _shopHandler.StartShop(_stageManager.OnStageCompleted, UpgradeCardDataSO.CardTCategory.Shop);
                break;
            case StageType.Hospital:
                _shopHandler.StartShop(_stageManager.OnStageCompleted, UpgradeCardDataSO.CardTCategory.Hospital);
                break;
            case StageType.Treasure:
                _shopHandler.StartShop(_stageManager.OnStageCompleted, UpgradeCardDataSO.CardTCategory.Treasure);
                break;
            case StageType.Award:
                _shopHandler.StartShop(_stageManager.OnStageCompleted, UpgradeCardDataSO.CardTCategory.Award);
                break;
            case StageType.GameOver:
                _gameOverHandler.ShowGameOverScreen();
                break;
            default:
                _stageManager.OnStageCompleted();
                break;
        }
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
    #endregion Личные методы
}