using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "T_CannotAdvanceOrMelee", menuName = "AI/Triggers/Cannot Advance Or Melee")]
public class Trigger_CannotAdvanceOrMelee : ActionTrigger
{
    public override bool IsTriggered(BattleUnit performer)
    {
        // 1. Проверяем, может ли юнит двигаться вперед
        Vector2Int bestMove = performer.Movement.FindBestMove(performer);
        bool cannotMove = bestMove == performer.CurrentPosition;

        // 2. Проверяем, может ли юнит атаковать в ближнем бою с текущей позиции
        bool canMelee = false;
        var arsenal = performer.Arsenal;
        var targeting = performer.TargetingSystem;

        if (arsenal != null && targeting != null)
        {
            var target = targeting.GetBestTarget();
            if (target != null)
            {
                // Ищем любой доступный режим ближнего боя
                var meleeMode = arsenal.GetAllAttackModes().FirstOrDefault(m => m.isMelee);
                if (meleeMode != null)
                {
                    int distance = BattleGridUtils.GetDistance(performer, target);
                    if (distance >= meleeMode.minRange && distance <= meleeMode.range && arsenal.CanUseMode(meleeMode))
                    {
                        canMelee = true;
                    }
                }
            }
        }

        // 3. Триггер срабатывает, если нельзя двигаться И нельзя атаковать в ближнем бою
        bool isTriggered = cannotMove && !canMelee;
        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка блокировки. Нельзя двигаться: {0}, Нельзя атаковать в ближнем бою: {1}. Сработал: <color=yellow>{2}</color>.", false, cannotMove, !canMelee, isTriggered);
        return isTriggered;
    }
}