// НАЗНАЧЕНИЕ: Определяет действие AI для перемещения юнита к его текущей цели. Использует Pathfinding для нахождения пути и выполняет один шаг.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleUnitMovement, Pathfinding, BattleGridUtils, BattleUnitTargetingSystem, AIBehaviorPattern.
// ПРИМЕЧАНИЕ: Решение о том, двигаться или атаковать/использовать спец. действие, принимается AIBehaviorPattern. Это действие просто проверяет, есть ли путь.
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[CreateAssetMenu(fileName = "AA_MoveTowardsTarget", menuName = "AI/Actions/Move Towards Target")]
public class MoveTowardsTargetAction : AIAction
{
    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, может ли юнит сделать ход по направлению к цели.
    /// </summary>
    /// <remarks>
    /// Возвращает true только если есть цель и Pathfinding находит путь, следующий шаг которого отличается от текущей позиции.
    /// </remarks>
    /// <param name="performer">Юнит, выполняющий проверку.</param>
    /// <param name="actionPointsLeft">Оставшиеся очки действий.</param>
    /// <returns>True, если движение к цели возможно.</returns>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        // 1. Проверка ОД и базовых триггеров
        if (actionPointsLeft < actionPointCost)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveAction:</color> Проверка CanExecute. Недостаточно ОД ({0}/{1}).", _ColoredDebug, actionPointsLeft, actionPointCost);
            return false;
        }
        if (!AreTriggersMet(performer))
        {
            ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveAction:</color> Проверка CanExecute. Базовые триггеры не выполнены.", _ColoredDebug);
            return false;
        }


        // 2. Проверка наличия цели
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null)
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveAction:</color> Проверка CanExecute. Нет цели.", _ColoredDebug);
            return false;
        }

        // 3. Проверка, может ли Pathfinding найти лучший ход
        bool canFindBetterMove = performer.Movement.FindBestMove(performer) != performer.CurrentPosition;

        //ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveAction:</color> Проверка CanExecute. Цель: {0}. CanFindBetterMove: {1}. ShouldMove = {1}.", _ColoredDebug, target.name, canFindBetterMove);
        return canFindBetterMove;
    }

    /// <summary>
    /// Выполняет один шаг по направлению к цели (логика перемещения делегируется TurnManager).
    /// Если путь не найден или заблокирован союзником, ожидает стандартное время действия.
    /// </summary>
    /// <param name="performer">Двигающийся юнит.</param>
    /// <param name="onComplete">Действие, вызываемое по завершении шага или ожидания.</param>
    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>MoveAction:</color> Цель потеряна во время выполнения Execute. Завершаю действие.", _ColoredDebug);
            onComplete?.Invoke();
            return;
        }

        Vector2Int nextStep = performer.Movement.FindBestMove(performer);
        bool canMove = nextStep != performer.CurrentPosition;

        if (canMove)
        {
            BattleCell nextCell = BattleGrid.Instance.GetCell(nextStep);
            bool blockedByAlly = false;
            if (nextCell != null && nextCell.IsOccupied())
            {
                if (nextCell.Occupant is BattleUnit occupantUnit && occupantUnit.FactionType == performer.FactionType)
                {
                    blockedByAlly = true;
                }
            }

            if (blockedByAlly)
            {
                ColoredDebug.CLog(performer.gameObject, "<color=orange>MoveAction:</color> Следующий шаг <color=yellow>{0}</color> заблокирован союзником <color=white>{1}</color>. Юнит ждет.", _ColoredDebug, nextStep, (nextCell.Occupant as BattleUnit)?.name ?? "UNKNOWN");
                BattleLogger.Instance.LogAction(performer, actionName, $"Blocked by ally at {performer.CurrentPosition}");
                performer.StartCoroutine(WaitCoroutine(onComplete));
            }
            else
            {
                ColoredDebug.CLog(performer.gameObject, "<color=lime>MoveAction:</color> Планирую ход на позицию <color=yellow>{0}</color> (будет обработано TurnManager).", _ColoredDebug, nextStep);
                onComplete?.Invoke();
            }
        }
        else
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>MoveAction:</color> Путь к цели не найден или движение не требуется. Юнит ждет.", _ColoredDebug);
            BattleLogger.Instance.LogAction(performer, actionName, $"Blocked or no path, waiting at {performer.CurrentPosition}");
            performer.StartCoroutine(WaitCoroutine(onComplete));
        }
    }
    #endregion Публичные методы

    #region Личные методы
    /// <summary>
    /// Корутина для ожидания стандартного времени хода.
    /// </summary>
    private IEnumerator WaitCoroutine(UnityAction onComplete)
    {
        yield return new WaitForSeconds(0.1f);
        onComplete?.Invoke();
    }
    #endregion Личные методы
}