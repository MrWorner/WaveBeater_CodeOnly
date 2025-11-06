// НАЗНАЧЕНИЕ: Централизованная система для сбора, форматирования и записи подробных логов боя. Является единой точкой входа для всех событий логирования, заменяя SimulationLogger и BattleReportSystem.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleLogFormatter (для форматирования строк), BattleLogWriter (для записи в файл), BattleUnit, BattleGrid.
// ПРИМЕЧАНИЕ: Является синглтоном. Настраивается в инспекторе для выбора уровня детализации лога.
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BattleLogger : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Tooltip("Папка, в которую будет сохранен итоговый файл лога.")]
    [FolderPath(AbsolutePath = true), Required(InfoMessageType.Error), SerializeField]
    private string _outputFolderPath;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS")]
    [BoxGroup("SETTINGS/File Options"), Tooltip("Базовое имя файла для лога.")]
    [SerializeField] private string _logFileName = "BattleLog";
    [BoxGroup("SETTINGS/File Options"), Tooltip("Если включено, для каждой сессии будет создаваться уникальный файл. Иначе будет использоваться один и тот же перезаписываемый файл.")]
    [SerializeField] private bool _useUniqueSessionFiles = false;

    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать информацию о начале сессии и каждого хода.")]
    [SerializeField] private bool _logTurnInfo = true;
    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать состояние сетки в начале каждого хода.")]
    [SerializeField] private bool _logGridState = true;
    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать состояние всех юнитов в начале каждого хода.")]
    [SerializeField] private bool _logUnitStates = true;
    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать каждое действие, выполняемое юнитами.")]
    [SerializeField] private bool _logActions = true;
    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать все изменения здоровья (урон, лечение).")]
    [SerializeField] private bool _logHealthChanges = true;
    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать смерть юнитов.")]
    [SerializeField] private bool _logDeaths = true;
    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать аналитическую сводку по итогам каждой волны.")]
    [SerializeField] private bool _logWaveAnalysis = true;
    [BoxGroup("SETTINGS/Logging Options"), Tooltip("Логировать результат боя (победа/поражение).")]
    [SerializeField] private bool _logBattleResult = true;

    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private string _sessionID;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _battleCounter;

    private BattleLogFormatter _formatter;
    private BattleLogWriter _writer;
    private static BattleLogger _instance;
    #endregion Поля

    #region Свойства
    /// <summary>
    /// Предоставляет глобальный доступ к экземпляру BattleLogger.
    /// </summary>
    public static BattleLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BattleLogger>();
            }
            return _instance;
        }
    }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DebugUtils.LogInstanceAlreadyExists(this, _instance);
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _formatter = new BattleLogFormatter();
        _writer = new BattleLogWriter();

        if (_useUniqueSessionFiles)
        {
            _sessionID = Guid.NewGuid().ToString();
        }

        ColoredDebug.CLog(gameObject, "<color=yellow>BattleLogger:</color> Синглтон инициализирован.", _ColoredDebug);
    }

    private void Start()
    {
        LogSessionStart();
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Начинает новую сессию логирования, создавая и очищая файл лога.
    /// </summary>
    public void LogSessionStart()
    {
        _battleCounter = 0;
        string fullPath = GetCurrentLogFilePath();

        _writer.Initialize(fullPath);
        string header = _formatter.FormatSessionStart(_sessionID);
        _writer.Append(header);
        ColoredDebug.CLog(gameObject, "<color=yellow>BattleLogger:</color> Новая сессия начата. Файл: <color=white>{0}</color>", _ColoredDebug, Path.GetFileName(fullPath));
    }

    /// <summary>
    /// Логирует начало нового боя (волны), создавая легенду юнитов и пропов.
    /// </summary>
    /// <param name="allUnits">Список всех юнитов, участвующих в бою.</param>
    public void LogNewBattleStart(List<BattleUnit> allUnits)
    {
        _battleCounter++;
        string battleHeader = _formatter.FormatBattleStart(_battleCounter, allUnits, BattleGridPropManager.Instance.ActiveProps);
        _writer.Append(battleHeader);
        ColoredDebug.CLog(gameObject, "<color=yellow>BattleLogger:</color> Начало боя <color=orange>#{0}</color>. Легенда создана.", _ColoredDebug, _battleCounter);
    }

    /// <summary>
    /// Логирует начало нового хода.
    /// </summary>
    /// <param name="turnNumber">Номер текущего хода.</param>
    /// <param name="turnOwner">Владелец хода (Игрок/Враги).</param>
    public void LogTurnStart(int turnNumber, string turnOwner)
    {
        if (!_logTurnInfo) return;
        string turnLog = _formatter.FormatTurnStart(turnNumber, turnOwner);
        _writer.Append(turnLog);
    }

    /// <summary>
    /// Логирует текущее состояние игровой сетки.
    /// </summary>
    /// <param name="grid">Игровая сетка.</param>
    /// <param name="allUnits">Список всех юнитов на поле.</param>
    public void LogGridState(BattleGrid grid, List<BattleUnit> allUnits)
    {
        if (!_logGridState) return;
        // --- ИЗМЕНЕНИЕ: Передаем пропы в форматтер ---
        string gridLog = _formatter.FormatGridState(grid, allUnits, BattleGridPropManager.Instance.ActiveProps);
        _writer.Append(gridLog);
    }

    /// <summary>
    /// Логирует состояние всех юнитов на поле.
    /// </summary>
    /// <param name="allUnits">Список всех юнитов.</param>
    public void LogAllUnitsState(BattleGrid grid, List<BattleUnit> allUnits)
    {
        if (!_logUnitStates) return;
        string unitsLog = _formatter.FormatUnitStates(grid, allUnits, _formatter.UnitSymbols);
        _writer.Append(unitsLog);
    }

    /// <summary>
    /// Логирует выполненное действие.
    /// </summary>
    /// <param name="performer">Юнит, выполнивший действие.</param>
    /// <param name="actionName">Название действия.</param>
    /// <param name="details">Дополнительные детали.</param>
    public void LogAction(BattleUnit performer, string actionName, string details)
    {
        if (!_logActions) return;
        string actionLog = _formatter.FormatAction(performer, actionName, details);
        _writer.Append(actionLog);
    }

    /// <summary>
    /// Логирует изменение здоровья юнита.
    /// </summary>
    /// <param name="target">Цель изменения.</param>
    /// <param name="amount">Количество (отрицательное для урона).</param>
    /// <param name="reason">Причина изменения.</param>
    /// <param name="isCritical">Было ли это критическим уроном.</param>
    public void LogHealthChange(BattleUnit target, int amount, string reason, bool isCritical = false)
    {
        if (!_logHealthChanges) return;
        string healthLog = _formatter.FormatHealthChange(target, amount, reason, isCritical);
        _writer.Append(healthLog);
    }

    /// <summary>
    /// Логирует смерть юнита.
    /// </summary>
    /// <param name="deceased">Погибший юнит.</param>
    public void LogDeath(BattleUnit deceased)
    {
        if (!_logDeaths) return;
        string deathLog = _formatter.FormatDeath(deceased);
        _writer.Append(deathLog);
    }

    /// <summary>
    /// Логирует аналитическую сводку по завершении волны.
    /// </summary>
    /// <param name="report">Объект отчета с данными о волне.</param>
    public void LogWaveAnalysis(BattleReport report)
    {
        if (!_logWaveAnalysis) return;
        string analysisLog = _formatter.FormatWaveAnalysis(report);
        _writer.Append(analysisLog);
        ColoredDebug.CLog(gameObject, "<color=yellow>BattleLogger:</color> Записана аналитика по волне #{0}.", _ColoredDebug, report.WaveNumber);
    }

    /// <summary>
    /// Логирует финальный результат боя.
    /// </summary>
    /// <param name="result">Строка с результатом (Победа/Поражение).</param>
    public void LogBattleEnd(string result)
    {
        if (!_logBattleResult) return;
        string endLog = _formatter.FormatBattleEnd(result);
        _writer.Append(endLog);
        ColoredDebug.CLog(gameObject, "<color=yellow>BattleLogger:</color> Бой завершен. Результат: <color=white>{0}</color>.", _ColoredDebug, result);
    }

    /// <summary>
    /// Открывает текущий файл лога.
    /// </summary>
    [Button("Open Log"), BoxGroup("ACTIONS", ShowLabel = false)]
    public void OpenLog()
    {
        if (_writer == null)
        {
            _writer = new BattleLogWriter();
        }
        string fullPath = GetCurrentLogFilePath();
        _writer.OpenLogFile(fullPath);
        ColoredDebug.CLog(gameObject, "<color=yellow>BattleLogger:</color> Запрос на открытие лог-файла.", _ColoredDebug);
    }

    /// <summary>
    /// Открывает папку, содержащую файл лога.
    /// </summary>
    [Button("Open Log Folder"), BoxGroup("ACTIONS")]
    public void OpenLogFolder()
    {
        if (_writer == null)
        {
            _writer = new BattleLogWriter();
        }
        _writer.OpenLogFolder(_outputFolderPath);
        ColoredDebug.CLog(gameObject, "<color=yellow>BattleLogger:</color> Запрос на открытие папки с логами.", _ColoredDebug);
    }
    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Возвращает полный путь к файлу лога в зависимости от настроек.
    /// </summary>
    private string GetCurrentLogFilePath()
    {
        string fileName;
        if (_useUniqueSessionFiles && !string.IsNullOrEmpty(_sessionID))
        {
            fileName = $"{_logFileName}_{_sessionID}.txt";
        }
        else
        {
            fileName = $"{_logFileName}.txt";
        }
        return Path.Combine(_outputFolderPath, fileName);
    }
    #endregion Личные методы
}