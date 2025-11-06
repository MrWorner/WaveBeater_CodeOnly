using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AA_Aim", menuName = "AI/Actions/Aim")]
public class AimAction : AIAction
{
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        var arsenal = performer.Arsenal;
        if (arsenal == null) return false;

        // Ищем любой режим, который требует прицеливания и еще не прицелен
        bool needsAiming = arsenal.GetAllAttackModes().Any(mode =>
            mode.requiresAim &&
            !arsenal.AimTurnsLeft.ContainsKey(mode)
        );

        ColoredDebug.CLog(performer.gameObject, "<color=cyan>AimAction:</color> Проверка необходимости прицеливания. Нужно ли целиться: <color=yellow>{0}</color>.", false, needsAiming);
        return needsAiming;
    }

    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var arsenal = performer.Arsenal;
        // Находим первый попавшийся режим, для которого нужно начать прицеливание
        var modeToAim = arsenal.GetAllAttackModes().FirstOrDefault(m => m.requiresAim && !arsenal.AimTurnsLeft.ContainsKey(m));

        if (modeToAim != null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>AimAction:</color> Начинаю прицеливание для режима <color=yellow>{0}</color>.", false, modeToAim.modeName);
            arsenal.StartAiming(modeToAim);
            performer.UI.ShowAimingUI(modeToAim.turnsToAim);
        }

        onComplete?.Invoke();
    }
}