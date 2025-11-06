using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "T_HealthThreshold", menuName = "AI/Triggers/Health Threshold")]
public class Trigger_HealthThreshold : ActionTrigger
{
    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, Range(0f, 1f)] private float _healthPercentage = 0.3f;
    [BoxGroup("SETTINGS"), SerializeField] private bool _triggerWhenBelow = true;
    #endregion Поля

    public override bool IsTriggered(BattleUnit performer)
    {
        float currentHealthRatio = (float)performer.Health.CurrentHealth / performer.Stats.MaxHealth;
        bool isTriggered;

        if (_triggerWhenBelow)
        {
            isTriggered = currentHealthRatio <= _healthPercentage;
        }
        else
        {
            isTriggered = currentHealthRatio >= _healthPercentage;
        }

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка порога здоровья ({0} {1}%). Текущее: {2:P0}. Сработал: <color=yellow>{3}</color>.", false, _triggerWhenBelow ? "<=" : ">=", _healthPercentage * 100, currentHealthRatio, isTriggered);
        return isTriggered;
    }
}