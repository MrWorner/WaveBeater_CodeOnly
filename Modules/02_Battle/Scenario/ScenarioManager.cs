// НАЗНАЧЕНИЕ: Управляет выполнением последовательностей боевых сценариев. Отвечает за отслеживание текущего сценария и переход к следующему.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: Settings, ScenarioSequenceSO, BattleScenarioSO.
// ПРИМЕЧАНИЕ: Является центральной точкой для интеграции системы сценариев с основной игровой логикой (ArenaManager, WaveManager).

using Sirenix.OdinInspector;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentScenarioIndex = -1;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private static ScenarioManager _instance;
    #endregion Поля

    #region Свойства
    public static ScenarioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ScenarioManager>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Возвращает текущий активный сценарий для исполнения.
    /// </summary>
    public BattleScenarioSO CurrentScenario { get; private set; }
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
    }

    private void Start()
    {
        if (Settings.EnableScenarioMode)
        {
            ColoredDebug.CLog(gameObject, "<color=purple><b>ScenarioManager:</b></color> Режим сценариев АКТИВИРОВАН. Загрузка первого сценария.", _ColoredDebug);
            Advance();
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=grey>ScenarioManager:</color> Режим сценариев отключен.", _ColoredDebug);
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Переключает менеджер на следующий сценарий в последовательности.
    /// </summary>
    public void Advance()
    {
        if (!Settings.EnableScenarioMode || Settings.ActiveScenarioSequence == null || Settings.ActiveScenarioSequence.Scenarios.Count == 0)
        {
            CurrentScenario = null;
            return;
        }

        var sequence = Settings.ActiveScenarioSequence;
        _currentScenarioIndex++;

        if (_currentScenarioIndex >= sequence.Scenarios.Count)
        {
            if (sequence.LoopSequence)
            {
                _currentScenarioIndex = 0;
                ColoredDebug.CLog(gameObject, "<color=purple><b>ScenarioManager:</b></color> Последовательность сценариев зациклена.", _ColoredDebug);
            }
            else
            {
                CurrentScenario = null;
                ColoredDebug.CLog(gameObject, "<color=purple><b>ScenarioManager:</b></color> Все сценарии в последовательности завершены.", _ColoredDebug);
                return;
            }
        }

        CurrentScenario = sequence.Scenarios[_currentScenarioIndex];
        ColoredDebug.CLog(gameObject, "<color=purple><b>ScenarioManager:</b></color> Следующий сценарий: <color=yellow>{0}</color> (Индекс: {1})", _ColoredDebug, CurrentScenario != null ? CurrentScenario.name : "NONE", _currentScenarioIndex);
    }
    #endregion Публичные методы
}