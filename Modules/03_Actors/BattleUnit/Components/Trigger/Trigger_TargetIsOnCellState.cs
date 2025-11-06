// НАЗНАЧЕНИЕ: Триггер срабатывает, если ЦЕЛЬ (Герой) стоит на клетке с определенным состоянием.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: BattleUnit, BattleCell.
// ПРИМЕЧАНИЕ: Позволяет ИИ использовать тактические приемы, основанные на уязвимом положении цели.
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "T_TargetIsOnCellState", menuName = "AI/Triggers/Target Is On Cell State")]
public class Trigger_TargetIsOnCellState : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private BattleCell.CellState _requiredState = BattleCell.CellState.Cracked;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет состояние клеток, на которых стоит цель (Герой).
    /// </summary>
    public override bool IsTriggered(BattleUnit performer)
    {
        BattleUnit target = BattleUnit.Hero;
        if (target == null || target.Movement.OccupiedCells == null) return false;

        // Триггер сработает, если ХОТЯ БЫ ОДНА клетка под целью соответствует состоянию
        bool targetIsOnRequiredState = target.Movement.OccupiedCells.Any(cell => cell != null && cell.CurrentState == _requiredState);

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка состояния клетки под ЦЕЛЬЮ (Требуется: {0}). Результат: <color=yellow>{1}</color>.", false, _requiredState, targetIsOnRequiredState);
        return targetIsOnRequiredState;
    }
    #endregion Публичные методы
}