using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HeroAnimation : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Animator _animator;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS")] private string _idleNoBattleName = "Hero_Idle_NoBattle";
    [BoxGroup("SETTINGS")] private string _idleInBattleName = "Hero_Idle_InBattle_LOOP";
    [BoxGroup("SETTINGS")] private string _shootingName = "Hero_Shooting_InBattle";
    [BoxGroup("SETTINGS")] private string _deathName = "Hero_Dies";
    [BoxGroup("SETTINGS")] private string _runName = "Hero_Runs";
    [BoxGroup("SETTINGS")] private string _hitName = "Hero_Hit";
    [BoxGroup("SETTINGS"), SerializeField] private float _defaultTransitionDuration = 0.25f;

    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public string IdleNoBattleName { get => _idleNoBattleName; }
    public string IdleInBattleName { get => _idleInBattleName; }
    public string ShootingName { get => _shootingName; }
    public string DeathName { get => _deathName; }
    public float DefaultTransitionDuration { get => _defaultTransitionDuration; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_animator == null) DebugUtils.LogMissingReference(this, nameof(_animator));
    }
    #endregion Методы UNITY

    #region Публичные методы
    [Button]
    public void SetStance(bool inBattle)
    {
        string targetAnimation = inBattle ? _idleInBattleName : _idleNoBattleName;
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroAnimation:</color> Установка стойки. В бою: <color=yellow>{0}</color>. Анимация: <color=lime>{1}</color>.", _ColoredDebug, inBattle, targetAnimation);
        _animator.CrossFade(targetAnimation, _defaultTransitionDuration);
    }

    [Button]
    public void PlayRunAnimation()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroAnimation:</color> Запуск анимации бега: <color=lime>{0}</color>.", _ColoredDebug, _runName);
        _animator.CrossFade(_runName, _defaultTransitionDuration);
    }

    [Button]
    public void PlayHitAnimation()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroAnimation:</color> Запуск анимации получения урона: <color=lime>{0}</color>.", _ColoredDebug, _hitName);
        _animator.CrossFade(_hitName, _defaultTransitionDuration);
    }

    [Button]
    public void PlayShootAnimation()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroAnimation:</color> Запуск анимации выстрела. Animator сам вернется в Idle.", _ColoredDebug);
        _animator.Play(_shootingName);
    }

    [Button]
    public void PlayDeathAnimation()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroAnimation:</color> Запуск анимации смерти: <color=lime>{0}</color>.", _ColoredDebug, _deathName);
        _animator.CrossFade(_deathName, _defaultTransitionDuration);
    }
    #endregion Публичные методы
}

