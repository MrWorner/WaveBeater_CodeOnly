// НАЗНАЧЕНИЕ: Отвечает за выполнение конкретных боевых действий юнита, таких как атака (ближняя и дальняя).
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleUnitStats, BattleUnitAnimator, ObjectPoolProjectiles.
// ПРИМЕЧАНИЕ: Содержит корутины для реализации последовательностей анимаций и логики атак.

using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[RequireComponent(typeof(BattleUnit))]
public class BattleUnitActions : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnit _battleUnit;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitStats _unitStats;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitAnimator _unitAnimator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _firePoint;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isAttacking;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public bool IsAttacking { get => _isAttacking; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_battleUnit == null) DebugUtils.LogMissingReference(this, nameof(_battleUnit));
        if (_unitStats == null) DebugUtils.LogMissingReference(this, nameof(_unitStats));
        if (_unitAnimator == null) DebugUtils.LogMissingReference(this, nameof(_unitAnimator));
        if (_firePoint == null) DebugUtils.LogMissingReference(this, nameof(_firePoint));
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Выполняет атаку на цель, используя указанный режим атаки.
    /// </summary>
    /// <param name="target">Цель атаки.</param>
    /// <param name="attackMode">Режим атаки из WeaponData.</param>
    /// <param name="onComplete">Действие, которое будет вызвано по завершении всей последовательности атаки.</param>
    public void ExecuteAttack(BattleUnit target, AttackMode attackMode, UnityAction onComplete)
    {
        if (_isAttacking)
        {
            ColoredDebug.CLog(gameObject, "<color=red>BattleUnitActions:</color> Невозможно начать атаку. Юнит уже атакует.", _ColoredDebug);
            return;
        }

        _isAttacking = true;
        if (attackMode.isMelee)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitActions:</color> Начинаю <color=yellow>атаку ближнего боя ({0})</color> по цели <color=yellow>{1}</color> с уроном <color=lime>{2}</color>.", _ColoredDebug, attackMode.modeName, target.name, attackMode.damage);
            StartCoroutine(MeleeAttackRoutine(target, attackMode, onComplete));
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitActions:</color> Начинаю <color=yellow>атаку дальнего боя ({0})</color> по цели <color=yellow>{1}</color> с уроном <color=lime>{2}</color>.", _ColoredDebug, attackMode.modeName, target.name, attackMode.damage);
            StartCoroutine(RangedAttackRoutine(target, attackMode, onComplete));
        }
    }
    #endregion Публичные методы

    #region Личные методы
    private IEnumerator MeleeAttackRoutine(BattleUnit target, AttackMode attackMode, UnityAction onComplete)
    {
        bool isCritical = Random.value * 100 < _unitStats.CriticalHitChance;
        int finalDamage = isCritical ? attackMode.damage * 2 : attackMode.damage;

        Vector3 originalPosition = transform.position;
        Vector3 targetAttackPos = target.DamagePoint.position + new Vector3(0.5f, 0, 0);

        ColoredDebug.CLog(gameObject, "<color=cyan>MeleeAttackRoutine:</color> Рывок к цели <color=yellow>{0}</color>.", _ColoredDebug, target.name);
        yield return transform.DOMove(targetAttackPos, Settings.MovementDuringMeleeAttackSpeed).SetEase(Ease.OutQuad).WaitForCompletion();

        _unitAnimator.PlayAttackAnimation();
        ColoredDebug.CLog(gameObject, "<color=cyan>MeleeAttackRoutine:</color> Воспроизведение анимации атаки.", _ColoredDebug);
        yield return new WaitForSeconds(Settings.HitAttackAnimationSpeed);

        if (target != null && target.IsAlive)
        {
            if (Random.value <= attackMode.hitChance)
            {
                ColoredDebug.CLog(gameObject, "<color=cyan>MeleeAttackRoutine:</color> <color=lime>Успешное попадание</color>. Наносимый урон: <color=lime>{0}</color> (Крит: {1}).", _ColoredDebug, finalDamage, isCritical);
                SoundManager.Instance.PlayOneShot(SoundType.EnemyMeleeHit);
                target.Health.TakeDamage(finalDamage, _battleUnit, isCritical);
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=cyan>MeleeAttackRoutine:</color> <color=red>Промах!</color>", _ColoredDebug);
                SoundManager.Instance.PlayOneShot(SoundType.MeleeMiss);
                target.UI.ShowEvasionText();
            }
        }

        yield return new WaitForSeconds(Settings.HitAttackAnimationSpeed * 2);

        ColoredDebug.CLog(gameObject, "<color=cyan>MeleeAttackRoutine:</color> Возврат на исходную позицию.", _ColoredDebug);
        yield return transform.DOMove(originalPosition, Settings.MovementDuringMeleeAttackSpeed).SetEase(Ease.OutQuad).WaitForCompletion();

        _isAttacking = false;
        onComplete?.Invoke();
        ColoredDebug.CLog(gameObject, "<color=cyan>MeleeAttackRoutine:</color> Атака завершена.", _ColoredDebug);
    }

    private IEnumerator RangedAttackRoutine(BattleUnit target, AttackMode attackMode, UnityAction onComplete)
    {
        bool isCritical = Random.value * 100 < _unitStats.CriticalHitChance;
        int finalDamage = isCritical ? attackMode.damage * 2 : attackMode.damage;

        _unitAnimator.PlayAttackAnimation();
        ColoredDebug.CLog(gameObject, "<color=cyan>RangedAttackRoutine:</color> Воспроизведение анимации выстрела.", _ColoredDebug);

        GameObject projectileObj = ObjectPoolProjectiles.Instance.GetObject();
        if (projectileObj == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>RangedAttackRoutine:</color> Не удалось получить снаряд из пула. Атака отменена.", _ColoredDebug);
            _isAttacking = false;
            onComplete?.Invoke();
            yield break;
        }

        Projectile proj = projectileObj.GetComponent<Projectile>();
        if (proj == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>RangedAttackRoutine:</color> Полученный объект не содержит компонента Projectile.", _ColoredDebug);
            ObjectPoolProjectiles.Instance.ReturnObject(projectileObj);
            _isAttacking = false;
            onComplete?.Invoke();
            yield break;
        }

        projectileObj.transform.position = _firePoint.position;
        SoundManager.Instance.PlayOneShot(SoundType.GunShot);
        ColoredDebug.CLog(gameObject, "<color=cyan>RangedAttackRoutine:</color> Снаряд инициализирован в позиции <color=yellow>{0}</color>.", _ColoredDebug, _firePoint.position);

        bool isHit = Random.value <= attackMode.hitChance;

        if (isHit)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>RangedAttackRoutine:</color> <color=lime>Попадание!</color> Снаряд полетел в <color=yellow>{0}</color> с уроном <color=lime>{1}</color> (Крит: {2}).", _ColoredDebug, target.name, finalDamage, isCritical);
            proj.Initialize(target, finalDamage, _battleUnit, () =>
            {
                onComplete?.Invoke();
                _isAttacking = false;
                ColoredDebug.CLog(gameObject, "<color=cyan>RangedAttackRoutine:</color> Атака завершена (вызвана из Projectile).", _ColoredDebug);
            }, true, Vector3.zero, isCritical);
        }
        else
        {
            Vector3 targetPos = target.DamagePoint.position;
            Vector3 startPos = _firePoint.position;
            Vector3 direction = (targetPos - startPos).normalized;
            Vector3 baseMissPosition = targetPos + direction * 3f;
            float missY = Random.Range(-2f, 0);
            Vector3 missPosition = new Vector3(baseMissPosition.x, baseMissPosition.y + missY, baseMissPosition.z);

            ColoredDebug.CLog(gameObject, "<color=cyan>RangedAttackRoutine:</color> <color=red>Промах!</color> Снаряд полетел в расчетную точку промаха <color=yellow>{0}</color>.", _ColoredDebug, missPosition);
            proj.Initialize(null, 0, _battleUnit, () =>
            {
                onComplete?.Invoke();
                _isAttacking = false;
                ColoredDebug.CLog(gameObject, "<color=cyan>RangedAttackRoutine:</color> Атака завершена (вызвана из Projectile).", _ColoredDebug);
            }, false, missPosition, false);
        }

        yield break;
    }
    #endregion Личные методы
}