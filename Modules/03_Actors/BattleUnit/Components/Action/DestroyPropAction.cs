using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[CreateAssetMenu(fileName = "AA_DestroyProp", menuName = "AI/Actions/Destroy Prop")]
public class DestroyPropAction : AIAction
{
    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Урон, наносимый пропу за одно действие."), MinValue(1), SerializeField]
    private int _destroyDamage = 1;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Публичные методы
    /// <summary>
    /// Проверяет, заблокирован ли юнит и есть ли на пути РАЗРУШАЕМОЕ препятствие.
    /// </summary>
    public override bool CanExecute(BattleUnit performer, int actionPointsLeft)
    {
        if (actionPointsLeft < actionPointCost) return false;
        if (!AreTriggersMet(performer)) return false;

        // Действие возможно, только если обычное движение невозможно
        // (FindBestMove теперь корректно вернет CurrentPosition, если путь заблокирован или ведет назад)
        bool isMovementBlocked = performer.Movement.FindBestMove(performer) == performer.CurrentPosition;

        // И если на пути есть что ломать (FindDestructibleObstacleOnPath already checks IsDestructible)
        bool canDestroy = performer.Movement.FindDestructibleObstacleOnPath() != null;

        ColoredDebug.CLog(performer.gameObject, "<color=cyan>DestroyPropAction:</color> Проверка возможности. Движение заблокировано: <color=yellow>{0}</color>, есть цель для уничтожения: <color=yellow>{1}</color>.", _ColoredDebug, isMovementBlocked, canDestroy);
        return isMovementBlocked && canDestroy;
    }

    /// <summary>
    /// Выполняет атаку на препятствие, нанося ему урон.
    /// </summary>
    public override void Execute(BattleUnit performer, UnityAction onComplete)
    {
        GameObject propToAttack = performer.Movement.FindDestructibleObstacleOnPath();
        if (propToAttack != null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=lime>DestroyPropAction:</color> Атакую объект <color=yellow>{0}</color>.", _ColoredDebug, propToAttack.name);
            BattleLogger.Instance.LogAction(performer, actionName, $"Attack Prop: {propToAttack.name}"); // Log action
            performer.StartCoroutine(AttackRoutine(performer, propToAttack, onComplete));
        }
        else
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>DestroyPropAction:</color> Не найдено цели для атаки при выполнении Execute. Завершаю действие.", _ColoredDebug);
            onComplete?.Invoke();
        }
    }
    #endregion Публичные методы

    #region Личные методы
    private IEnumerator AttackRoutine(BattleUnit performer, GameObject targetProp, UnityAction onComplete)
    {
        performer.Animator.PlayAttackAnimation();
        yield return new WaitForSeconds(Settings.HitAttackAnimationSpeed);

        if (targetProp != null && targetProp.TryGetComponent<PropHealth>(out var propHealth))
        {
            propHealth.TakeDamage(_destroyDamage, performer);
        }
        else if (targetProp != null)
        {
            ColoredDebug.CLog(performer.gameObject, "<color=orange>DestroyPropAction:</color> Prop '{0}' is destructible but missing PropHealth component!", _ColoredDebug, targetProp.name);
        }

        yield return new WaitForSeconds(Settings.WaitAfterUnitAction_Melee);

        onComplete?.Invoke();
    }
    #endregion Личные методы
}
