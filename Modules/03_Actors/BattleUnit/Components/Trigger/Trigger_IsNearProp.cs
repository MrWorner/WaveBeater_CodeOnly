// НАЗНАЧЕНИЕ: Триггер срабатывает, если юнит находится в заданном радиусе от пропа определенного типа (любого, разрушаемого или неразрушаемого).
// ОСНОВНЫЕ ЗАВИСИМОСТИ: BattleGridPropManager, Prop, BattleGridUtils.
// ПРИМЕЧАНИЕ: Позволяет создавать поведение, зависящее от окружения, например, "спрятаться за укрытием" или "взорвать бочку".
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "T_IsNearProp", menuName = "AI/Triggers/Is Near Prop")]
public class Trigger_IsNearProp : ActionTrigger
{
    public enum PropCondition { Any, Destructible, Indestructible }

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, MinValue(1)] private int _searchRadius = 1;
    [BoxGroup("SETTINGS"), SerializeField] private PropCondition _condition = PropCondition.Any;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет наличие пропов нужного типа в указанном радиусе от юнита.
    /// </summary>
    public override bool IsTriggered(BattleUnit performer)
    {
        var activeProps = BattleGridPropManager.Instance.ActiveProps;
        if (activeProps == null || !activeProps.Any())
        {
            return false;
        }

        foreach (var propGO in activeProps)
        {
            if (propGO == null) continue;

            if (propGO.TryGetComponent<Prop>(out var propComponent) && propComponent.PropSO != null)
            {
                // Используем BattleGridUtils для корректного расчета расстояния на сетке
                int distance = BattleGridUtils.GetDistance(performer, propComponent.AnchorPosition, propComponent.PropSO.PropSize);

                if (distance <= _searchRadius)
                {
                    if (_condition == PropCondition.Any)
                    {
                        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка близости к пропу. Найден любой проп <color=yellow>'{0}'</color> в радиусе {1}. Сработал: <color=yellow>True</color>.", false, propGO.name, _searchRadius);
                        return true;
                    }

                    bool isDestructible = propComponent.PropSO.IsDestructible;
                    if ((_condition == PropCondition.Destructible && isDestructible) || (_condition == PropCondition.Indestructible && !isDestructible))
                    {
                        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка близости к пропу. Найден подходящий проп <color=yellow>'{0}'</color> (Условие: {1}). Сработал: <color=yellow>True</color>.", false, propGO.name, _condition);
                        return true;
                    }
                }
            }
        }

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка близости к пропу. Подходящих пропов в радиусе {0} не найдено. Сработал: <color=yellow>False</color>.", false, _searchRadius);
        return false;
    }
    #endregion Публичные методы
}