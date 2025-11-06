// НАЗНАЧЕНИЕ: Хранит и управляет базовыми и производными характеристиками боевой единицы. Отвечает за расчет бонусов от уровня и применение апгрейдов, включая здоровье и урон оружия.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit, BattleUnitHealth, BattleUnitUI, BattleUnitAbilities, BattleUnitArsenal.
// ПРИМЕЧАНИЕ: Инициализируется один раз, сохраняя базовые значения из инспектора, чтобы корректно пересчитывать характеристики при смене уровня.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnitStats : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleUnitArsenal _battleUnitArsenal;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS")]
    [BoxGroup("SETTINGS/Base Stats"), InfoBox("Размер юнита (Ширина x Высота). Юнит 'строится' вверх и вправо от своей якорной клетки на сетке."), SerializeField]
    private Vector2Int _unitSize = Vector2Int.one;
    [BoxGroup("SETTINGS/Base Stats"), SerializeField] private int _maxHealth = 2;
    [BoxGroup("SETTINGS/Base Stats"), SerializeField] private int _maxActionPoints = 2;
    [BoxGroup("SETTINGS/Base Stats"), SerializeField] private int _bounty = 1;

    [BoxGroup("SETTINGS/Offense"), SerializeField] private int _criticalHitChance = 5;
    [BoxGroup("SETTINGS/Defense"), SerializeField, Range(0f, 1f)] private float _evasionChance = 0.05f;
    [BoxGroup("SETTINGS/Defense"), SerializeField, Range(0f, 1f)] private float _criticalHitEvasionChance = 0.66f;
    [BoxGroup("SETTINGS/Defense"), SerializeField] private int _maxElectroShieldCharges = 0;
    [BoxGroup("SETTINGS/Defense"), SerializeField] private int _maxIronVestCharges = 0;
    [BoxGroup("SETTINGS/Abilities"), SerializeField] private int _autoHealValue = 0;
    [BoxGroup("SETTINGS/Abilities"), SerializeField] private int _maxEmergencySystemCharges = 0;
    [BoxGroup("SETTINGS/Abilities"), SerializeField, Range(0f, 1f)] private float _backlashChance = 0f;
    [BoxGroup("SETTINGS/Abilities"), SerializeField] private int _backlashMinDistance = 2;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _baseMaxHealth;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _baseMaxActionPoints;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _baseBounty;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _baseCriticalHitChance;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _baseEvasionChance;
    [BoxGroup("DEBUG"), ShowInInspector, ReadOnly] private Dictionary<AttackMode, int> _baseAttackDamages = new Dictionary<AttackMode, int>();
    #endregion Поля

    #region Свойства
    public Vector2Int UnitSize { get => _unitSize; }
    public int MaxHealth { get => _maxHealth; }
    public int MaxActionPoints { get => _maxActionPoints; }
    public int Bounty { get => _bounty; }
    public int CriticalHitChance { get => _criticalHitChance; }
    public float EvasionChance { get => _evasionChance; }
    public float CriticalHitEvasionChance { get => _criticalHitEvasionChance; }
    public int MaxElectroShieldCharges { get => _maxElectroShieldCharges; }
    public int MaxIronVestCharges { get => _maxIronVestCharges; }
    public int AutoHealValue { get => _autoHealValue; }
    public int MaxEmergencySystemCharges { get => _maxEmergencySystemCharges; }
    public float BacklashChance { get => _backlashChance; }
    public int BacklashMinDistance { get => _backlashMinDistance; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_battleUnitArsenal == null) DebugUtils.LogMissingReference(this, nameof(_battleUnitArsenal));
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Инициализирует статы, сохраняя базовые значения из инспектора, и применяет бонусы от начального уровня.
    /// </summary>
    /// <param name="level">Начальный уровень юнита.</param>
    public void Initialize(BattleUnit.UnitLevel level)
    {
        // Сохраняем начальные значения всех характеристик из инспектора
        _baseMaxHealth = _maxHealth;
        _baseMaxActionPoints = _maxActionPoints;
        _baseBounty = _bounty;
        _baseCriticalHitChance = _criticalHitChance;
        _baseEvasionChance = _evasionChance;

        _baseAttackDamages.Clear();
        foreach (var weapon in _battleUnitArsenal.Weapons)
        {
            foreach (var mode in weapon.AttackModes)
            {
                _baseAttackDamages[mode] = mode.damage;
            }
        }

        ColoredDebug.CLog(gameObject, "<color=blue>BattleUnitStats:</color> Сохранены базовые статы. Здоровье: <color=yellow>{0}</color>, ОД: <color=yellow>{1}</color>, Базовый урон сохранен для <color=yellow>{2}</color> режимов.", _ColoredDebug, _baseMaxHealth, _baseMaxActionPoints, _baseAttackDamages.Count);

        // Применяем бонусы от уровня
        ApplyLevelBonus(level);
    }

    /// <summary>
    /// Применяет бонус к характеристикам в зависимости от уровня юнита.
    /// </summary>
    /// <param name="levelEnum">Уровень юнита.</param>
    public void ApplyLevelBonus(BattleUnit.UnitLevel levelEnum)
    {
        // Сбрасываем текущие статы к базовым перед пересчетом
        _maxHealth = _baseMaxHealth;
        _maxActionPoints = _baseMaxActionPoints;
        _bounty = _baseBounty;
        _criticalHitChance = _baseCriticalHitChance;
        _evasionChance = _baseEvasionChance;

        int level = (int)levelEnum;
        if (level == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=blue>BattleUnitStats:</color> Уровень 0, бонусы не применяются.", _ColoredDebug);
            return;
        }

        _maxHealth = _baseMaxHealth + Mathf.RoundToInt(_baseMaxHealth * level * 0.5f);
        _bounty = _baseBounty + Mathf.RoundToInt(_baseBounty * level * 0.5f);

        foreach (var entry in _baseAttackDamages)
        {
            AttackMode mode = entry.Key;
            int baseDamage = entry.Value;
            int oldDamage = mode.damage;
            mode.damage = baseDamage + (level / 2);
            ColoredDebug.CLog(gameObject, "<color=blue>BattleUnitStats:</color> Урон для режима <color=white>'{0}'</color> увеличен с <color=yellow>{1}</color> до <color=lime>{2}</color>.", _ColoredDebug, mode.modeName, oldDamage, mode.damage);
        }

        ColoredDebug.CLog(gameObject, "<color=blue>BattleUnitStats:</color> Применены бонусы для уровня <color=yellow>{0}</color>. Новое здоровье: <color=lime>{1}</color>.", _ColoredDebug, level, _maxHealth);
    }

    /// <summary>
    /// Увеличивает максимальное здоровье юнита и исцеляет его на это же значение.
    /// </summary>
    public void IncreaseMaxHealth(int amount, BattleUnitHealth health, BattleUnitUI ui)
    {
        _maxHealth += amount;
        health.Heal(amount);
        ui.UpdateHealthDisplay(health.CurrentHealth, _maxHealth);
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitStats:</color> Макс. здоровье увеличено на <color=lime>{0}</color>. Текущее значение: <color=yellow>{1}</color>.", _ColoredDebug, amount, _maxHealth);
    }

    /// <summary>
    /// Устаревший метод. Урон теперь управляется через WeaponData.
    /// </summary>
    public void IncreaseAttackDamage(int amount)
    {
        ColoredDebug.CLog(gameObject, "<color=orange>BattleUnitStats:</color> IncreaseAttackDamage устарел. Урон управляется через WeaponData и бонусы уровня.", _ColoredDebug);
    }

    /// <summary>
    /// Применяет улучшение авто-лечения.
    /// </summary>
    public void ApplyAutoHeal(int bonusValue)
    {
        _autoHealValue += bonusValue;
    }

    /// <summary>
    /// Применяет улучшение электрощита.
    /// </summary>
    public void ApplyElectroShield(int bonusValue, BattleUnitAbilities abilities, BattleUnitUI ui)
    {
        bool wasUnlocked = _maxElectroShieldCharges > 0;
        _maxElectroShieldCharges += bonusValue;
        abilities.ReplenishElectroShields();

        if (!wasUnlocked)
        {
            ui.SetElectroShieldActive(true);
        }
        ui.InitializeShields(_maxElectroShieldCharges);
        ui.SetElectroShieldVisual(true);
    }

    /// <summary>
    /// Применяет улучшение бронежилета.
    /// </summary>
    public void ApplyIronVest(int bonusValue, BattleUnitAbilities abilities, BattleUnitUI ui)
    {
        bool wasUnlocked = _maxIronVestCharges > 0;
        _maxIronVestCharges += bonusValue;
        abilities.ReplenishIronVest();

        if (!wasUnlocked)
        {
            ui.SetIronVestActive(true);
        }
        ui.InitializeVests(_maxIronVestCharges);
    }

    /// <summary>
    /// Применяет улучшение системы экстренной помощи.
    /// </summary>
    public void ApplyEmergencySystem(int bonusValue, BattleUnitAbilities abilities)
    {
        _maxEmergencySystemCharges += bonusValue;
        abilities.AddEmergencySystemCharges(bonusValue);
    }

    /// <summary>
    /// Применяет улучшение ответного удара.
    /// </summary>
    public void ApplyBacklash(int bonusValue)
    {
        _backlashChance = Mathf.Clamp01(_backlashChance + bonusValue / 100f);
    }

    /// <summary>
    /// Применяет улучшение критического удара.
    /// </summary>
    public void ApplyCriticalHit(int bonusValue)
    {
        _criticalHitChance += bonusValue;
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleUnitStats:</color> Шанс крит. удара увеличен на <color=lime>{0}%</color>. Текущее значение: <color=yellow>{1}%</color>.", _ColoredDebug, bonusValue, _criticalHitChance);
    }
    #endregion Публичные методы
}