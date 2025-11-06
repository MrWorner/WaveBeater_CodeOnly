// НАЗНАЧЕНИЕ: Триггер срабатывает, если цель (Герой) находится в соседней клетке.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleGridUtils.
// ПРИМЕЧАНИЕ: Учитывает размер юнитов и проверяет соседство по всем занимаемым клеткам.

using UnityEngine;

[CreateAssetMenu(fileName = "T_TargetIsAdjacent", menuName = "AI/Triggers/Target Is Adjacent")]
public class Trigger_TargetIsAdjacent : ActionTrigger
{
    public override bool IsTriggered(BattleUnit performer)
    {
        if (BattleUnit.Hero == null) return false;

        bool areAdjacent = BattleGridUtils.AreUnitsAdjacent(performer, BattleUnit.Hero);
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка соседства с героем. Является соседним: <color=yellow>{0}</color>.", false, areAdjacent);
        return areAdjacent;
    }
}