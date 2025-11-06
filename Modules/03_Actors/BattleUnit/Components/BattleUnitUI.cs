// НАЗНАЧЕНИЕ: Управляет всеми UI-компонентами, связанными с конкретным BattleUnit (здоровье, щиты, иконки статусов). Является главным посредником между данными юнита и их визуальным представлением.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: HealthCubesUI, ShieldCubesUI, IronVestCubesUI, AimingUI, FloatingTextManager.
// ПРИМЕЧАНИЕ: Отвечает за инициализацию, обновление и отображение/скрытие всех дочерних UI-элементов.
using Sirenix.OdinInspector;
using UnityEngine;

public class BattleUnitUI : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnit _battleUnit;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StatCubesUI _healthCubesUI;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StatCubesUI _shieldCubesUI;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StatCubesUI _ironVestCubesUI;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private AimingUI _aimingUI;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _electroShieldVisual;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isAiming = false;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public bool IsAiming { get => _isAiming; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_battleUnit == null) DebugUtils.LogMissingReference(this, nameof(_battleUnit));
        if (_healthCubesUI == null) DebugUtils.LogMissingReference(this, nameof(_healthCubesUI));
        if (_shieldCubesUI == null) DebugUtils.LogMissingReference(this, nameof(_shieldCubesUI));
        if (_ironVestCubesUI == null) DebugUtils.LogMissingReference(this, nameof(_ironVestCubesUI));
        if (_aimingUI == null) DebugUtils.LogMissingReference(this, nameof(_aimingUI));
        if (_electroShieldVisual == null) DebugUtils.LogMissingReference(this, nameof(_electroShieldVisual));
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Инициализирует все UI компоненты на основе статов юнита.
    /// </summary>
    /// <param name="unit">Юнит, к которому привязан этот UI.</param>
    public void Initialize(BattleUnit unit)
    {
        var stats = unit.Stats;
        var abilities = unit.Abilities;

        _shieldCubesUI.gameObject.SetActive(false);
        _ironVestCubesUI.gameObject.SetActive(false);
        _electroShieldVisual.SetActive(false);
        if (_aimingUI != null) _aimingUI.Hide(); // Принудительно скрываем UI прицеливания

        // Инициализация здоровья
        _healthCubesUI.Initialize(stats.MaxHealth);

        // Инициализация щитов (если есть)
        if (stats.MaxElectroShieldCharges > 0)
        {
            _shieldCubesUI.gameObject.SetActive(true);
            _shieldCubesUI.Initialize(stats.MaxElectroShieldCharges);
            _shieldCubesUI.SetCurrentValue(abilities.CurrentElectroShieldCharges);
            _electroShieldVisual.SetActive(abilities.CurrentElectroShieldCharges > 0);
        }

        // Инициализация бронежилета (если есть)
        if (stats.MaxIronVestCharges > 0)
        {
            _ironVestCubesUI.gameObject.SetActive(true);
            _ironVestCubesUI.Initialize(stats.MaxIronVestCharges);
            _ironVestCubesUI.SetCurrentValue(abilities.CurrentIronVestCharges);
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitUI:</color> UI инициализирован для юнита <color=yellow>{0}</color>.", _ColoredDebug, unit.name);
    }


    public void UpdateHealthDisplay(int currentHealth, int? maxHealth = null)
    {
        if (maxHealth.HasValue) _healthCubesUI.SetMaxValue(maxHealth.Value);
        _healthCubesUI.SetCurrentValue(currentHealth);
    }

    public void UpdateShieldDisplay(int charges) => _shieldCubesUI.SetCurrentValue(charges);
    public void UpdateVestDisplay(int charges) => _ironVestCubesUI.SetCurrentValue(charges);
    public void InitializeShields(int maxCharges) => _shieldCubesUI.Initialize(maxCharges);
    public void InitializeVests(int maxCharges) => _ironVestCubesUI.Initialize(maxCharges);
    public void SetElectroShieldActive(bool active) => _shieldCubesUI.gameObject.SetActive(active);
    public void SetIronVestActive(bool active) => _ironVestCubesUI.gameObject.SetActive(active);
    public void SetElectroShieldVisual(bool active) => _electroShieldVisual.SetActive(active);
    public void ShowAimingUI(int turns)
    {
        if (_aimingUI != null) _aimingUI.Show(turns);
        _isAiming = true;
        if (_battleUnit.CurrentUnitType == BattleUnit.UnitType_DEPRECATED.Ranged) SoundManager.Instance.PlayOneShot(SoundType.ReloadGun); ;
    }

    public void HideAimingUI()
    {
        if (_aimingUI != null && _aimingUI.gameObject.activeInHierarchy) _aimingUI.Hide();
        _isAiming = false;
    }

    public void ShowEvasionText() => ShowFloatingText("DODGE", FloatingText.TextType.Neutral);
    public void ShowDamageText(int damage, bool isCritical) => ShowFloatingText(damage.ToString(), isCritical ? FloatingText.TextType.CriticalDamage : FloatingText.TextType.Damage);
    public void ShowHealText(int amount) => ShowFloatingText(amount.ToString(), FloatingText.TextType.Heal);
    public void ShowShieldLossText(int amount) => ShowFloatingText(amount.ToString(), FloatingText.TextType.Shield);
    public void ShowVestLossText(int amount) => ShowFloatingText(amount.ToString(), FloatingText.TextType.Vest);
    public void ShowBountyText(int amount)
    {
        Vector3 position = BattleUnit.Hero.transform.position + new Vector3(0, 2, 0);
        ShowFloatingText(amount.ToString(), FloatingText.TextType.Coin, position);
    }
    #endregion Публичные методы

    #region Личные методы
    private void ShowFloatingText(string text, FloatingText.TextType type, Vector3? position = null)
    {
        GameObject textObj = ObjectPoolFloatingText.Instance.RetrieveObject();
        FloatingText floatingText = textObj.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            textObj.transform.position = position ?? _battleUnit.DamagePoint.position;
            floatingText.SetText(text, type);
        }
    }
    #endregion Личные методы
}