using UnityEngine;
using System.Collections.Generic;

public abstract class TargetingProfile : ScriptableObject
{
    /// <summary>
    /// Выбирает лучшую цель из списка потенциальных целей.
    /// </summary>
    /// <param name="performer">Юнит, который выбирает цель.</param>
    /// <param name="potentialTargets">Список всех доступных для атаки вражеских юнитов.</param>
    /// <returns>Выбранная цель или null.</returns>
    public abstract BattleUnit SelectTarget(BattleUnit performer, List<BattleUnit> potentialTargets);
}