using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

public class BattleUnitAbilities : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnit _battleUnit;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitStats _unitStats;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitUI _unitUI;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _ghostFistObject;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _firePoint;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentElectroShieldCharges;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentIronVestCharges;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentEmergencySystemCharges;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public int CurrentElectroShieldCharges { get => _currentElectroShieldCharges; }
    public int CurrentIronVestCharges { get => _currentIronVestCharges; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_battleUnit == null) DebugUtils.LogMissingReference(this, nameof(_battleUnit));
        if (_unitStats == null) DebugUtils.LogMissingReference(this, nameof(_unitStats));
        if (_unitUI == null) DebugUtils.LogMissingReference(this, nameof(_unitUI));
        if (_ghostFistObject == null) DebugUtils.LogMissingReference(this, nameof(_ghostFistObject));
        if (_firePoint == null) DebugUtils.LogMissingReference(this, nameof(_firePoint));
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void Initialize()
    {
        if (_ghostFistObject != null) _ghostFistObject.SetActive(false);
        _currentElectroShieldCharges = _unitStats.MaxElectroShieldCharges;
        _currentIronVestCharges = _unitStats.MaxIronVestCharges;
        _currentEmergencySystemCharges = _unitStats.MaxEmergencySystemCharges;
    }

    public int ProcessDamage(int incomingDamage)
    {
        if (_currentElectroShieldCharges > 0)
        {
            SoundManager.Instance.PlayOneShot(SoundType.ElectroShieldHit);
            _currentElectroShieldCharges--;
            _unitUI.ShowShieldLossText(1);
            _unitUI.UpdateShieldDisplay(_currentElectroShieldCharges);
            if (_currentElectroShieldCharges <= 0) _unitUI.SetElectroShieldVisual(false);
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitAbilities:</color> Электрощит поглотил урон. Осталось зарядов: <color=lime>{0}</color>.", _ColoredDebug, _currentElectroShieldCharges);
            return 0;
        }

        if (_currentIronVestCharges > 0 && incomingDamage > 0)
        {
            SoundManager.Instance.PlayOneShot(SoundType.IronClawHit);
            int absorbedDamage = 1;
            incomingDamage -= absorbedDamage;
            _currentIronVestCharges--;
            _unitUI.ShowVestLossText(absorbedDamage);
            _unitUI.UpdateVestDisplay(_currentIronVestCharges);
            ColoredDebug.CLog(gameObject, "<color=grey>BattleUnitAbilities:</color> Бронежилет поглотил <color=yellow>{0}</color> ед. урона. Осталось зарядов: <color=lime>{1}</color>.", _ColoredDebug, absorbedDamage, _currentIronVestCharges);
        }

        return Mathf.Max(0, incomingDamage);
    }

    public void TryBacklash(BattleUnit target)
    {
        if (target != null && target.Movement.CurrentPosition.x >= _unitStats.BacklashMinDistance)
        {
            if (Random.value <= _unitStats.BacklashChance)
            {
                ColoredDebug.CLog(gameObject, "<color=magenta>BattleUnitAbilities:</color> Сработал Backlash! Атакую <color=yellow>{0}</color>.", _ColoredDebug, target.name);
                TurnManager.Instance.EnqueueAction(BacklashRoutine(target));
            }
        }
    }

    public bool CanUseEmergencySystem()
    {
        return _currentEmergencySystemCharges > 0;
    }

    public void UseEmergencySystem()
    {
        _currentEmergencySystemCharges--;
        ColoredDebug.CLog(gameObject, "<color=lime><b>BattleUnitAbilities:</b></color> <color=yellow>Система экстренной помощи</color> использована. Осталось зарядов: <color=lime>{0}</color>.", _ColoredDebug, _currentEmergencySystemCharges);
    }

    public void AddEmergencySystemCharges(int amount)
    {
        _currentEmergencySystemCharges += amount;
    }

    public void RechargeShieldAfterStage()
    {
        if (_unitStats.MaxElectroShieldCharges > 0 && _currentElectroShieldCharges < _unitStats.MaxElectroShieldCharges)
        {
            SoundManager.Instance.PlayOneShot(SoundType.ElectroShieldUp);
            _currentElectroShieldCharges++;
            _unitUI.UpdateShieldDisplay(_currentElectroShieldCharges);
            _unitUI.SetElectroShieldVisual(true);
        }
    }

    public void ReplenishElectroShields()
    {
        _currentElectroShieldCharges = _unitStats.MaxElectroShieldCharges;
    }

    public void ReplenishIronVest()
    {
        _currentIronVestCharges = _unitStats.MaxIronVestCharges;
    }
    #endregion Публичные методы

    #region Личные методы
    private IEnumerator BacklashRoutine(BattleUnit target)
    {
        if (_ghostFistObject != null && target != null && target.IsAlive)
        {
            _ghostFistObject.transform.position = _firePoint.position;
            _ghostFistObject.SetActive(true);
            yield return _ghostFistObject.transform.DOMove(target.DamagePoint.position, Settings.BacklashEffectDuration).SetEase(Ease.InSine).WaitForCompletion();

            if (target.IsAlive)
            {
                //target.Health.TakeDamage(_unitStats.AttackDamage, _battleUnit, false);
                target.Health.TakeDamage(-1, _battleUnit, false);
            }

            yield return new WaitForSeconds(0.1f);
            _ghostFistObject.SetActive(false);
        }
    }
    #endregion Личные методы
}