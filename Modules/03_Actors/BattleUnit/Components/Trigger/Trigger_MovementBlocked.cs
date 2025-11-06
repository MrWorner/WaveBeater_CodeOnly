using UnityEngine;

[CreateAssetMenu(fileName = "T_MovementBlocked", menuName = "AI/Triggers/Movement Blocked")]
public class Trigger_MovementBlocked : ActionTrigger
{
    public override bool IsTriggered(BattleUnit performer)
    {
        Vector2Int bestMove = performer.Movement.FindBestMove(performer);
        bool isBlocked = bestMove == performer.CurrentPosition;
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка блокировки движения. Заблокирован: <color=yellow>{0}</color>.", false, isBlocked);
        return isBlocked;
    }
}