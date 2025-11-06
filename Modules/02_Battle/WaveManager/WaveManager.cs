// НАЗНАЧЕНИЕ: Является главным контроллером жизненного цикла волны. Отвечает за оркестрацию процесса: запуск, ожидание завершения и передачу управления другим компонентам.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: WaveDirector, WaveAssembler, EnemyFactory, ScenarioManager.
// ПРИМЕЧАНИЕ: Не содержит сложной логики, а лишь делегирует задачи специализированным компонентам. Может переопределяться системой сценариев.

using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(WaveDirector), typeof(WaveAssembler))]
public class WaveManager : MonoBehaviour
{
    public event UnityAction OnWaveSpawned;
    private event UnityAction _onWaveFinished;
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private EnemyFactory _enemyFactory;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleGrid _grid;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private WaveDirector _waveDirector;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private WaveAssembler _waveAssembler;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, ReadOnly] private BattleUnit _hero;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isSpawning;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;

    private LevelData _currentLevelData;
    private StageType _currentStageType;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_enemyFactory == null) DebugUtils.LogMissingReference(this, nameof(_enemyFactory));
        if (_grid == null) DebugUtils.LogMissingReference(this, nameof(_grid));
        if (_waveDirector == null) DebugUtils.LogMissingReference(this, nameof(_waveDirector));
        if (_waveAssembler == null) DebugUtils.LogMissingReference(this, nameof(_waveAssembler));
    }

    private void Start()
    {
        _hero = BattleUnit.Hero;
        if (_hero == null) DebugUtils.LogMissingReference(this, nameof(_hero));

        _waveDirector.Initialize(_hero);
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Запускает процесс создания и спавна одной волны врагов.
    /// </summary>
    public void StartWave(UnityAction onFinished, LevelData levelData, StageType stageType)
    {
        if (_isSpawning) return;
        _onWaveFinished = onFinished;
        _currentLevelData = levelData;
        _currentStageType = stageType;

        StartCoroutine(WaveLifecycleRoutine());
    }
    #endregion Публичные методы

    #region Личные методы
    private IEnumerator WaveLifecycleRoutine()
    {
        _isSpawning = true;
        int heroHealthAtWaveStart = _hero.Health.CurrentHealth;

        List<(EnemySO so, EnemySO.EnemyVariant variant)> enemiesForAnalysis = null;
        var spawnedUnits = new List<BattleUnit>();

        if (Settings.EnableScenarioMode && ScenarioManager.Instance != null && ScenarioManager.Instance.CurrentScenario != null)
        {
            var scenario = ScenarioManager.Instance.CurrentScenario;
            ColoredDebug.CLog(gameObject, "<color=purple>WaveManager:</color> РЕЖИМ СЦЕНАРИЯ АКТИВЕН. Спавн врагов из сценария <color=yellow>{0}</color>.", _ColoredDebug, scenario.name);

            enemiesForAnalysis = new List<(EnemySO so, EnemySO.EnemyVariant variant)>();

            foreach (var enemyData in scenario.enemiesToSpawn)
            {
                if (enemyData.enemySO != null)
                {
                    BattleUnit spawnedEnemy = _enemyFactory.SpawnEnemy(enemyData.enemySO, enemyData.level);
                    if (spawnedEnemy != null)
                    {
                        spawnedUnits.Add(spawnedEnemy);
                        // Собираем данные для анализа
                        var variant = enemyData.enemySO.AvailableVariants.FirstOrDefault(v => v.level == enemyData.level);
                        enemiesForAnalysis.Add((enemyData.enemySO, variant));
                    }
                }
            }
        }
        else // Стандартная логика
        {
            string waveType = null;
            ColoredDebug.CLog(gameObject, "<color=cyan>WaveManager:</color> Запрос на сборку волны к <color=yellow>WaveAssembler</color>...", _ColoredDebug);
            yield return StartCoroutine(_waveAssembler.AssembleWave(
                _waveDirector.CurrentThreatLevel,
                _currentStageType,
                _currentLevelData,
                (result) =>
                {
                    enemiesForAnalysis = result.wave;
                    waveType = result.waveType;
                }
            ));

            foreach (var choice in enemiesForAnalysis)
            {
                BattleUnit spawnedEnemy = _enemyFactory.SpawnEnemy(choice.so, choice.variant.level);
                if (spawnedEnemy != null)
                {
                    spawnedUnits.Add(spawnedEnemy);
                }
            }
        }

        var allUnitsForLog = new List<BattleUnit>(spawnedUnits);
        if (_hero != null)
        {
            allUnitsForLog.Add(_hero);
        }
        BattleLogger.Instance.LogNewBattleStart(allUnitsForLog);

        int enemiesToKillCount = spawnedUnits.Count;
        int enemiesKilledCount = 0;
        UnityAction<BattleUnit> killCounter = (e) =>
        {
            enemiesKilledCount++;
            ColoredDebug.CLog(gameObject, "<color=cyan>WaveManager:</color> Враг '{0}' убит. Всего убито: <color=orange>{1}/{2}</color>.", _ColoredDebug, e.name, enemiesKilledCount, enemiesToKillCount);
        };
        EnemyManager.Instance.RegisterEnemyDeathListener(killCounter);

        yield return new WaitForSeconds(Settings.WaitAfterAllEnemySpawned);
        ColoredDebug.CLog(gameObject, "<color=cyan>WaveManager:</color> Все враги заспавнены. Переключаю камеру в режим <color=orange>Combat</color>.", _ColoredDebug);
        DynamicDuelCamera.Instance.SwitchToCombatMode();

        OnWaveSpawned?.Invoke();
        yield return new WaitUntil(() => enemiesKilledCount >= enemiesToKillCount || !_hero.IsAlive);

        EnemyManager.Instance.UnregisterEnemyDeathListener(killCounter);
        if (_hero.IsAlive)
        {
            // Анализ боя происходит в любом случае, но в режиме сценария его результат не влияет на следующую волну
            _waveDirector.AnalyzeBattleAndPrepareForNext(enemiesForAnalysis, heroHealthAtWaveStart);
            _isSpawning = false;
            _onWaveFinished?.Invoke();
        }
        else
        {
            _isSpawning = false;
        }
    }
    #endregion
}