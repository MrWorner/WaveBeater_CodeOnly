using Sirenix.OdinInspector;
using UnityEngine;
/// <summary>
/// Универсальный триггер, который проверяет наличие указанного флага в BattleUnitState.
/// Позволяет создавать сложные цепочки поведений без написания кода.
/// </summary>
[CreateAssetMenu(fileName = "T_StateFlag", menuName = "AI/Triggers/Generic State Flag")]
public class Trigger_StateFlag : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private string _flagToCheck = "MyCustomFlag";
    #endregion Поля

    public override bool IsTriggered(BattleUnit performer)
    {
        if (performer.State == null || string.IsNullOrEmpty(_flagToCheck)) return false;

        bool hasFlag = performer.State.HasFlag(_flagToCheck);
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка наличия флага '{0}'. Найден: <color=yellow>{1}</color>.", false, _flagToCheck, hasFlag);
        return hasFlag;
    }
}
