using UnityEngine;

/// <summary>
/// Этот эффект устанавливает юниту флаг "DISABLED", который заставляет его пропускать ходы.
/// </summary>
[CreateAssetMenu(fileName = "SE_Disable", menuName = "AI/Status Effects/Disable Unit")]
public class DisableEffect : StatusEffect
{
    [Tooltip("Флаг, который будет установлен в BattleUnitState.")]
    public string disabledFlag = "DISABLED";

    public override void ApplyEffect(BattleUnit target)
    {
        if (target != null && target.State != null)
        {
            target.State.SetFlag(disabledFlag);
            ColoredDebug.CLog(target.gameObject, "<color=red>StatusEffect:</color> Наложен эффект <color=yellow>'{0}'</color>. Юнит отключен.", false, effectName);
        }
    }
}