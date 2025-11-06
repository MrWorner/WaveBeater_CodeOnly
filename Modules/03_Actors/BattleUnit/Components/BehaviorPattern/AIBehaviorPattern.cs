// In a new file: AIBehaviorPattern.cs
using UnityEngine;
using System.Collections.Generic;

public abstract class AIBehaviorPattern : ScriptableObject
{
    /// <summary>
    /// The main decision-making method.
    /// </summary>
    /// <param name="performer">The unit making the decision.</param>
    /// <param name="availableActions">The list of actions the unit can perform.</param>
    /// <returns>The chosen action to execute, or null to do nothing.</returns>
    public abstract AIAction DecideAction(BattleUnit performer, List<AIAction> availableActions);
}