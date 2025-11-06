// НАЗНАЧЕНИЕ: Действие ИИ для создания моста (восстановления клетки) над пропастью (отсутствующей клеткой) перед собой.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnitMovement, Pathfinding, BattleGrid, BattleCell.
// ПРИМЕЧАНИЕ: Срабатывает, когда прямой путь заблокирован отсутствующей клеткой (Abyss) (проверяется в BehaviorPattern). Создает клетку определенного типа.
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "AA_BuildBridge", menuName = "AI/Actions/Build Bridge")]
public class BuildBridgeAction : AIAction
{
    #region Поля: Required
    // No specific required fields
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Тип клетки, которая будет создана."), SerializeField]
    private BattleCell.CellType _builtCellType = BattleCell.CellType.Indestructible; ///Тип создаваемой клетки.
    [BoxGroup("SETTINGS"), Tooltip("Длительность анимации строительства (если есть)."), SerializeField]
    private float _buildAnimationDuration = 0.7f; ///Длительность анимации.
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, находится ли перед юнитом отсутствующая (null) или деактивированная (Abyss) клетка.
    /// НЕ проверяет, заблокирован ли полный путь A* (это делает BehaviorPattern).
    /// </summary>
    /// <param name="performer">Юнит, выполняющий проверку.</param>
    /// <param name="actionPointsLeft">Оставшиеся очки действий.</param>
    /// <returns>True, если строительство возможно.</returns>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        // 1. Проверка ОД и триггеров
        if (actionPointsLeft < actionPointCost) return false;
        if (!AreTriggersMet(performer)) return false;

        // 2. Проверка наличия цели
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

        if (cellInFront == null || cellInFront.IsDeactivated)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>BuildBridgeAction:</color> Проверка CanExecute. Перед юнитом на {0} пропасть (null или Deactivated). Строительство ВОЗМОЖНО.", _ColoredDebug, cellInFrontPos);
            return true;
        }
        else
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=orange>BuildBridgeAction:</color> Проверка CanExecute. Клетка {0} существует и активна (State: {1}). Строительство невозможно.", _ColoredDebug, cellInFrontPos, cellInFront.CurrentState);
            return false;
        }
    }

    /// <summary>
    /// Выполняет строительство моста: восстанавливает деактивированную клетку и изменяет ее тип.
    /// </summary>
    /// <param name="performer">Юнит, выполняющий строительство.</param>
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

        Vector2Int cellToBuildPos = performer.CurrentPosition + moveDirection;
        BattleCell cellToBuild = BattleGrid.Instance.GetCell(cellToBuildPos);

        if (cellToBuild != null && cellToBuild.IsDeactivated) // Строим только на ДЕАКТИВИРОВАННЫХ
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>BuildBridgeAction:</color> Выполнение строительства моста на {0}.", _ColoredDebug, cellToBuildPos);
            BattleLogger.Instance.LogAction(performer, actionName, $"Build Bridge at {cellToBuildPos}");

            Sequence buildSequence = DOTween.Sequence();
            // TODO: Add build animation trigger here if needed
            // performer.Animator.PlayBuildAnimation();
            buildSequence.AppendInterval(_buildAnimationDuration);
            buildSequence.OnComplete(() => {
                cellToBuild.BuildCell(_builtCellType);
                ColoredDebug.CLog(performer.gameObject, "<color=green>BuildBridgeAction:</color> Клетка {0} построена с типом {1}.", _ColoredDebug, cellToBuildPos, _builtCellType);
                onComplete?.Invoke();
            });
        }
        else if (cellToBuild == null && BattleGrid.Instance != null && BattleGridGenerator.Instance != null)
        {
            // Проверяем, находится ли позиция в пределах ожидаемых границ генератора
            if (cellToBuildPos.x >= 0 && cellToBuildPos.x < BattleGridGenerator.Instance.Width && cellToBuildPos.y >= 0 && cellToBuildPos.y < BattleGridGenerator.Instance.Height)
            {
                ColoredDebug.CLog(performer.gameObject, "<color=lime>BuildBridgeAction:</color> Выполнение строительства НОВОЙ клетки на {0}.", _ColoredDebug, cellToBuildPos);
                BattleLogger.Instance.LogAction(performer, actionName, $"Build NEW Bridge Cell at {cellToBuildPos}");

                Sequence buildSequence = DOTween.Sequence();
                buildSequence.AppendInterval(_buildAnimationDuration);
                buildSequence.OnComplete(() => {
                    // Нужно как-то создать и инициализировать новую клетку здесь.
                    // Это выходит за рамки простого изменения состояния и требует
                    // взаимодействия с BattleGridGenerator или BattleGrid.
                    // ВРЕМЕННОЕ РЕШЕНИЕ: Логгируем, но не создаем.
                    Debug.LogError($"[BuildBridgeAction] Attempted to build a NEW cell at {cellToBuildPos}, but functionality is not fully implemented yet!");
                    // TODO: Implement actual cell creation and registration in BattleGrid
                    ColoredDebug.CLog(performer.gameObject, "<color=orange>BuildBridgeAction:</color> Создание НОВОЙ клетки на {0} пока не реализовано.", _ColoredDebug, cellToBuildPos);
                    onComplete?.Invoke();
                });
            }
            else
            {
                ColoredDebug.CLog(performer.gameObject, "<color=red>BuildBridgeAction:</color> Execute. Позиция {0} вне ожидаемых границ сетки! Строительство отменено.", _ColoredDebug, cellToBuildPos);
                onComplete?.Invoke();
            }
        }
        else
        {
            ColoredDebug.CLog(performer.gameObject, "<color=red>BuildBridgeAction:</color> Execute. Клетка {0} не является пропастью (null или Deactivated)! Строительство отменено.", _ColoredDebug, cellToBuildPos);
            onComplete?.Invoke();
        }
    }

    /// <summary> Находит ближайшую точку на цели. </summary>
    private Vector2Int GetClosestPointOnTarget(BattleUnit performer, BattleUnit target)
    {
        if (target.Movement == null || target.Movement.OccupiedCells == null || target.Movement.OccupiedCells.Count == 0)
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=yellow>BuildBridgeAction (GetClosestPoint):</color> Не могу получить занятые клетки цели <color=white>{0}</color>, использую CurrentPosition {1}", _ColoredDebug, target.name, target.CurrentPosition);
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
        //ColoredDebug.CLog(performer.gameObject, "<color=grey>BuildBridgeAction (GetClosestPoint):</color> Ближайшая точка на цели <color=white>{0}</color>: <color=yellow>{1}</color> (дистанция: {2})", _ColoredDebug, target.name, bestTargetPoint, minDistance);
        return bestTargetPoint;
    }
    #endregion Публичные методы
}