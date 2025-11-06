using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "AA_Jump", menuName = "AI/Actions/Jump Over Gap")]
public class JumpAction : AIAction
{
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Максимальное расстояние прыжка в клетках (1 = через одну клетку)."), MinValue(1), SerializeField]
    private int _maxJumpDistance = 1; ///Максимальное расстояние прыжка.
    [BoxGroup("SETTINGS"), Tooltip("Длительность анимации прыжка (приблизительная)."), SerializeField]
    private float _jumpAnimationDuration = 0.5f; ///Длительность анимации.
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug; //Должен быть в каждом классе
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, есть ли перед юнитом пропасть подходящего размера и можно ли приземлиться за ней.
    /// НЕ проверяет, заблокирован ли полный путь A* (это делает BehaviorPattern).
    /// </summary>
    /// <param name="performer">Юнит, выполняющий проверку.</param>
    /// <param name="actionPointsLeft">Оставшиеся очки действий.</param>
    /// <returns>True, если прыжок возможен.</returns>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        // 1. Проверка базовых условий (ОД, триггеры)
        if (actionPointsLeft < actionPointCost) return false;
        if (!AreTriggersMet(performer)) return false;

        // 2. Проверка наличия цели (нужна для определения направления)
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null) return false;

        // 3. Находим позицию для приземления (если возможно)
        Vector2Int landingPos = FindLandingPosition(performer, target, out int foundGapSize);
        // 4. Возвращаем результат
        bool canJump = landingPos != Vector2Int.zero; // Если позиция не нулевая, значит нашли
        if (canJump)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>JumpAction:</color> Проверка CanExecute. Найдена точка приземления {0} за пропастью {1} кл. Прыжок ВОЗМОЖЕН.", _ColoredDebug, landingPos, foundGapSize);
        }
        else
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=orange>JumpAction:</color> Проверка CanExecute. Не найдено подходящей пропасти/места приземления в пределах {0} кл.", _ColoredDebug, _maxJumpDistance);
        }
        return canJump;
    }

    /// <summary>
    /// Выполняет прыжок: анимирует и перемещает юнит на клетку приземления.
    /// </summary>
    /// <param name="performer">Юнит, выполняющий прыжок.</param>
    /// <param name="onComplete">Действие, вызываемое по завершении прыжка.</param>
    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>JumpAction:</color> Execute. Цель потеряна. Прыжок отменен.", _ColoredDebug);
            onComplete?.Invoke(); return;
        }

        Vector2Int landingPosition = FindLandingPosition(performer, target, out int gapSize);

        if (landingPosition == Vector2Int.zero) // Если точка приземления НЕ найдена
        {
            // Эта ветка теперь не должна выполняться, если CanExecute прошло успешно, но оставим для безопасности
            ColoredDebug.CLog(performer.gameObject, "<color=red>JumpAction:</color> Execute. Логическая ошибка! Не удалось найти точку приземления, хотя CanExecute разрешил. Прыжок отменен.", _ColoredDebug);
            onComplete?.Invoke();
            return;
        }

        ColoredDebug.CLog(performer.gameObject, "<color=lime>JumpAction:</color> Выполнение прыжка на {0} через {1} кл.", _ColoredDebug, landingPosition, gapSize);
        BattleLogger.Instance.LogAction(performer, actionName, $"Jump to {landingPosition}");

        BattleCell targetCell = BattleGrid.Instance.GetCell(landingPosition);
        if (targetCell == null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=red>JumpAction:</color> Execute. Целевая клетка {0} не найдена в сетке! Прыжок отменен.", _ColoredDebug, landingPosition);
            onComplete?.Invoke();
            return;
        }

        Vector3 currentWorldPos = performer.transform.position;
        Vector3 targetWorldPos = BattleGridUtils.GetAreaCenterInWorldSpace(landingPosition, performer.Stats.UnitSize);
        float jumpHeight = 1.0f;

        performer.Movement.ClearOccupation();

        Sequence jumpSequence = DOTween.Sequence();
        jumpSequence.Append(performer.transform.DOJump(targetWorldPos, jumpHeight, 1, _jumpAnimationDuration).SetEase(Ease.OutQuad));
        jumpSequence.OnComplete(() =>
        {
            performer.Movement.Initialize(targetCell); // Re-initialize movement (occupies new cells)
            ColoredDebug.CLog(performer.gameObject, "<color=green>JumpAction:</color> Прыжок завершен. Новая позиция: {0}.", _ColoredDebug, landingPosition);
            onComplete?.Invoke();
        });
    }

    /// <summary> Находит ближайшую точку на цели. </summary>
    private Vector2Int GetClosestPointOnTarget(BattleUnit performer, BattleUnit target)
    {
        if (target.Movement == null || target.Movement.OccupiedCells == null || target.Movement.OccupiedCells.Count == 0)
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=yellow>JumpAction (GetClosestPoint):</color> Не могу получить занятые клетки цели <color=white>{0}</color>, использую CurrentPosition {1}", _ColoredDebug, target.name, target.CurrentPosition);
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
        //ColoredDebug.CLog(performer.gameObject, "<color=grey>JumpAction (GetClosestPoint):</color> Ближайшая точка на цели <color=white>{0}</color>: <color=yellow>{1}</color> (дистанция: {2})", _ColoredDebug, target.name, bestTargetPoint, minDistance);
        return bestTargetPoint;
    }
    #endregion Публичные методы

    #region Личные методы (Логика поиска точки приземления)
    /// <summary>
    /// Находит валидную позицию для приземления после прыжка.
    /// </summary>
    /// <param name="performer">Прыгающий юнит.</param>
    /// <param name="target">Цель (для определения направления).</param>
    /// <param name="foundGapSize">Выходной параметр: размер обнаруженной пропасти.</param>
    /// <returns>Позицию для приземления или Vector2Int.zero, если прыжок невозможен.</returns>
    private Vector2Int FindLandingPosition(BattleUnit performer, BattleUnit target, out int foundGapSize)
    {
        foundGapSize = 0;
        BattleGrid grid = BattleGrid.Instance;
        if (grid == null) return Vector2Int.zero;

        // Определяем направление
        Vector2Int moveDirection = Vector2Int.zero;
        int deltaX = target.CurrentPosition.x - performer.CurrentPosition.x;
        int deltaY = target.CurrentPosition.y - performer.CurrentPosition.y;
        if (deltaX != 0) moveDirection = new Vector2Int(System.Math.Sign(deltaX), 0);
        else if (deltaY != 0) moveDirection = new Vector2Int(0, System.Math.Sign(deltaY));
        else return Vector2Int.zero;

        // Сначала проверяем все клетки, через которые нужно перепрыгнуть (от 1 до _maxJumpDistance)
        for (int checkDist = 1; checkDist <= _maxJumpDistance; checkDist++)
        {
            Vector2Int checkPos = performer.CurrentPosition + moveDirection * checkDist;
            BattleCell cellToCheck = grid.GetCell(checkPos);

            // Проверяем, есть ли на этой клетке непрыгаемый проп
            if (cellToCheck != null && cellToCheck.Occupant is GameObject propGO)
            {
                if (propGO.TryGetComponent<Prop>(out var propComp))
                {
                    if (propComp.PropSO != null && !propComp.PropSO.IsJumpable)
                    {
                        Debug.Log("<color=purple>IT WORKS!</color>", this);
                        ColoredDebug.CLog(performer.gameObject, "<color=orange>JumpAction:</color> Клетка {0} заблокирована НЕПРЫГАЕМЫМ пропом '{1}'. Прыжок невозможен.", _ColoredDebug, checkPos, propComp.PropSO.name);
                        return Vector2Int.zero; // Прыжок невозможен
                    }
                }
            }
        }

        // Ищем пропасть и точку приземления
        bool gapFound = false;
        for (int i = 1; i <= _maxJumpDistance + 1; i++)
        {
            Vector2Int checkPos = performer.CurrentPosition + moveDirection * i;
            // Проверяем проходимость ОДИНОЧНОЙ клетки (игнорируя юнитов)
            bool isCurrentCellWalkable = Pathfinding.IsSingleCellWalkableForUnit(checkPos, grid, null, performer.CurrentPosition, Vector2Int.zero, performer.name);

            if (i <= _maxJumpDistance) // Клетки *внутри* возможной пропасти
            {
                if (isCurrentCellWalkable && !gapFound)
                {
                    // Нашли проходимую клетку до начала пропасти - прыжок невозможен отсюда
                    return Vector2Int.zero;
                }
                if (!isCurrentCellWalkable)
                {
                    gapFound = true;
                    foundGapSize = i;
                }
            }
            else // Проверяем клетку *приземления* (i = foundGapSize + 1)
            {
                // Проверяем проходимость области приземления ДЛЯ ЮНИТА
                if (gapFound && isCurrentCellWalkable && Pathfinding.IsAreaWalkableForUnit(checkPos, performer.Stats.UnitSize, grid, performer, performer.CurrentPosition, Vector2Int.zero, performer.name))
                {
                    return checkPos;
                }
                else
                {
                    // Если клетка приземления непроходима или занята, прыжок невозможен
                    return Vector2Int.zero;
                }
            }
        }

        // Не нашли подходящей ситуации
        return Vector2Int.zero;
    }
    #endregion Личные методы (Логика поиска точки приземления)
}
