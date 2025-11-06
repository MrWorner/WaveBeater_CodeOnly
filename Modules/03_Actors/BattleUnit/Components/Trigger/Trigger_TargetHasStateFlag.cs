using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Триггер срабатывает, если у цели (Героя) установлен определенный флаг в BattleUnitState.
/// </summary>
[CreateAssetMenu(fileName = "T_TargetHasStateFlag", menuName = "AI/Triggers/Target Has State Flag")]
public class Trigger_TargetHasStateFlag : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private string _flagToCheck = "PlayerIsStunned";
    #endregion Поля

    public override bool IsTriggered(BattleUnit performer)
    {
        if (BattleUnit.Hero == null || BattleUnit.Hero.State == null || string.IsNullOrEmpty(_flagToCheck)) return false;

        bool hasFlag = BattleUnit.Hero.State.HasFlag(_flagToCheck);
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка флага '{0}' у Героя. Найден: <color=yellow>{1}</color>.", false, _flagToCheck, hasFlag);
        return hasFlag;
    }
}