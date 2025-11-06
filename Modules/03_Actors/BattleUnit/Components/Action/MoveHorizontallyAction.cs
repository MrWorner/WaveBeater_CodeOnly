// НАЗНАЧЕНИЕ: Действие ИИ для простого перемещения на одну клетку по горизонтали в сторону цели.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnitMovement, Pathfinding, BattleGrid.
// ПРИМЕЧАНИЕ: Используется как базовое поведение, когда сложный поиск пути (A*) не находит маршрут, но клетка прямо по горизонтали доступна. Не проверяет наличие полного пути.
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "AA_MoveHorizontally", menuName = "AI/Actions/Move Horizontally Towards Target")]
public class MoveHorizontallyAction : AIAction
{
    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug; //Должен быть в каждом классе
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, есть ли цель и доступна ли клетка прямо по горизонтали в ее сторону.
    /// Не проверяет, заблокирован ли полный путь A*.
    /// </summary>
    /// <param name="performer">Юнит, выполняющий проверку.</param>
    /// <param name="actionPointsLeft">Оставшиеся очки действий.</param>
    /// <returns>True, если горизонтальный шаг возможен.</returns>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        // 1. Проверка ОД и триггеров
        if (actionPointsLeft < actionPointCost)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveHorizontallyAction:</color> Проверка CanExecute. Недостаточно ОД ({0}/{1}).", _ColoredDebug, actionPointsLeft, actionPointCost);
            return false;
        }
        if (!AreTriggersMet(performer))
        {
            ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveHorizontallyAction:</color> Проверка CanExecute. Базовые триггеры не выполнены.", _ColoredDebug);
            return false;
        }


        // 2. Проверка наличия цели
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null)
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveHorizontallyAction:</color> Проверка CanExecute. Нет цели.", _ColoredDebug);
            return false;
        }

        // 3. Определяем горизонтальное направление
        int deltaX = target.CurrentPosition.x - performer.CurrentPosition.x;
        if (deltaX == 0)
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=cyan>MoveHorizontallyAction:</color> Проверка CanExecute. Уже на целевой X-координате.", _ColoredDebug);
            return false; // Уже на нужной вертикали
        }
        Vector2Int horizontalDirection = new Vector2Int(System.Math.Sign(deltaX), 0);
        Vector2Int nextHorizontalPos = performer.CurrentPosition + horizontalDirection;

        // 4. Проверяем проходимость *только* следующей горизонтальной клетки
        BattleGrid grid = BattleGrid.Instance;
        bool isNextCellWalkable = Pathfinding.IsAreaWalkableForUnit(nextHorizontalPos, performer.Stats.UnitSize, grid, performer, performer.CurrentPosition, Vector2Int.zero, performer.name);

        if (isNextCellWalkable)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>MoveHorizontallyAction:</color> Проверка CanExecute. Горизонтальная клетка {0} доступна. Движение ВОЗМОЖНО.", _ColoredDebug, nextHorizontalPos);
            return true;
        }
        else
        {
            //ColoredDebug.CLog(performer.gameObject, "<color=orange>MoveHorizontallyAction:</color> Проверка CanExecute. Горизонтальная клетка {0} недоступна.", _ColoredDebug, nextHorizontalPos);
            return false;
        }
    }

    /// <summary>
    /// Выполняет один шаг по горизонтали (логика перемещения делегируется TurnManager).
    /// </summary>
    /// <param name="performer">Двигающийся юнит.</param>
    /// <param name="onComplete">Действие, вызываемое по завершении.</param>
    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var target = performer.TargetingSystem?.GetBestTarget();
        if (target == null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>MoveHorizontallyAction:</color> Execute. Цель потеряна. Завершаю действие.", _ColoredDebug);
            onComplete?.Invoke();
            return;
        }

        int deltaX = target.CurrentPosition.x - performer.CurrentPosition.x;
        if (deltaX == 0)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>MoveHorizontallyAction:</color> Execute. Уже на целевой X. Завершаю действие.", _ColoredDebug);
            onComplete?.Invoke();
            return;
        }
        Vector2Int horizontalDirection = new Vector2Int(System.Math.Sign(deltaX), 0);
        Vector2Int nextHorizontalPos = performer.CurrentPosition + horizontalDirection;

        // Важно: Мы *не* проверяем проходимость здесь снова в Execute,
        // предполагая, что CanExecute было вызвано непосредственно перед этим.
        // TurnManager отвечает за фактическое выполнение и обработку возможных конфликтов.

        ColoredDebug.CLog(performer.gameObject, "<color=lime>MoveHorizontallyAction:</color> Выполняю горизонтальный ход на позицию <color=yellow>{0}</color> (будет обработано TurnManager).", _ColoredDebug, nextHorizontalPos);

        // --- ВАЖНО: Как и в MoveTowardsTargetAction, мы не вызываем MoveTo отсюда ---
        // TurnManager должен будет определить, что это действие было выбрано
        // и использовать nextHorizontalPos для своего MovementPlan.

        // Сообщаем AIBrain, что действие (запрос на движение) выполнено.
        onComplete?.Invoke();
    }
    #endregion Публичные методы
}