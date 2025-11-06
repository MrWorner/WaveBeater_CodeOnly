// НАЗНАЧЕНИЕ: Шаблон поведения, который выполняет действия в соответствии с заданным списком приоритетов.
// Выбирает первое действие из списка, которое может быть выполнено. Включает логику обхода препятствий.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: AIAction, BattleUnit, Pathfinding.
// ПРИМЕЧАНИЕ: Сначала проверяет возможность стандартного пути (A*). Если путь заблокирован, пытается использовать действия-обходы (Прыжок, Ремонт, Строительство), затем простой горизонтальный шаг, и только потом остальные действия.
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "B_PrioritizedAction", menuName = "AI/Behavior Patterns/Prioritized Action")]
public class PrioritizedActionPattern : AIBehaviorPattern
{
    #region Поля: Required
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Список действий, отсортированный по приоритету (от высшего к низшему).")]
    public List<AIAction> prioritizedActions;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Выбирает действие на основе приоритетов, учитывая доступность пути A*.
    /// </summary>
    /// <param name="performer">Юнит, принимающий решение.</param>
    /// <param name="availableActions">Список действий, прошедших базовую проверку CanExecute (ОД, лимиты).</param>
    /// <returns>Выбранное действие или null.</returns>
    public override AIAction DecideAction(BattleUnit performer, List<AIAction> availableActions)
    {
        if (prioritizedActions == null || !prioritizedActions.Any() || !availableActions.Any())
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>PrioritizedActionPattern:</color> Нет приоритетных или доступных действий.", _ColoredDebug);
            return null;
        }

        // --- Этап 1: Проверка ПРЯМОГО пути ---
        // ИСПОЛЬЗУЕМ ИСПРАВЛЕННУЮ ЛОГИКУ:
        // Проверяем, может ли FindBestMove (который теперь запрещает ход назад) найти ход,
        // который не является "стоять на месте".
        var target = performer.TargetingSystem?.GetBestTarget();
        bool canMoveForward = false;
        if (target != null)
        {
            Vector2Int bestMove = performer.Movement.FindBestMove(performer);
            canMoveForward = bestMove != performer.CurrentPosition;
            ColoredDebug.CLog(performer.gameObject, "<color=yellow>PrioritizedActionPattern:</color> Проверка прямого пути. Цель: {0}. Возможен ход (вперед/вбок): <color={1}>{2}</color>.", _ColoredDebug, target.name, canMoveForward ? "lime" : "red", canMoveForward);
        }
        else
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>PrioritizedActionPattern:</color> Цель не найдена, проверка пути невозможна.", _ColoredDebug);
        }


        // --- Этап 2: Выбор действия на основе наличия пути ---

        if (canMoveForward)
        {
            // --- Сценарий 1: Путь (вперед/вбок) СУЩЕСТВУЕТ ---
            // Ищем первое приоритетное действие, которое доступно
            ColoredDebug.CLog(performer.gameObject, "<color=yellow>PrioritizedActionPattern:</color> Путь ЕСТЬ. Проверяю стандартные приоритеты...", _ColoredDebug);
            foreach (var action in prioritizedActions)
            {
                // Пропускаем действия-обходы, если путь есть
                // ДОБАВЛЕНО: DestroyPropAction также является обходом
                if (action is JumpAction || action is RepairHoleAction || action is BuildBridgeAction || action is DestroyPropAction || action is MoveHorizontallyAction)
                {
                    continue;
                }


                // Проверяем, есть ли действие в списке *изначально* доступных (проверка ОД, лимитов)
                // И дополнительно вызываем CanExecute (на случай, если CanExecute зависит от чего-то еще, кроме ОД/лимитов)
                if (availableActions.Contains(action) && action.CanExecute(performer, performer.Brain.ActionPointsLeft))
                {
                    ColoredDebug.CLog(performer.gameObject, "<color=lime>PrioritizedActionPattern:</color> Выбрано (Путь ЕСТЬ): <color=white>'{0}'</color>.", _ColoredDebug, action.actionName);
                    return action;
                }
                //else if(availableActions.Contains(action)) {
                //    ColoredDebug.CLog(performer.gameObject, "<color=grey>PrioritizedActionPattern:</color> Действие '{0}' доступно по ОД/лимиту, но CanExecute = false.", _ColoredDebug, action.actionName);
                //}
            }
            ColoredDebug.CLog(performer.gameObject, "<color=orange>PrioritizedActionPattern:</color> Путь есть, но ни одно приоритетное действие (кроме обходов) не прошло CanExecute.", _ColoredDebug);
        }
        else
        {
            // --- Сценарий 2: Путь (вперед/вбок) НЕ НАЙДЕН ---
            // Сначала ищем действия-обходы
            ColoredDebug.CLog(performer.gameObject, "<color=yellow>PrioritizedActionPattern:</color> Путь (вперед/вбок) НЕ НАЙДЕН. Проверяю действия-обходы...", _ColoredDebug);
            foreach (var action in prioritizedActions)
            {
                // Ищем только обходы
                // ДОБАВЛЕНО: DestroyPropAction также является обходом
                if (action is JumpAction || action is RepairHoleAction || action is BuildBridgeAction || action is DestroyPropAction || action is MoveHorizontallyAction)
                {
                    if (availableActions.Contains(action) && action.CanExecute(performer, performer.Brain.ActionPointsLeft))
                    {
                        ColoredDebug.CLog(performer.gameObject, "<color=lime>PrioritizedActionPattern:</color> Выбрано (Путь ЗАБЛОКИРОВАН): <color=white>'{0}'</color>.", _ColoredDebug, action.actionName);
                        return action;
                    }
                    // else if(availableActions.Contains(action)) {
                    //    ColoredDebug.CLog(performer.gameObject, "<color=grey>PrioritizedActionPattern:</color> Обход '{0}' доступен по ОД/лимиту, но CanExecute = false.", _ColoredDebug, action.actionName);
                    // }
                }
            }

            // Если обходы не сработали, проверяем остальные действия (например, атака с места, спец. способность)
            ColoredDebug.CLog(performer.gameObject, "<color=yellow>PrioritizedActionPattern:</color> Обходы не сработали. Проверяю остальные действия...", _ColoredDebug);
            foreach (var action in prioritizedActions)
            {
                // Пропускаем обходы И стандартное движение
                // ДОБАВЛЕНО: DestroyPropAction также является обходом
                if (action is JumpAction || action is RepairHoleAction || action is BuildBridgeAction || action is DestroyPropAction || action is MoveHorizontallyAction || action is MoveTowardsTargetAction)
                {
                    continue;
                }

                if (availableActions.Contains(action) && action.CanExecute(performer, performer.Brain.ActionPointsLeft))
                {
                    ColoredDebug.CLog(performer.gameObject, "<color=lime>PrioritizedActionPattern:</color> Выбрано (Путь ЗАБЛОКИРОВАН, Обходы НЕ СРАБОТАЛИ): <color=white>'{0}'</color>.", _ColoredDebug, action.actionName);
                    return action;
                }
                // else if(availableActions.Contains(action)) {
                //    ColoredDebug.CLog(performer.gameObject, "<color=grey>PrioritizedActionPattern:</color> Действие '{0}' доступно по ОД/лимиту, но CanExecute = false.", _ColoredDebug, action.actionName);
                // }
            }
            ColoredDebug.CLog(performer.gameObject, "<color=orange>PrioritizedActionPattern:</color> Путь не найден, обходы не сработали, остальные действия тоже не прошли CanExecute.", _ColoredDebug);
        }


        // Если ничего не выбрано ни в одном из сценариев
        ColoredDebug.CLog(performer.gameObject, "<color=red>PrioritizedActionPattern:</color> Ни одно действие не выбрано.", _ColoredDebug);
        return null;
    }

    /// <summary>
    /// Находит ближайшую к юниту точку на "теле" цели для расчета пути.
    /// </summary>
    private Vector2Int GetClosestPointOnTarget(BattleUnit performer, BattleUnit target)
    {
        if (target.Movement == null || target.Movement.OccupiedCells == null || target.Movement.OccupiedCells.Count == 0)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=yellow>PrioritizedActionPattern (GetClosestPoint):</color> Не могу получить занятые клетки цели <color=white>{0}</color>, использую CurrentPosition {1}", _ColoredDebug, target.name, target.CurrentPosition);
            return target.CurrentPosition;
        }

        Vector2Int bestTargetPoint = target.CurrentPosition;
        int minDistance = int.MaxValue;
        foreach (var targetCell in target.Movement.OccupiedCells)
        {
            if (targetCell == null) continue;
            int dist = BattleGridUtils.GetDistance(performer.CurrentPosition, performer.Stats.UnitSize, targetCell.Position, Vector2Int.one);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestTargetPoint = targetCell.Position;
            }
        }
        //ColoredDebug.CLog(performer.gameObject, "<color=grey>PrioritizedActionPattern (GetClosestPoint):</color> Ближайшая точка на цели <color=white>{0}</color>: <color=yellow>{1}</color> (дистанция: {2})", _ColoredDebug, target.name, bestTargetPoint, minDistance);
        return bestTargetPoint;
    }
    #endregion Публичные методы
}