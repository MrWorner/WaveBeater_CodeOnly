using Sirenix.OdinInspector;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class PlayerInfoPanelUI : MonoBehaviour
{
    #region Поля: Required

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _coinsInfoText;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _attackInfoText;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _healthInfoText;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _speedInfoText;

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _autoHealInfoObject;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _autoHealInfoText;

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _emergencySystemInfoObject;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _emergencySystemInfoText;

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _electroShieldInfoObject;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _electroShieldInfoText;

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _ironcladInfoObject;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _ironcladInfoText;

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _backlashInfoObject;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _backlashInfoText;

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _criticalHitInfoObject;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _criticalHitInfoText;

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, ReadOnly] private BattleUnit _hero;

    #endregion Поля: Required

    #region Поля

    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;

    private static PlayerInfoPanelUI _instance;

    #endregion Поля

    #region Свойства

    public static PlayerInfoPanelUI Instance { get => _instance; }

    #endregion Свойства

    #region Методы UNITY

    private void Awake()
    {
        if (_instance != null) { DebugUtils.LogInstanceAlreadyExists(this); Destroy(gameObject); } else _instance = this;
    }

    private void Start()
    {
        _hero = BattleUnit.Hero;
        if (_hero == null) DebugUtils.LogMissingReference(this, nameof(_hero));
    }

    private void LateUpdate()
    {
        UpdateUI();
    }

    #endregion Методы UNITY

    #region Личные методы

    private void UpdateUI()
    {
        _coinsInfoText.text = FormatNumber(CurrencyManager.Instance.Currency);
        //_attackInfoText.text = FormatNumber(_hero.Stats.AttackDamage);
        _attackInfoText.text = FormatNumber(0);
        _healthInfoText.text = FormatNumber(_hero.Stats.MaxHealth);

        if (_hero.Stats.AutoHealValue > 0)
        {
            _autoHealInfoObject.SetActive(true);
            _autoHealInfoText.text = FormatNumber(_hero.Stats.AutoHealValue);
            ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> AutoHeal info updated to <color=yellow>{0}</color>.", _ColoredDebug, _hero.Stats.AutoHealValue);
        }
        else
        {
            if (_autoHealInfoObject.activeSelf)
            {
                _autoHealInfoObject.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> AutoHeal info is now inactive.", _ColoredDebug);
            }
        }

        if (_hero.Stats.MaxEmergencySystemCharges > 0)
        {
            _emergencySystemInfoObject.SetActive(true);
            _emergencySystemInfoText.text = FormatNumber(_hero.Stats.MaxEmergencySystemCharges);
            ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> EmergencySystem info updated to <color=yellow>{0}</color>.", _ColoredDebug, _hero.Stats.MaxEmergencySystemCharges);
        }
        else
        {
            if (_emergencySystemInfoObject.activeSelf)
            {
                _emergencySystemInfoObject.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> EmergencySystem info is now inactive.", _ColoredDebug);
            }
        }

        if (_hero.Stats.MaxElectroShieldCharges > 0)
        {
            _electroShieldInfoObject.SetActive(true);
            _electroShieldInfoText.text = $"{_hero.Stats.MaxElectroShieldCharges}/{_hero.Stats.MaxElectroShieldCharges}";
            ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> ElectroShield info updated to <color=yellow>{0}/{1}</color>.", _ColoredDebug, _hero.Stats.MaxElectroShieldCharges, _hero.Stats.MaxElectroShieldCharges);
        }
        else
        {
            if (_electroShieldInfoObject.activeSelf)
            {
                _electroShieldInfoObject.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> ElectroShield info is now inactive.", _ColoredDebug);
            }
        }

        if (_hero.Stats.MaxIronVestCharges > 0)
        {
            _ironcladInfoObject.SetActive(true);
            _ironcladInfoText.text = $"{_hero.Stats.MaxIronVestCharges}/{_hero.Stats.MaxIronVestCharges}";
            ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> Ironclad info updated to <color=yellow>{0}/{1}</color>.", _ColoredDebug, _hero.Stats.MaxIronVestCharges, _hero.Stats.MaxIronVestCharges);
        }
        else
        {
            if (_ironcladInfoObject.activeSelf)
            {
                _ironcladInfoObject.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> Ironclad info is now inactive.", _ColoredDebug);
            }
        }

        if (_hero.Stats.BacklashChance > 0)
        {
            _backlashInfoObject.SetActive(true);
            _backlashInfoText.text = $"{Mathf.RoundToInt(_hero.Stats.BacklashChance * 100)}%";
            ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> Backlash info updated to <color=yellow>{0}%</color>.", _ColoredDebug, Mathf.RoundToInt(_hero.Stats.BacklashChance * 100));
        }
        else
        {
            if (_backlashInfoObject.activeSelf)
            {
                _backlashInfoObject.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> Backlash info is now inactive.", _ColoredDebug);
            }
        }

        if (_hero.Stats.CriticalHitChance > 0)
        {
            _criticalHitInfoObject.SetActive(true);
            _criticalHitInfoText.text = $"{_hero.Stats.CriticalHitChance}%";
            ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> CriticalHit info updated to <color=yellow>{0}%</color>.", _ColoredDebug, _hero.Stats.CriticalHitChance);
        }
        else
        {
            if (_criticalHitInfoObject.activeSelf)
            {
                _criticalHitInfoObject.SetActive(false);
                ColoredDebug.CLog(gameObject, "<color=cyan>PlayerInfoPanelUI:</color> CriticalHit info is now inactive.", _ColoredDebug);
            }
        }
    }

    private string FormatNumber(int value)
    {
        if (value >= 1000000)
        {
            return (value / 1000000f).ToString("0.#").Replace(",", ".") + "M";
        }
        if (value >= 10000)
        {
            return (value / 1000).ToString() + "k";
        }
        if (value >= 1000)
        {
            return (value / 1000f).ToString("0.#").Replace(",", ".") + "k";
        }
        return value.ToString();
    }

    #endregion Личные методы
}