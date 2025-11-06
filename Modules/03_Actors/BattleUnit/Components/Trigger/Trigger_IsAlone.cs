using UnityEngine;

/// <summary>
/// Триггер срабатывает, если данный юнит - последний оставшийся враг на поле.
/// </summary>
[CreateAssetMenu(fileName = "T_IsAlone", menuName = "AI/Triggers/Is Alone")]
public class Trigger_IsAlone : ActionTrigger
{
    public override bool IsTriggered(BattleUnit performer)
    {
        // Используем новый статический список врагов из BattleUnit
        bool isAlone = BattleUnit.Enemies.Count == 1 && BattleUnit.Enemies.Contains(performer);
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка, является ли юнит последним врагом. Результат: <color=yellow>{0}</color>.", false, isAlone);
        return isAlone;
    }
}