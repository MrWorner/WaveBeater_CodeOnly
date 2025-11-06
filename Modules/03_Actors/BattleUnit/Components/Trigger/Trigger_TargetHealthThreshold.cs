// НАЗНАЧЕНИЕ: Триггер срабатывает, если здоровье цели (Героя) находится выше или ниже заданного порога.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit.
// ПРИМЕЧАНИЕ: Позволяет создавать "казнящие" или "ослабляющие" типы атак.
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "T_TargetHealth", menuName = "AI/Triggers/Target Health Threshold")]
public class Trigger_TargetHealthThreshold : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, Range(0f, 1f)] private float _healthPercentage = 0.5f;
    [BoxGroup("SETTINGS"), SerializeField] private bool _triggerWhenBelow = true;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, соответствует ли здоровье цели заданному условию.
    /// </summary>
    public override bool IsTriggered(BattleUnit performer)
    {
        BattleUnit target = BattleUnit.Hero;
        if (target == null) return false;

        float currentHealthRatio = (float)target.Health.CurrentHealth / target.Stats.MaxHealth;
        bool isTriggered;

        if (_triggerWhenBelow)
        {
            isTriggered = currentHealthRatio <= _healthPercentage;
        }
        else
        {
            isTriggered = currentHealthRatio >= _healthPercentage;
        }

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка порога здоровья ЦЕЛИ ({0} {1}%). Текущее у цели: {2:P0}. Сработал: <color=yellow>{3}</color>.", false, _triggerWhenBelow ? "<=" : ">=", _healthPercentage * 100, currentHealthRatio, isTriggered);
        return isTriggered;
    }
    #endregion Публичные методы
}