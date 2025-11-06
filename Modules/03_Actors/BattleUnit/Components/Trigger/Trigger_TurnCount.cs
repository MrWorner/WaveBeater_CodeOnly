using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Триггер срабатывает в зависимости от номера текущего хода.
/// Например, "каждые 3 хода" или "на 5-й ход".
/// ПРИМЕЧАНИЕ: Требует наличия статического свойства TurnManager.CurrentTurn.
/// </summary>
[CreateAssetMenu(fileName = "T_TurnCount", menuName = "AI/Triggers/Turn Count")]
public class Trigger_TurnCount : ActionTrigger
{
    public enum ConditionType { EveryNTurns, ExactTurn, AfterTurn }

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private ConditionType _condition = ConditionType.EveryNTurns;
    [BoxGroup("SETTINGS"), SerializeField] private int _turnValue = 3;
    #endregion Поля

    public override bool IsTriggered(BattleUnit performer)
    {
        int currentTurn = TurnManager.Instance.CurrentTurn;
        bool isTriggered = false;

        switch (_condition)
        {
            case ConditionType.EveryNTurns:
                if (_turnValue > 0) isTriggered = (currentTurn % _turnValue == 0);
                break;
            case ConditionType.ExactTurn:
                isTriggered = (currentTurn == _turnValue);
                break;
            case ConditionType.AfterTurn:
                isTriggered = (currentTurn > _turnValue);
                break;
        }

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка номера хода (Условие: {0} {1}, Текущий: {2}). Сработал: <color=yellow>{3}</color>.", false, _condition, _turnValue, currentTurn, isTriggered);
        return isTriggered;
    }
}