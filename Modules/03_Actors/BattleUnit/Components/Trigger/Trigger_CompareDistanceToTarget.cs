// НАЗНАЧЕНИЕ: Триггер срабатывает, сравнивая дистанцию до цели (Героя) с заданным значением.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleGridUtils.
// ПРИМЕЧАНИЕ: Учитывает размер юнитов при расчете дистанции.
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "T_CompareDistance", menuName = "AI/Triggers/Compare Distance")]
public class Trigger_CompareDistanceToTarget : ActionTrigger
{
    public enum ComparisonType { LessThan, GreaterThan, EqualTo }

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private ComparisonType _comparison = ComparisonType.LessThan;
    [BoxGroup("SETTINGS"), SerializeField, MinValue(0)] private int _distance = 3;
    #endregion Поля

    public override bool IsTriggered(BattleUnit performer)
    {
        if (BattleUnit.Hero == null) return false;

        int distance = BattleGridUtils.GetDistance(performer, BattleUnit.Hero);
        bool isTriggered = false;

        switch (_comparison)
        {
            case ComparisonType.LessThan:
                isTriggered = distance < _distance;
                break;
            case ComparisonType.GreaterThan:
                isTriggered = distance > _distance;
                break;
            case ComparisonType.EqualTo:
                isTriggered = distance == _distance;
                break;
        }

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка дистанции (Текущая: {0}, Условие: {1} {2}). Сработал: <color=yellow>{3}</color>.", false, distance, _comparison, _distance, isTriggered);
        return isTriggered;
    }
}