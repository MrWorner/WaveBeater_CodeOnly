using UnityEngine;

public abstract class ActionTrigger : ScriptableObject
{
    /// <summary>
    /// Проверяет, выполнено ли условие триггера для указанного юнита.
    /// </summary>
    /// <param name="performer">Юнит, для которого проверяется условие.</param>
    /// <returns>True, если условие выполнено.</returns>
    public abstract bool IsTriggered(BattleUnit performer);
}