// НАЗНАЧЕНИЕ: Действие ИИ для восстановления разрушенной клетки (дыры) перед собой.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnitMovement, Pathfinding, BattleGrid, BattleCell.
// ПРИМЕЧАНИЕ: Срабатывает, когда прямой путь заблокирован дырой (проверяется в BehaviorPattern). Восстанавливает клетку до определенного типа (напр., Indestructible).
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "AA_RepairHole", menuName = "AI/Actions/Repair Hole")]
public class RepairHoleAction : AIAction
{
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Тип клетки, в который будет превращена дыра."), SerializeField]
    private BattleCell.CellType _repairedCellType = BattleCell.CellType.Indestructible; ///Тип восстановленной клетки.
    [BoxGroup("SETTINGS"), Tooltip("Длительность анимации починки (если есть)."), SerializeField]
    private float _repairAnimationDuration = 0.5f; ///Длительность анимации.
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, находится ли перед юнитом ПОВРЕЖДЕННАЯ дыра (не пропасть).
    /// </summary>
    /// <param name="performer">Юнит, выполняющий проверку.</param>
    /// <param name="actionPointsLeft">Оставшиеся очки действий.</param>
    /// <returns>True, если починка возможна.</returns>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        // 1. Проверка ОД и триггеров
        if (actionPointsLeft < actionPointCost) return false;
        if (!AreTriggersMet(performer)) return false;

        // 2. Проверка наличия цели (нужна для определения направления)
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null) return false;

        // 3. Определяем направление
        Vector2Int moveDirection = Vector2Int.zero;
        int deltaX = target.CurrentPosition.x - performer.CurrentPosition.x;
        int deltaY = target.CurrentPosition.y - performer.CurrentPosition.y;
        if (deltaX != 0) moveDirection = new Vector2Int(System.Math.Sign(deltaX), 0);
        else if (deltaY != 0) moveDirection = new Vector2Int(0, System.Math.Sign(deltaY));
        else return false;

        // 4. Проверяем клетку прямо перед юнитом
        Vector2Int cellInFrontPos = performer.CurrentPosition + moveDirection;
        BattleCell cellInFront = BattleGrid.Instance.GetCell(cellInFrontPos);

        if (cellInFront != null && cellInFront.CurrentState == BattleCell.CellState.Hole && !cellInFront.IsDeactivated)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>RepairHoleAction:</color> Проверка CanExecute. Перед юнитом на {0} находится РАЗРУШЕННАЯ дыра. Починка ВОЗМОЖНА.", _ColoredDebug, cellInFrontPos);
            return true;
        }
        else
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=orange>RepairHoleAction:</color> Проверка CanExecute. Клетка {0} не является разрушенной дырой (State: {1}, Deactivated: {2}). Починка невозможна.", _ColoredDebug, cellInFrontPos, cellInFront?.CurrentState.ToString() ?? "NULL", cellInFront?.IsDeactivated.ToString() ?? "N/A");
            return false;
        }
    }

    /// <summary>
    /// Выполняет починку дыры: изменяет состояние клетки и проигрывает анимацию.
    /// </summary>
    /// <param name="performer">Юнит, выполняющий починку.</param>
    /// <param name="onComplete">Действие, вызываемое по завершении.</param>
    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null) { onComplete?.Invoke(); return; }

        Vector2Int moveDirection = Vector2Int.zero;
        int deltaX = target.CurrentPosition.x - performer.CurrentPosition.x;
        int deltaY = target.CurrentPosition.y - performer.CurrentPosition.y;
        if (deltaX != 0) moveDirection = new Vector2Int(System.Math.Sign(deltaX), 0);
        else if (deltaY != 0) moveDirection = new Vector2Int(0, System.Math.Sign(deltaY));

        Vector2Int cellToRepairPos = performer.CurrentPosition + moveDirection;
        BattleCell cellToRepair = BattleGrid.Instance.GetCell(cellToRepairPos);

        if (cellToRepair != null && cellToRepair.CurrentState == BattleCell.CellState.Hole && !cellToRepair.IsDeactivated)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>RepairHoleAction:</color> Выполнение починки клетки {0}.", _ColoredDebug, cellToRepairPos);
            BattleLogger.Instance.LogAction(performer, actionName, $"Repair Hole at {cellToRepairPos}");

            Sequence repairSequence = DOTween.Sequence();
            // TODO: Add repair animation trigger here if needed
            // performer.Animator.PlayRepairAnimation();
            repairSequence.AppendInterval(_repairAnimationDuration);
            repairSequence.OnComplete(() => {
                cellToRepair.RepairCell(_repairedCellType);
                ColoredDebug.CLog(performer.gameObject, "<color=green>RepairHoleAction:</color> Клетка {0} восстановлена до типа {1}.", _ColoredDebug, cellToRepairPos, _repairedCellType);
                onComplete?.Invoke();
            });
        }
        else
        {
            ColoredDebug.CLog(performer.gameObject, "<color=red>RepairHoleAction:</color> Execute. Клетка {0} не найдена или не является РАЗРУШЕННОЙ дырой! Починка отменена.", _ColoredDebug, cellToRepairPos);
            onComplete?.Invoke();
        }
    }

    /// <summary> Находит ближайшую точку на цели. </summary>
    private Vector2Int GetClosestPointOnTarget(BattleUnit performer, BattleUnit target)
    {
        if (target.Movement == null || target.Movement.OccupiedCells == null || target.Movement.OccupiedCells.Count == 0)
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=yellow>RepairHoleAction (GetClosestPoint):</color> Не могу получить занятые клетки цели <color=white>{0}</color>, использую CurrentPosition {1}", _ColoredDebug, target.name, target.CurrentPosition);
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
        //ColoredDebug.CLog(performer.gameObject, "<color=grey>RepairHoleAction (GetClosestPoint):</color> Ближайшая точка на цели <color=white>{0}</color>: <color=yellow>{1}</color> (дистанция: {2})", _ColoredDebug, target.name, bestTargetPoint, minDistance);
        return bestTargetPoint;
    }
    #endregion Публичные методы
}