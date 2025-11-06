// НАЗНАЧЕНИЕ: Представляет специальное действие атаки для AI, которое может также применять статус-эффекты.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnitArsenal, BattleUnitTargetingSystem, BattleUnitActions, StatusEffect, AttackMode, BattleGridUtils.
// ПРИМЕЧАНИЕ: Активируется по триггерам и использует конкретный режим атаки по имени.
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "AA_SpecialAttack", menuName = "AI/Actions/Special Attack")]
public class SpecialAttackAction : AIAction
{
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Имя режима атаки (modeName) из WeaponData, который будет использован.")]
    [SerializeField] private string _attackModeName;
    [BoxGroup("SETTINGS"), Tooltip("Эффекты, которые будут применены к ИСПОЛНИТЕЛЮ действия после атаки.")]
    [SerializeField] private List<StatusEffect> _effectsToApplyOnSelf;
    [BoxGroup("SETTINGS"), Tooltip("Эффекты, которые будут применены к ЦЕЛИ после атаки.")]
    [SerializeField] private List<StatusEffect> _effectsToApplyOnTarget;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, может ли юнит выполнить специальную атаку.
    /// </summary>
    /// <param name="performer">Юнит, выполняющий проверку.</param>
    /// <param name="actionPointsLeft">Оставшиеся очки действий.</param>
    /// <returns>True, если атака возможна.</returns>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        if (!AreTriggersMet(performer)) return false;

        var arsenal = performer.Arsenal;
        var targeting = performer.TargetingSystem;
        if (arsenal == null || targeting == null) return false;

        var target = targeting.GetBestTarget();
        if (target == null) return false;

        var attackMode = arsenal.GetAllAttackModes().FirstOrDefault(m => m.modeName == _attackModeName);
        if (attackMode == null) return false;

        int distance = BattleGridUtils.GetDistance(performer, target);
        if (distance < attackMode.minRange || distance > attackMode.range) return false;

        return arsenal.CanUseMode(attackMode);
    }

    /// <summary>
    /// Выполняет специальную атаку и применяет статус-эффекты.
    /// </summary>
    /// <param name="performer">Атакующий юнит.</param>
    /// <param name="onComplete">Действие, вызываемое по завершении атаки.</param>
    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        var arsenal = performer.Arsenal;
        var actions = performer.Actions;
        var targeting = performer.TargetingSystem;

        if (arsenal == null || actions == null || targeting == null)
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

        var attackMode = arsenal.GetAllAttackModes().FirstOrDefault(m => m.modeName == _attackModeName);
        if (attackMode != null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>SpecialAttackAction:</color> Выполняю специальную атаку <color=yellow>'{0}'</color>.", _ColoredDebug, attackMode.modeName);
            actions.ExecuteAttack(target, attackMode, () =>
            {
                foreach (var effect in _effectsToApplyOnSelf)
                {
                    effect.ApplyEffect(performer);
                }

                if (target != null && target.IsAlive)
                {
                    foreach (var effect in _effectsToApplyOnTarget)
                    {
                        effect.ApplyEffect(target);
                    }
                }

                onComplete?.Invoke();
            });
            arsenal.UseMode(attackMode);
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    #endregion Публичные методы
}