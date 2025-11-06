using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AA_Reload", menuName = "AI/Actions/Reload")]
public class ReloadAction : AIAction
{
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        var arsenal = performer.Arsenal;
        if (arsenal == null) return false;

        // Ищем любой режим, требующий перезарядки, у которого кончились патроны
        bool needsReload = arsenal.GetAllAttackModes().Any(mode =>
            mode.requiresReload &&
            arsenal.CurrentAmmo.ContainsKey(mode) &&
            arsenal.CurrentAmmo[mode] <= 0 &&
            !arsenal.ReloadTurnsLeft.ContainsKey(mode)
        );

        ColoredDebug.CLog(performer.gameObject, "<color=cyan>ReloadAction:</color> Проверка необходимости перезарядки. Нужно ли перезаряжать: <color=yellow>{0}</color>.", false, needsReload);
        return needsReload;
    }

    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var arsenal = performer.Arsenal;
        var modeToReload = arsenal.GetAllAttackModes().FirstOrDefault(mode =>
            mode.requiresReload &&
            arsenal.CurrentAmmo.ContainsKey(mode) &&
            arsenal.CurrentAmmo[mode] <= 0 &&
            !arsenal.ReloadTurnsLeft.ContainsKey(mode));

        if (modeToReload != null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>ReloadAction:</color> Начинаю перезарядку для режима <color=yellow>{0}</color>.", false, modeToReload.modeName);
            // BattleUnitArsenal сам начнет перезарядку, когда патроны кончатся.
            // Это действие просто "тратит" ход, позволяя времени перезарядки идти.
            performer.UI.ShowAimingUI(modeToReload.reloadTimeTurns);
        }

        onComplete?.Invoke();
    }
}