using UnityEngine;

/// <summary>
/// Триггер срабатывает, если юнит получил урон в текущем ходу.
/// Требует, чтобы BattleUnitHealth устанавливал флаг "DamagedThisTurn".
/// </summary>
[CreateAssetMenu(fileName = "T_DamagedThisTurn", menuName = "AI/Triggers/Damaged This Turn")]
public class Trigger_DamagedThisTurn : ActionTrigger
{
    public const string DAMAGED_THIS_TURN_FLAG = "DamagedThisTurn";

    public override bool IsTriggered(BattleUnit performer)
    {
        if (performer.State == null) return false;
        bool wasDamaged = performer.State.HasFlag(DAMAGED_THIS_TURN_FLAG);
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка получения урона в этом ходу. Был поврежден: <color=yellow>{0}</color>.", false, wasDamaged);
        return wasDamaged;
    }
}