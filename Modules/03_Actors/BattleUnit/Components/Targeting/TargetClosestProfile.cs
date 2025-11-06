using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "TP_Closest", menuName = "AI/Targeting Profiles/Target Closest")]
public class TargetClosestProfile : TargetingProfile
{
    public override BattleUnit SelectTarget(BattleUnit performer, List<BattleUnit> potentialTargets)
    {
        if (potentialTargets == null || potentialTargets.Count == 0) return null;

        // Сортируем цели по дистанции до исполнителя и возвращаем первую (ближайшую)
        return potentialTargets
            .OrderBy(target => BattleGridUtils.GetDistance(performer, target))
            .FirstOrDefault();
    }
}