using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "T_AmmoState", menuName = "AI/Triggers/Ammo State")]
public class Trigger_AmmoState : ActionTrigger
{
    public enum AmmoCondition { IsFull, IsEmpty, IsBelowPercentage }

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, Tooltip("Имя режима атаки (modeName), который нужно проверить.")]
    private string _attackModeName;
    [BoxGroup("SETTINGS"), SerializeField]
    private AmmoCondition _condition = AmmoCondition.IsEmpty;
    [BoxGroup("SETTINGS"), SerializeField, Range(0f, 1f), ShowIf("_condition", AmmoCondition.IsBelowPercentage)]
    private float _percentage = 0.5f;
    #endregion Поля

    public override bool IsTriggered(BattleUnit performer)
    {
        var arsenal = performer.Arsenal;
        if (arsenal == null || string.IsNullOrEmpty(_attackModeName)) return false;

        var mode = arsenal.GetAllAttackModes().FirstOrDefault(m => m.modeName == _attackModeName);
        if (mode == null || !mode.requiresReload)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=#FFC0CB>Trigger:</color> Режим атаки '{0}' не найден или не требует боезапаса.", false, _attackModeName);
            return false;
        }

        arsenal.CurrentAmmo.TryGetValue(mode, out int currentAmmo);
        bool isTriggered = false;
        switch (_condition)
        {
            case AmmoCondition.IsFull:
                isTriggered = currentAmmo >= mode.clipSize;
                break;
            case AmmoCondition.IsEmpty:
                isTriggered = currentAmmo <= 0;
                break;
            case AmmoCondition.IsBelowPercentage:
                isTriggered = (float)currentAmmo / mode.clipSize <= _percentage;
                break;
        }

        ColoredDebug.CLog(performer.gameObject, "<color=#ADD8E6>Trigger:</color> Проверка боезапаса '{0}' ({1}/{2}). Условие: {3}. Сработал: <color=yellow>{4}</color>.", false, _attackModeName, currentAmmo, mode.clipSize, _condition, isTriggered);
        return isTriggered;
    }
}