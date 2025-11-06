using UnityEngine;

[CreateAssetMenu(fileName = "T_FirstDamageTaken", menuName = "AI/Triggers/First Damage Taken")]
public class Trigger_OnFirstDamage : ActionTrigger
{
    private const string FIRST_DAMAGE_FLAG = "FirstDamageTaken";

    public override bool IsTriggered(BattleUnit performer)
    {
        if (performer.State == null) return false;
        bool isTriggered = performer.State.HasFlag(FIRST_DAMAGE_FLAG);
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка получения первого урона. Флаг установлен: <color=yellow>{0}</color>.", false, isTriggered);
        return isTriggered;
    }
}