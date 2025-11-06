using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "TP_PlayerPriority", menuName = "AI/Targeting Profiles/Target Player Priority")]
public class TargetPlayerPriorityProfile : TargetingProfile
{
    public override BattleUnit SelectTarget(BattleUnit performer, List<BattleUnit> potentialTargets)
    {
        if (potentialTargets == null || potentialTargets.Count == 0) return null;

        // Если Герой есть в списке целей, всегда выбираем его
        if (BattleUnit.Hero != null && potentialTargets.Contains(BattleUnit.Hero))
        {
            return BattleUnit.Hero;
        }

        // Если Героя нет, выбираем ближайшую цель
        return potentialTargets
            .OrderBy(target => BattleGridUtils.GetDistance(performer, target))
            .FirstOrDefault();
    }
}