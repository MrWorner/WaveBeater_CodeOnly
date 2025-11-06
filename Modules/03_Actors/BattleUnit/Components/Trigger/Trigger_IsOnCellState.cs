// НАЗНАЧЕНИЕ: Триггер срабатывает, если юнит стоит на клетке с определенным состоянием (целая, треснувшая, дыра).
// ОСНОВНЫЕ ЗАВИСИМОСТИ: BattleUnit, BattleCell.
// ПРИМЕЧАНИЕ: Полезно для создания ИИ, который реагирует на опасность под ногами.
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "T_IsOnCellState", menuName = "AI/Triggers/Is On Cell State")]
public class Trigger_IsOnCellState : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private BattleCell.CellState _requiredState = BattleCell.CellState.Cracked;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет состояние клеток, на которых стоит юнит.
    /// </summary>
    public override bool IsTriggered(BattleUnit performer)
    {
        if (performer.Movement.OccupiedCells == null) return false;

        // Триггер сработает, если ХОТЯ БЫ ОДНА клетка под юнитом соответствует состоянию
        bool isOnRequiredState = performer.Movement.OccupiedCells.Any(cell => cell != null && cell.CurrentState == _requiredState);

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка состояния клетки под собой (Требуется: {0}). Результат: <color=yellow>{1}</color>.", false, _requiredState, isOnRequiredState);
        return isOnRequiredState;
    }
    #endregion Публичные методы
}