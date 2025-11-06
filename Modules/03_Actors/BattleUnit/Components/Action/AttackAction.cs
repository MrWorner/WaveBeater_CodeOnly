// НАЗНАЧЕНИЕ: Представляет действие атаки для AI. Определяет, может ли юнит атаковать, и выполняет атаку, используя лучший доступный режим.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnitArsenal, BattleUnitTargetingSystem, BattleUnitActions, AttackMode, BattleGridUtils.
// ПРИМЕЧАНИЕ: Может быть настроен для предпочтения ближней или дальней атаки.
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "AA_Attack", menuName = "AI/Actions/Attack")]
public class AttackAction : AIAction
{
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Если true, будет выбрана атака ближнего боя. Иначе - дальнего.")]
    public bool preferMelee = false;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, может ли юнит выполнить атаку на текущую цель.
    /// </summary>
    /// <param name="performer">Юнит, выполняющий проверку.</param>
    /// <param name="actionPointsLeft">Оставшиеся очки действий.</param>
    /// <returns>True, если атака возможна.</returns>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        var arsenal = performer.Arsenal;
        var targeting = performer.TargetingSystem;
        if (arsenal == null || targeting == null) return false;

        var target = targeting.GetBestTarget();
        if (target == null) return false;

        bool canAttack = arsenal.GetAllAttackModes()
            .Any(mode => mode.isMelee == preferMelee && IsTargetInRange(performer, target, mode) && arsenal.CanUseMode(mode));

        ColoredDebug.CLog(performer.gameObject, "<color=cyan>AttackAction ({0}):</color> Проверка возможности атаки. Можно ли атаковать: <color=yellow>{1}</color>.", false, preferMelee ? "Melee" : "Ranged", canAttack);
        return canAttack;
    }

    /// <summary>
    /// Выполняет атаку, выбирая лучший доступный режим.
    /// </summary>
    /// <param name="performer">Атакующий юнит.</param>
    /// <param name="onComplete">Действие, вызываемое по завершении атаки.</param>
    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var arsenal = performer.Arsenal;
        var actions = performer.Actions;
        var targeting = performer.TargetingSystem;

        if (actions == null || arsenal == null || targeting == null)
        {
            onComplete?.Invoke();
            return;
        }

        var target = targeting.GetBestTarget();
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        AttackMode modeToUse = arsenal.GetAllAttackModes()
            .Where(mode => mode.isMelee == preferMelee && IsTargetInRange(performer, target, mode) && arsenal.CanUseMode(mode))
            .OrderByDescending(mode => mode.damage)
            .FirstOrDefault();

        if (modeToUse != null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>AttackAction ({0}):</color> Атакую <color=yellow>{1}</color> в режиме <color=yellow>{2}</color>.", _ColoredDebug, preferMelee ? "Melee" : "Ranged", target.name, modeToUse.modeName);

            string details = $"Target: {target.name}, Mode: {modeToUse.modeName}, Damage: {modeToUse.damage}";
            BattleLogger.Instance.LogAction(performer, actionName, details);


            arsenal.UseMode(modeToUse);
            actions.ExecuteAttack(target, modeToUse, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    #endregion Публичные методы

    #region Личные методы
    private bool IsTargetInRange(BattleUnit performer, BattleUnit target, AttackMode mode)
    {
        if (target == null) return false;
        int distance = BattleGridUtils.GetDistance(performer, target);
        return distance >= mode.minRange && distance <= mode.range;
    }
    #endregion Личные методы
}