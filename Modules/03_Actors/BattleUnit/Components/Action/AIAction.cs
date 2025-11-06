using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public abstract class AIAction : ScriptableObject
{
    public string actionName;

    [Tooltip("Стоимость действия в очках (AP).")]
    public int actionPointCost = 1;

    [Tooltip("Сколько раз это действие можно выполнить за один ход. 0 = бесконечно.")]
    public int maxUsesPerTurn = 1;

    [Tooltip("Действие сработает, если выполнено ХОТЯ БЫ ОДНО из этих условий. Если список пуст, триггеры не проверяются.")]
    public List<ActionTrigger> triggers;

    protected bool AreTriggersMet(BattleUnit performer)
    {
        if (triggers == null || triggers.Count == 0)
        {
            return true;
        }
        return triggers.Any(t => t.IsTriggered(performer));
    }

    public abstract bool CanExecute(BattleUnit performer, int actionPointsLeft);

    /// <summary>
    /// Выполняет действие. Должен вызывать onComplete по завершении.
    /// </summary>
    public abstract void Execute(BattleUnit performer, UnityAction onComplete);
}