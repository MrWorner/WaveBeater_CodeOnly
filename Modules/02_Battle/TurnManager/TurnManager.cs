// НАЗНАЧЕНИЕ: Управляет последовательностью ходов (игрок, враги) и координирует фазы хода врагов (планирование, движение, атака).
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, EnemyManager, WaveManager, BattleLogger, BattleGrid.
// ПРИМЕЧАНИЕ: Использует пошаговую систему с синхронной фазой движения для всех врагов.
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class TurnManager : MonoBehaviour
{
    /// <summary> Вспомогательный класс для хранения плана движения. </summary>
    private class MovementPlan
    {
        public BattleUnit unit;
        public Vector2Int from;
        public Vector2Int to;
        public AIAction action; // Сохраняем AIAction (MoveTowardsTargetAction или MoveHorizontallyAction)
        public bool isApproved;
    }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private EnemyManager _enemyManager; ///Ссылка на менеджер врагов.
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private WaveManager _waveManager; ///Ссылка на менеджер волн.
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private BattleUnit _hero; ///Ссылка на героя (получается динамически).
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentTurn = -1; ///Номер текущего хода.
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isPlayerTurn = true; ///True, если сейчас ход игрока.
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isTurnInProgress = false; ///True, если ход в данный момент выполняется.
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private static TurnManager _instance; ///Статический экземпляр синглтона.
    private Queue<IEnumerator> _pendingCoroutines = new Queue<IEnumerator>(); ///Очередь для отложенных корутин (напр., Backlash).
    private Dictionary<BattleUnit, MovementPlan> _movementPlans = new Dictionary<BattleUnit, MovementPlan>(); ///Планы движения для текущего хода врагов.
    #endregion Поля

    #region Свойства
    /// <summary> Статический экземпляр TurnManager (Singleton). </summary>
    public static TurnManager Instance => _instance;
    /// <summary> Возвращает true, если сейчас ход игрока. </summary>
    public bool IsPlayerTurn { get => _isPlayerTurn; }
    /// <summary> Возвращает номер текущего хода (начинается с 1). </summary>
    public int CurrentTurn { get => _currentTurn; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this) { DebugUtils.LogInstanceAlreadyExists(this, _instance); Destroy(gameObject); return; }
        _instance = this;
        if (_enemyManager == null) DebugUtils.LogMissingReference(this, nameof(_enemyManager));
        if (_waveManager == null) DebugUtils.LogMissingReference(this, nameof(_waveManager));
        if (_waveManager != null) _waveManager.OnWaveSpawned += StartBattle;
    }

    private void Start()
    {
        RefreshHeroReference();
        if (_hero == null)
        {
            Debug.LogError("[TurnManager] Critical: Hero reference could not be established in Start.");
        }
    }

    private void OnDestroy()
    {
        if (_waveManager != null) _waveManager.OnWaveSpawned -= StartBattle;
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary> Обновляет ссылку на объект героя. </summary>
    public void RefreshHeroReference()
    {
        _hero = BattleUnit.Hero;
        ColoredDebug.CLog(gameObject, "<color=cyan>TurnManager:</color> Ссылка на героя обновлена.", _ColoredDebug);
        if (_hero == null)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>TurnManager:</color> Предупреждение: Герой не найден при обновлении ссылки.", _ColoredDebug);
        }
    }

    /// <summary> Добавляет корутину в очередь для выполнения после текущего действия. </summary>
    /// <param name="coroutine">Корутина для добавления в очередь.</param>
    public void EnqueueAction(IEnumerator coroutine)
    {
        _pendingCoroutines.Enqueue(coroutine);
        ColoredDebug.CLog(gameObject, "<color=grey>TurnManager:</color> Корутина добавлена в очередь ожидания.", _ColoredDebug);
    }

    /// <summary> Начинает бой после спавна первой волны. </summary>
    public void StartBattle()
    {
        if (_currentTurn != -1 || _enemyManager.Enemies.Count == 0 || _hero == null || !_hero.IsAlive) return;
        _isPlayerTurn = true;
        _currentTurn = 0;
        ColoredDebug.CLog(gameObject, "<color=lime><b>TurnManager:</b></color> Бой начался! УДАЧИ! 🟢", _ColoredDebug);
        StartNextTurn();
    }

    /// <summary> Инициирует начало следующего хода (игрока или врагов). </summary>
    [Button(ButtonSizes.Medium)]
    public void StartNextTurn()
    {
        if (_isTurnInProgress)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>TurnManager:</color> Ход уже в процессе, ожидаем завершения. Текущий ход: <color=yellow>{0}</color>. 🟡", _ColoredDebug, _isPlayerTurn ? "Игрока" : "Врагов");
            return;
        }

        if (_hero == null || !_hero.IsAlive)
        {
            ColoredDebug.CLog(gameObject, "<color=red><b>TurnManager:</b></color> Герой мертв, бой окончен. 🔴", _ColoredDebug);
            _isTurnInProgress = false;
            return;
        }
        if (_enemyManager.Enemies.Count == 0 && _currentTurn > 0)
        {
            ColoredDebug.CLog(gameObject, "<color=green><b>TurnManager:</b></color> Все враги побеждены, бой окончен. 🟢", _ColoredDebug);
            _currentTurn = -1;
            _isTurnInProgress = false;
            return;
        }

        _isTurnInProgress = true;
        TickUnitTimers();

        if (_isPlayerTurn)
        {
            _currentTurn++;
            LogTurnState("Player");
            StartCoroutine(PlayerTurnRoutine());
        }
        else
        {
            LogTurnState("Enemies");
            StartCoroutine(EnemyTurnRoutine());
        }
    }
    #endregion Публичные методы

    #region Личные методы (Логика Ходов)
    /// <summary> Корутина для выполнения хода игрока. </summary>
    private IEnumerator PlayerTurnRoutine()
    {
        ColoredDebug.CLog(gameObject, "<color=lime>TurnManager:</color> Начало хода игрока (Ход #{0}). 🟢", _ColoredDebug, _currentTurn);
        if (_hero != null && _hero.IsAlive)
        {
            _hero.Brain.ResetTurnState();
            _hero.TakeTurn(() =>
            {
                ColoredDebug.CLog(gameObject, "<color=lime>TurnManager:</color> Ход игрока завершен. Переход к ходу врагов. 🟢", _ColoredDebug);
                EndTurn();
            });
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>TurnManager:</color> Герой не найден или мертв в начале хода игрока!", true);
            EndTurn();
        }
        yield break;
    }

    /// <summary> Корутина для выполнения хода врагов, включая все фазы. </summary>
    private IEnumerator EnemyTurnRoutine()
    {
        ColoredDebug.CLog(gameObject, "<color=red>TurnManager:</color> Начало хода врагов (Ход #{0}). Всего врагов: {1}. 🔴", _ColoredDebug, _currentTurn, _enemyManager.Enemies.Count);
        var enemiesToProcess = new List<BattleUnit>(_enemyManager.Enemies);
        _movementPlans.Clear();

        foreach (var enemy in enemiesToProcess)
        {
            if (IsEnemyValid(enemy))
            {
                enemy.Brain.ResetTurnState();
                ColoredDebug.CLog(enemy.gameObject, "<color=yellow>🔵 НАЧАЛО ХОДА ВРАГА:</color> ОД сброшены до {0}", _ColoredDebug, enemy.Brain.ActionPointsLeft);
            }
        }

        // ===== ФАЗА 1: ПЛАНИРОВАНИЕ ДВИЖЕНИЯ =====
        // Используем поле класса _movementPlans
        LogPhaseStart("ФАЗА 1: ПЛАНИРОВАНИЕ ДВИЖЕНИЯ", "lightblue");

        foreach (var enemy in enemiesToProcess)
        {
            if (!IsEnemyValid(enemy)) continue;
            if (enemy.Brain == null || enemy.Brain.Profile == null || enemy.Brain.Profile.availableActions == null)
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=orange>⚠️ ПРОПУСК (Фаза 1):</color> Отсутствует Brain, Profile или Actions", _ColoredDebug);
                continue;
            }

            var executableActions = enemy.Brain.Profile.availableActions
                                        .Where(action => enemy.Brain.CanUseAction(action))
                                        .ToList();

            if (executableActions.Count == 0)
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=yellow>⚠️ ПРОПУСК (Фаза 1):</color> Нет доступных действий (CanUseAction)", _ColoredDebug);
                continue;
            }

            AIAction chosenAction = enemy.Brain.Profile.behaviorPattern?.DecideAction(enemy, executableActions);

            ColoredDebug.CLog(enemy.gameObject, "<color=cyan>📝 ПРОВЕРКА ПЛАНА:</color> Выбрано действие: <color=white>{0}</color>", _ColoredDebug, chosenAction?.actionName ?? "NULL");

            MoveTowardsTargetAction standardMoveAction = chosenAction as MoveTowardsTargetAction;
            MoveHorizontallyAction horizontalMoveAction = chosenAction as MoveHorizontallyAction;
            AIAction moveActionToPlan = null;
            Vector2Int nextStep = enemy.CurrentPosition;

            if (standardMoveAction != null)
            {
                nextStep = enemy.Movement.FindBestMove(enemy);
                moveActionToPlan = standardMoveAction;
                ColoredDebug.CLog(enemy.gameObject, "<color=cyan>🔍 ПОИСК ПУТИ (A*):</color> Текущая: {0}, Найдена: {1}", _ColoredDebug, enemy.CurrentPosition, nextStep);
            }
            else if (horizontalMoveAction != null)
            {
                var target = enemy.TargetingSystem?.GetBestTarget();
                if (target != null)
                {
                    int deltaX = target.CurrentPosition.x - enemy.CurrentPosition.x;
                    if (deltaX != 0)
                    {
                        Vector2Int horizontalDirection = new Vector2Int(System.Math.Sign(deltaX), 0);
                        nextStep = enemy.CurrentPosition + horizontalDirection;
                        moveActionToPlan = horizontalMoveAction;
                        ColoredDebug.CLog(enemy.gameObject, "<color=cyan>📏 ПОИСК ПУТИ (Горизонталь):</color> Текущая: {0}, Расчетная: {1}", _ColoredDebug, enemy.CurrentPosition, nextStep);
                    }
                    else
                    {
                        ColoredDebug.CLog(enemy.gameObject, "<color=orange>⚠️ ОШИБКА ПЛАНА (Горизонталь):</color> DeltaX == 0, хотя действие выбрано.", _ColoredDebug);
                        nextStep = enemy.CurrentPosition;
                    }
                }
                else
                {
                    ColoredDebug.CLog(enemy.gameObject, "<color=orange>⚠️ ОШИБКА ПЛАНА (Горизонталь):</color> Цель потеряна.", _ColoredDebug);
                    nextStep = enemy.CurrentPosition;
                }
            }
            else
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=grey>⏸️ БЕЗ ПЛАНА ДВИЖЕНИЯ:</color> Выбрано действие <color=white>'{0}'</color>, не требующее планирования.", _ColoredDebug, chosenAction?.actionName ?? "NULL");
            }

            if (moveActionToPlan != null && nextStep != enemy.CurrentPosition)
            {
                if (enemy.Brain.ActionPointsLeft >= moveActionToPlan.actionPointCost)
                {
                    MovementPlan plan = new MovementPlan
                    {
                        unit = enemy,
                        from = enemy.CurrentPosition,
                        to = nextStep,
                        action = moveActionToPlan,
                        isApproved = false
                    };
                    // Используем поле класса _movementPlans
                    _movementPlans[enemy] = plan;
                    ColoredDebug.CLog(enemy.gameObject, "<color=cyan>📋 ПЛАН ДВИЖЕНИЯ ДОБАВЛЕН:</color> {0} → {1} (Action: {2})", _ColoredDebug, enemy.CurrentPosition, nextStep, moveActionToPlan.actionName);
                }
                else
                {
                    ColoredDebug.CLog(enemy.gameObject, "<color=orange>⚠️ ПЛАН ОТКЛОНЕН (Недостаточно ОД):</color> Для действия '{0}' нужно {1}, есть {2}.", _ColoredDebug, moveActionToPlan.actionName, moveActionToPlan.actionPointCost, enemy.Brain.ActionPointsLeft);
                }
            }
            else if (moveActionToPlan != null)
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=yellow>⏸️ БЕЗ ДВИЖЕНИЯ:</color> Остается на месте {0} (не найден лучший ход для '{1}')", _ColoredDebug, enemy.CurrentPosition, moveActionToPlan.actionName);
            }
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>📊 ИТОГО ПЛАНОВ:</color> {0} из {1} врагов планируют движение", _ColoredDebug, _movementPlans.Count, enemiesToProcess.Count(IsEnemyValid));

        // ===== ФАЗА 2: РЕЗЕРВАЦИЯ И ПРОВЕРКА ЦЕПОЧЕК =====
        LogPhaseStart("ФАЗА 2: РЕЗЕРВАЦИЯ КЛЕТОК", "yellow");
        // Используем поле класса _movementPlans
        ResolveMovementConflicts(_movementPlans);

        // ===== ФАЗА 3.A: ОСВОБОЖДЕНИЕ СТАРЫХ КЛЕТОК =====
        LogPhaseStart("ФАЗА 3.A: ОСВОБОЖДЕНИЕ КЛЕТОК", "lime");
        // Используем поле класса _movementPlans
        foreach (var kvp in _movementPlans)
        {
            if (!kvp.Value.isApproved || !IsEnemyValid(kvp.Key)) continue;
            ColoredDebug.CLog(kvp.Key.gameObject, "<color=orange>📤 ОСВОБОЖДЕНИЕ:</color> Старая позиция {0}", _ColoredDebug, kvp.Value.from);
            kvp.Key.Movement.ClearOccupation();
        }

        // ===== ФАЗА 3.B: ЗАХВАТ НОВЫХ КЛЕТОК И АНИМАЦИЯ =====
        LogPhaseStart("ФАЗА 3.B: ЗАХВАТ КЛЕТОК И ДВИЖЕНИЕ", "lime");
        int movementsInProgress = 0;
        UnityAction onMoveComplete = () =>
        {
            movementsInProgress--;
            ColoredDebug.CLog(gameObject, "<color=grey>📉 СЧЕТЧИК ДВИЖЕНИЙ:</color> {0}", _ColoredDebug, movementsInProgress);
        };

        // Используем поле класса _movementPlans
        foreach (var kvp in _movementPlans)
        {
            if (!kvp.Value.isApproved) continue;

            BattleUnit enemy = kvp.Key;
            MovementPlan plan = kvp.Value;

            if (!IsEnemyValid(enemy))
            {
                ColoredDebug.CLog(enemy?.gameObject, "<color=red>❌ ОТМЕНА (Фаза 3.B):</color> Враг невалиден", _ColoredDebug);
                continue;
            }

            ColoredDebug.CLog(enemy.gameObject, "<color=lime>🚀 ВЫПОЛНЕНИЕ (3.B):</color> {0} → {1}", _ColoredDebug, plan.from, plan.to);
            enemy.Brain.ConsumeActionPoints(plan.action.actionPointCost);
            enemy.Brain.RecordActionUse(plan.action);
            BattleLogger.Instance.LogAction(enemy, plan.action.actionName, $"Move to {plan.to}");

            movementsInProgress++;
            ColoredDebug.CLog(gameObject, "<color=grey>📈 СЧЕТЧИК ДВИЖЕНИЙ:</color> {0}", _ColoredDebug, movementsInProgress);

            enemy.Movement.ExecuteMove(plan.to, onMoveComplete);
            yield return new WaitForSeconds(Settings.QuickEnemyMoveWaitTime);
        }

        // ===== ФАЗА 4: ОЖИДАНИЕ ЗАВЕРШЕНИЯ ДВИЖЕНИЯ =====
        LogPhaseStart($"ФАЗА 4: ОЖИДАНИЕ ({movementsInProgress} юнитов)", "orange");
        float waitStartTime = Time.time;
        const float maxWaitTime = 10f;

        while (movementsInProgress > 0 && _hero != null && _hero.IsAlive && (Time.time - waitStartTime) < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (movementsInProgress > 0)
        {
            ColoredDebug.CLog(gameObject, "<color=red>⚠️ ТАЙМАУТ (Фаза 4):</color> Движения не завершились за {0}с. Осталось: {1}", true, maxWaitTime, movementsInProgress);
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=lime>✅ ВСЕ ДВИЖЕНИЯ ЗАВЕРШЕНЫ</color>", _ColoredDebug);
        }

        ColoredDebug.CLog(gameObject, "<color=yellow>🚦 ПЕРЕД ФАЗОЙ 5 (Post-Movement):</color> Проверка ОД для всех врагов:", _ColoredDebug);
        foreach (var enemy in enemiesToProcess)
        {
            if (IsEnemyValid(enemy))
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=yellow>   -> {0}:</color> ОД={1}", _ColoredDebug, GetSafeEnemyName(enemy), enemy.Brain.ActionPointsLeft);
            }
        }

        // ===== ФАЗА 5: ВЫПОЛНЕНИЕ ОСТАЛЬНЫХ ДЕЙСТВИЙ (АТАКИ, ОБХОДЫ И ДР.) =====
        LogPhaseStart("ФАЗА 5: ДЕЙСТВИЯ (АТАКИ/ОБХОДЫ)", "magenta");
        foreach (var enemy in enemiesToProcess)
        {
            if (!IsEnemyValid(enemy))
            {
                ColoredDebug.CLog(enemy?.gameObject, "<color=red>❌ ПРОПУСК (Фаза 5):</color> Враг невалиден", _ColoredDebug);
                continue;
            }
            if (_hero == null || !_hero.IsAlive)
            {
                ColoredDebug.CLog(gameObject, "<color=red>💀 ГЕРОЙ ПОГИБ:</color> Прерываю фазу действий врагов!", _ColoredDebug);
                break;
            }

            string enemyName = GetSafeEnemyName(enemy);
            ColoredDebug.CLog(enemy.gameObject, "<color=magenta>🎯 ФАЗА ДЕЙСТВИЙ:</color> {0} | ОД: {1}", _ColoredDebug, enemyName, enemy.Brain.ActionPointsLeft);

            if (enemy.Brain.ActionPointsLeft > 0)
            {
                bool actionsCompleted = false;
                StartCoroutine(ExecuteEnemyActionsRoutine(enemy, () => actionsCompleted = true));

                float actionWaitStartTime = Time.time;
                const float maxActionWaitTime = 5f;

                while (!actionsCompleted && IsEnemyValid(enemy) && _hero != null && _hero.IsAlive && (Time.time - actionWaitStartTime) < maxActionWaitTime)
                {
                    yield return null;
                }

                if (!actionsCompleted && IsEnemyValid(enemy))
                {
                    ColoredDebug.CLog(enemy.gameObject, "<color=red>⚠️ ТАЙМАУТ (Фаза 5):</color> Действия врага {0} не завершились за {1}с", true, enemyName, maxActionWaitTime);
                }
            }
            else
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=grey>⏹️ НЕТ ОД:</color> Пропуск действий для {0}", _ColoredDebug, enemyName);
            }

            while (_pendingCoroutines.Count > 0)
            {
                ColoredDebug.CLog(gameObject, "<color=cyan>🔄 ВЫПОЛНЕНИЕ ОТЛОЖЕННЫХ ДЕЙСТВИЙ:</color> {0} в очереди", _ColoredDebug, _pendingCoroutines.Count);
                yield return StartCoroutine(_pendingCoroutines.Dequeue());
                if (_hero == null || !_hero.IsAlive)
                {
                    ColoredDebug.CLog(gameObject, "<color=red>💀 ГЕРОЙ ПОГИБ:</color> Во время отложенных действий!", _ColoredDebug);
                    goto EndEnemyTurn;
                }
            }
            yield return new WaitForSeconds(Settings.QuickEnemyMoveWaitTime);
        }

    EndEnemyTurn:
        EndTurn();
    }


    /// <summary> Корутина для выполнения цикла действий одного врага (атаки, способности, обходы). </summary>
    /// <param name="enemy">Враг, выполняющий действия.</param>
    /// <param name="onComplete">Callback по завершении.</param>
    private IEnumerator ExecuteEnemyActionsRoutine(BattleUnit enemy, UnityAction onComplete)
    {
        ColoredDebug.CLog(enemy.gameObject, "<color=magenta>🎬 НАЧАЛО ДЕЙСТВИЙ:</color> ОД: {0}", _ColoredDebug, enemy.Brain.ActionPointsLeft);
        while (enemy.Brain.ActionPointsLeft > 0 && !enemy.IsBusy && IsEnemyValid(enemy) && _hero != null && _hero.IsAlive)
        {
            if (enemy.Brain.Profile?.behaviorPattern == null)
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=orange>⚠️ ПРЕРЫВАНИЕ (Действия):</color> Нет behaviorPattern", _ColoredDebug);
                break;
            }

            // --- ИЗМЕНЕНИЕ: Получаем ВСЕ доступные действия, КРОМЕ УЖЕ ЗАПЛАНИРОВАННОГО движения ---
            // Используем поле класса _movementPlans
            AIAction plannedMoveAction = _movementPlans.ContainsKey(enemy) && _movementPlans[enemy].isApproved ? _movementPlans[enemy].action : null;
            var executableActions = enemy.Brain.Profile.availableActions
                                        .Where(action => action != plannedMoveAction && // Исключаем уже запланированное движение
                                                         enemy.Brain.CanUseAction(action))
                                        .ToList();

            ColoredDebug.CLog(enemy.gameObject, "<color=cyan>📋 ДОСТУПНЫЕ ДЕЙСТВИЯ (Non-Planned Move):</color> {0}", _ColoredDebug, executableActions.Count);


            if (executableActions.Count == 0)
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=orange>⏹️ НЕТ ДЕЙСТВИЙ (Non-Planned Move):</color> Все действия выполнены или недоступны", _ColoredDebug);
                break;
            }

            AIAction chosenAction = enemy.Brain.Profile.behaviorPattern.DecideAction(enemy, executableActions);

            if (chosenAction != null)
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=lime>🎯 ВЫБРАНО ДЕЙСТВИЕ:</color> {0} (ОД: {1})", _ColoredDebug, chosenAction.actionName, chosenAction.actionPointCost);
                bool actionIsComplete = false;
                chosenAction.Execute(enemy, () =>
                {
                    actionIsComplete = true;
                    ColoredDebug.CLog(enemy.gameObject, "<color=green>✅ ДЕЙСТВИЕ ЗАВЕРШЕНО:</color> {0}", _ColoredDebug, chosenAction.actionName);
                });

                enemy.Brain.ConsumeActionPoints(chosenAction.actionPointCost);
                enemy.Brain.RecordActionUse(chosenAction);

                float actionWaitStartTime = Time.time;
                const float maxActionWaitTime = 3f;

                while (!actionIsComplete && IsEnemyValid(enemy) && _hero != null && _hero.IsAlive && (Time.time - actionWaitStartTime) < maxActionWaitTime)
                {
                    yield return null;
                }

                if (!actionIsComplete && IsEnemyValid(enemy))
                {
                    ColoredDebug.CLog(enemy.gameObject, "<color=red>⚠️ ТАЙМАУТ ДЕЙСТВИЯ:</color> {0} не завершилось за {1}с", true, chosenAction.actionName, maxActionWaitTime);
                }
                yield return null; // Уступаем кадр после каждого действия
            }
            else
            {
                ColoredDebug.CLog(enemy.gameObject, "<color=yellow>🤔 НЕТ РЕШЕНИЯ:</color> BehaviorPattern не выбрал действие из доступных", _ColoredDebug);
                break;
            }
        } // Конец while

        ColoredDebug.CLog(enemy.gameObject, "<color=magenta>🏁 ЗАВЕРШЕНИЕ ДЕЙСТВИЙ:</color> Осталось ОД: {0}", _ColoredDebug, enemy.Brain?.ActionPointsLeft ?? -1);
        onComplete?.Invoke();
    }

    /// <summary> Завершает текущий ход и передает управление. </summary>
    private void EndTurn()
    {
        _isPlayerTurn = !_isPlayerTurn;
        _isTurnInProgress = false;
        ColoredDebug.CLog(gameObject, "<color=cyan>🔄 СМЕНА ХОДА:</color> Теперь ход {0}", _ColoredDebug, _isPlayerTurn ? "Игрока" : "Врагов");
        StartNextTurn();
    }

    /// <summary> Обновляет внутренние таймеры юнитов. </summary>
    private void TickUnitTimers()
    {
        ColoredDebug.CLog(gameObject, "<color=grey>TurnManager:</color> Обновление таймеров юнитов...", _ColoredDebug);
        if (_hero != null && _hero.IsAlive)
        {
            _hero.Arsenal?.TickTurn();
        }
        foreach (var enemy in _enemyManager.Enemies)
        {
            if (IsEnemyValid(enemy))
            {
                enemy.Arsenal?.TickTurn();
            }
        }
    }

    /// <summary> Логирует начало хода и состояние игры. </summary>
    /// <param name="owner">"Player" или "Enemies".</param>
    private void LogTurnState(string owner)
    {
        var allUnits = new List<BattleUnit>(_enemyManager.Enemies);
        if (_hero != null) allUnits.Add(_hero);

        BattleLogger.Instance.LogTurnStart(_currentTurn, owner);
        BattleLogger.Instance.LogGridState(BattleGrid.Instance, allUnits);
        BattleLogger.Instance.LogAllUnitsState(BattleGrid.Instance, allUnits);
    }

    /// <summary> Выводит в консоль заголовок для текущей фазы хода врагов. </summary>
    private void LogPhaseStart(string phaseName, string color)
    {
        ColoredDebug.CLog(gameObject, $"<color={color}>╔══════════════════════════════════════╗</color>", _ColoredDebug);
        ColoredDebug.CLog(gameObject, $"<color={color}>║  {phaseName,-34}║</color>", _ColoredDebug);
        ColoredDebug.CLog(gameObject, $"<color={color}>╚══════════════════════════════════════╝</color>", _ColoredDebug);
    }


    /// <summary> Проверяет, валиден ли враг для обработки. </summary>
    /// <param name="enemy">Юнит врага для проверки.</param>
    private bool IsEnemyValid(BattleUnit enemy)
    {
        bool isValid = enemy != null && enemy.gameObject != null && enemy.IsAlive;
        if (!isValid && _ColoredDebug)
        {
            string enemyName = "Unknown or Destroyed Enemy";
            try { if (enemy != null) enemyName = enemy.name; } catch { }
            ColoredDebug.CLog(gameObject, "<color=red>❌ НЕВАЛИДНЫЙ ВРАГ:</color> {0} (isNull={1}, isDestroyed={2}, isAlive={3})", true,
                         enemyName,
                         enemy == null,
                         enemy != null && enemy.gameObject == null,
                         enemy != null ? enemy.IsAlive.ToString() : "N/A");
        }
        return isValid;
    }

    /// <summary> Безопасно возвращает имя врага или заглушку. </summary>
    private string GetSafeEnemyName(BattleUnit enemy)
    {
        try { return (enemy != null && enemy) ? enemy.name : "Unknown/Destroyed Enemy"; }
        catch { return "Destroyed Enemy"; }
    }

    #endregion Личные методы (Логика Ходов)

    #region Метод разрешения конфликтов (вынесен отдельно)
    /// <summary> Разрешает конфликты движения между врагами. </summary>
    /// <param name="plans">Словарь с планами движения.</param>
    private void ResolveMovementConflicts(Dictionary<BattleUnit, MovementPlan> plans)
    {
        var cellReservations = new Dictionary<Vector2Int, BattleUnit>();
        var unitsWithConflicts = new HashSet<BattleUnit>();

        ColoredDebug.CLog(gameObject, "<color=yellow>🔍 ПРОВЕРКА КОНФЛИКТОВ:</color> Всего планов: {0}", _ColoredDebug, plans.Count);
        // Этап 1: Разрешение прямых конфликтов
        foreach (var unitId in plans.Keys.Select(u => u.GetInstanceID()).OrderBy(id => id))
        {
            var kvp = plans.FirstOrDefault(p => p.Key.GetInstanceID() == unitId);
            BattleUnit currentUnit = kvp.Key;
            MovementPlan currentPlan = kvp.Value;

            if (!IsEnemyValid(currentUnit)) continue;
            Vector2Int targetPos = currentPlan.to;
            //ColoredDebug.CLog(currentUnit.gameObject, "<color=cyan>📍 ЦЕЛЕВАЯ ПОЗИЦИЯ:</color> {0}", _ColoredDebug, targetPos);

            if (cellReservations.TryGetValue(targetPos, out BattleUnit existingUnit))
            {
                //ColoredDebug.CLog(currentUnit.gameObject, "<color=orange>⚡ КОНФЛИКТ:</color> с {0} на клетке {1}", _ColoredDebug, GetSafeEnemyName(existingUnit), targetPos);
                if (currentUnit.GetInstanceID() < existingUnit.GetInstanceID())
                {
                    unitsWithConflicts.Add(existingUnit);
                    cellReservations[targetPos] = currentUnit;
                    ColoredDebug.CLog(existingUnit.gameObject, "<color=red>❌ ПРОИГРАЛ КОНФЛИКТ (1):</color> с {0} на {1} - ход отменен", _ColoredDebug, GetSafeEnemyName(currentUnit), targetPos);
                    //ColoredDebug.CLog(currentUnit.gameObject, "<color=green>✅ ВЫИГРАЛ КОНФЛИКТ (1):</color> Клетка {0} теперь зарезервирована", _ColoredDebug, targetPos);
                }
                else
                {
                    unitsWithConflicts.Add(currentUnit);
                    ColoredDebug.CLog(currentUnit.gameObject, "<color=red>❌ ПРОИГРАЛ КОНФЛИКТ (1):</color> с {0} на {1} - ход отменен", _ColoredDebug, GetSafeEnemyName(existingUnit), targetPos);
                }
            }
            else
            {
                cellReservations.Add(targetPos, currentUnit);
                //ColoredDebug.CLog(currentUnit.gameObject, "<color=green>✅ РЕЗЕРВАЦИЯ:</color> Клетка {0} зарезервирована", _ColoredDebug, targetPos);
            }
        }
        ColoredDebug.CLog(gameObject, "<color=yellow>📊 ПОСЛЕ ПРЯМЫХ КОНФЛИКТОВ:</color> Конфликтов (отмененных ходов): {0}", _ColoredDebug, unitsWithConflicts.Count);

        // Этап 2: Проверка цепочек
        bool changesMadeThisIteration;
        int maxIterations = Mathf.Max(plans.Count + 1, 3);
        int currentIteration = 0;
        do
        {
            changesMadeThisIteration = false;
            currentIteration++;
            ColoredDebug.CLog(gameObject, "<color=cyan>🔗 ПРОВЕРКА ЦЕПОЧЕК (Итерация {0}/{1}):</color>", _ColoredDebug, currentIteration, maxIterations);
            foreach (var unitId in plans.Keys.Select(u => u.GetInstanceID()).OrderBy(id => id))
            {
                var kvp = plans.FirstOrDefault(p => p.Key.GetInstanceID() == unitId);
                BattleUnit unit = kvp.Key;
                MovementPlan plan = kvp.Value;

                if (unitsWithConflicts.Contains(unit) || !IsEnemyValid(unit)) continue;

                BattleCell targetCell = BattleGrid.Instance.GetCell(plan.to);

                if (targetCell == null || !targetCell.IsPassable)
                {
                    ColoredDebug.CLog(unit.gameObject, "<color=red>❌ БЛОКИРОВКА (Непроходимо):</color> Клетка {0} непроходима или null", _ColoredDebug, plan.to);
                    if (unitsWithConflicts.Add(unit))
                    {
                        changesMadeThisIteration = true;
                        plan.isApproved = false;
                        ColoredDebug.CLog(unit.gameObject, "<color=yellow>📝 РЕЗУЛЬТАТ ПРОВЕРКИ:</color> ОТКЛОНЕНО", _ColoredDebug);
                    }
                    continue;
                }

                if (targetCell.IsOccupied())
                {
                    string occupantName = (targetCell.Occupant as Component)?.gameObject.name ?? targetCell.Occupant?.GetType().Name ?? "UNKNOWN";

                    if (targetCell.Occupant is BattleUnit occupant && plans.ContainsKey(occupant))
                    {
                        if (unitsWithConflicts.Contains(occupant))
                        {
                            ColoredDebug.CLog(unit.gameObject, "<color=red>❌ БЛОКИРОВКА (Цепочка):</color> {0} не освобождает {1} (ход оккупанта отменен)", _ColoredDebug, GetSafeEnemyName(occupant), plan.to);
                            if (unitsWithConflicts.Add(unit))
                            {
                                changesMadeThisIteration = true;
                                plan.isApproved = false;
                                ColoredDebug.CLog(unit.gameObject, "<color=yellow>📝 РЕЗУЛЬТАТ ПРОВЕРКИ:</color> ОТКЛОНЕНО", _ColoredDebug);
                            }
                        }
                        else
                        {
                            if (!plan.isApproved) plan.isApproved = true;
                        }
                    }
                    else
                    {
                        ColoredDebug.CLog(unit.gameObject, "<color=red>❌ БЛОКИРОВКА (Статично):</color> {0} занята статичным объектом/юнитом ({1})", _ColoredDebug, plan.to, occupantName);
                        if (unitsWithConflicts.Add(unit))
                        {
                            changesMadeThisIteration = true;
                            plan.isApproved = false;
                            ColoredDebug.CLog(unit.gameObject, "<color=yellow>📝 РЕЗУЛЬТАТ ПРОВЕРКИ:</color> ОТКЛОНЕНО", _ColoredDebug);
                        }
                    }
                }
                else
                {
                    if (!plan.isApproved) plan.isApproved = true;
                }
            }
        } while (changesMadeThisIteration && currentIteration < maxIterations);


        if (currentIteration >= maxIterations)
        {
            ColoredDebug.CLog(gameObject, "<color=red>⚠️ ПРЕРЫВАНИЕ ЦЕПОЧЕК:</color> Достигнут лимит итераций ({0}). Возможна циклическая зависимость!", true, maxIterations);
            foreach (var kvp in plans)
            {
                if (!kvp.Value.isApproved && !unitsWithConflicts.Contains(kvp.Key))
                {
                    unitsWithConflicts.Add(kvp.Key);
                    kvp.Value.isApproved = false;
                    ColoredDebug.CLog(kvp.Key.gameObject, "<color=red>❌ ОТМЕНА (Лимит итераций):</color> Ход {0} -> {1} отменен.", _ColoredDebug, kvp.Value.from, kvp.Value.to);
                }
            }
        }

        foreach (var unit in unitsWithConflicts)
        {
            if (plans.TryGetValue(unit, out var plan))
            {
                plan.isApproved = false;
            }
        }

        ColoredDebug.CLog(gameObject, "<color=yellow>🏁 РАЗРЕШЕНИЕ КОНФЛИКТОВ ЗАВЕРШЕНО:</color> Одобрено планов: {0}", _ColoredDebug, plans.Count(kvp => kvp.Value.isApproved));
    }
    #endregion Метод разрешения конфликтов
}