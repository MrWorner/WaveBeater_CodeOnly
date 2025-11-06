// НАЗНАЧЕНИЕ: Управляет применением всех улучшений к герою и отслеживает стоимость карточек улучшений.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleUnit (Hero), BattleGridGenerator, UpgradeCardDataSO.
// ПРИМЕЧАНИЕ: Является центральным сервисом для изменения характеристик героя и состояния игры через систему улучшений.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleGridGenerator _gridGenerator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, ReadOnly] private BattleUnit _hero;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), ShowInInspector, ReadOnly] private Dictionary<UpgradeCardDataSO, int> _currentCardCosts = new Dictionary<UpgradeCardDataSO, int>();
    private static UpgradeManager _instance;
    #endregion Поля

    #region Свойства
    /// <summary>
    /// Предоставляет глобальный доступ к экземпляру UpgradeManager.
    /// </summary>
    public static UpgradeManager Instance { get => _instance; }

    /// <summary>
    /// Возвращает ссылку на генератор боевой сетки.
    /// </summary>
    public BattleGridGenerator GridGenerator { get => _gridGenerator; }

    /// <summary>
    /// Возвращает словарь с текущими стоимостями всех карт.
    /// </summary>
    public Dictionary<UpgradeCardDataSO, int> CurrentCardCosts { get => _currentCardCosts; set => _currentCardCosts = value; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null) { DebugUtils.LogInstanceAlreadyExists(this); Destroy(gameObject); } else { _instance = this; }
        if (_gridGenerator == null) DebugUtils.LogMissingReference(this, nameof(_gridGenerator));
    }

    private void Start()
    {
        _hero = BattleUnit.Hero;
        if (_hero == null) DebugUtils.LogMissingReference(this, nameof(_hero));
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Применяет бонус от указанной карты к герою.
    /// </summary>
    /// <param name="cardData">Данные карты улучшения.</param>
    public void ApplyBonus(UpgradeCardDataSO cardData)
    {
        int bonusValue = cardData.BonusValue;
        ColoredDebug.CLog(gameObject, "<color=lightblue>UpgradeManager:</color> Применение бонуса <color=white>{0}</color> со значением <color=lime>{1}</color>.", _ColoredDebug, cardData.BonusType, bonusValue);
        switch (cardData.BonusType)
        {
            case UpgradeCardDataSO.CardTypeBonus.Health: ApplyHealthUpgrade(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.Damage: ApplyDamageUpgrade(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.Heal: ApplyHeal(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.Money: ApplyMoney(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.AutoHeal: ApplyAutoHeal(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.ElectroShield: ApplyElectroShield(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.EmergencySystem: ApplyEmergencySystem(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.Backlash: ApplyBacklash(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.CriticalHit: ApplyCriticalHit(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.DoubleMoney: ApplyDoubleMoney(bonusValue); break;
            case UpgradeCardDataSO.CardTypeBonus.Ironclad: ApplyIronclad(bonusValue); break;
        }
    }

    /// <summary>
    /// Получает текущую стоимость карты, инициализируя ее, если она еще не отслеживается.
    /// </summary>
    /// <param name="cardData">Данные карты.</param>
    /// <returns>Текущая стоимость карты.</returns>
    public int GetCardCost(UpgradeCardDataSO cardData)
    {
        if (_currentCardCosts.ContainsKey(cardData))
        {
            return _currentCardCosts[cardData];
        }
        else
        {
            _currentCardCosts.Add(cardData, cardData.InitialCost);
            return cardData.InitialCost;
        }
    }

    /// <summary>
    /// Увеличивает стоимость карты после покупки.
    /// </summary>
    /// <param name="cardData">Данные купленной карты.</param>
    public void IncreaseCardCost(UpgradeCardDataSO cardData)
    {
        if (cardData.CanIncreaseCost == false)
        {
            return;
        }

        if (_currentCardCosts.ContainsKey(cardData))
        {
            _currentCardCosts[cardData] += cardData.CostIncrease;
            ColoredDebug.CLog(gameObject, "<color=green>UpgradeManager:</color> Обновлена стоимость карты <color=yellow>{0}</color>. Новая цена: <color=white>{1}</color>.", _ColoredDebug, cardData.Title, _currentCardCosts[cardData]);
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>UpgradeManager:</color> Попытка обновить стоимость карты <color=yellow>'{0}'</color>, которой нет в словаре.", _ColoredDebug, cardData.Title);
        }
    }

    /// <summary>
    /// Применяет улучшение максимального здоровья.
    /// </summary>
    /// <param name="bonusValue">Значение для увеличения.</param>
    public void ApplyHealthUpgrade(int bonusValue)
    {
        _hero.Stats.IncreaseMaxHealth(bonusValue, _hero.Health, _hero.UI);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение здоровья. Максимальное здоровье героя увеличено на <color=lime>{0}</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Применяет улучшение урона.
    /// </summary>
    /// <param name="bonusValue">Значение для увеличения.</param>
    public void ApplyDamageUpgrade(int bonusValue)
    {
        _hero.Stats.IncreaseAttackDamage(bonusValue);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение урона. Урон героя увеличен на <color=lime>{0}</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Исцеляет героя на процент от его максимального здоровья.
    /// </summary>
    /// <param name="bonusValue">Процент для исцеления.</param>
    public void ApplyHeal(int bonusValue)
    {
        float healthIncrease = _hero.Stats.MaxHealth * (bonusValue / 100f);
        int roundedIncrease = Mathf.RoundToInt(healthIncrease);
        _hero.Health.Heal(roundedIncrease);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Герой исцелен. Восстановлено <color=lime>{0}</color> HP, что составляет <color=yellow>{1}%</color> от максимального здоровья.", _ColoredDebug, roundedIncrease, bonusValue);
    }

    /// <summary>
    /// Увеличивает ширину игрового поля.
    /// </summary>
    /// <param name="bonusValue">Количество колонок для добавления.</param>
    public void ApplyGridWidthUpgrade(int bonusValue)
    {
        _gridGenerator.IncreaseWidth(bonusValue);
        _gridGenerator.GenerateField();
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Поле расширено на <color=lime>{0}</color>!", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Добавляет валюту игроку.
    /// </summary>
    /// <param name="bonusValue">Количество добавляемой валюты.</param>
    public void ApplyMoney(int bonusValue)
    {
        CurrencyManager.Instance.AddCurrency(bonusValue);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Добавлены деньги! Бонус: <color=lime>{0}</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Применяет улучшение автоматического исцеления.
    /// </summary>
    /// <param name="bonusValue">Значение авто-исцеления.</param>
    public void ApplyAutoHeal(int bonusValue)
    {
        _hero.Stats.ApplyAutoHeal(bonusValue);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение авто-лечения. Бонус: <color=lime>{0}</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Применяет улучшение "Электрощит".
    /// </summary>
    /// <param name="bonusValue">Количество зарядов.</param>
    public void ApplyElectroShield(int bonusValue)
    {
        _hero.Stats.ApplyElectroShield(bonusValue, _hero.Abilities, _hero.UI);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение 'Электрощит'. Бонус: <color=lime>{0}</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Применяет улучшение "Система экстренной помощи".
    /// </summary>
    /// <param name="bonusValue">Количество зарядов.</param>
    public void ApplyEmergencySystem(int bonusValue)
    {
        _hero.Stats.ApplyEmergencySystem(bonusValue, _hero.Abilities);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение 'Система экстренного реагирования'. Бонус: <color=lime>{0}</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Применяет улучшение ответного удара "Backlash".
    /// </summary>
    /// <param name="bonusValue">Процент увеличения шанса.</param>
    public void ApplyBacklash(int bonusValue)
    {
        _hero.Stats.ApplyBacklash(bonusValue);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение 'Backlash'. Бонус: <color=lime>{0}%</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Применяет улучшение шанса критического удара.
    /// </summary>
    /// <param name="bonusValue">Процент увеличения шанса.</param>
    public void ApplyCriticalHit(int bonusValue)
    {
        _hero.Stats.ApplyCriticalHit(bonusValue);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение 'Critical Hit'. Бонус: <color=lime>+{0}%</color>.", _ColoredDebug, bonusValue);
    }

    /// <summary>
    /// Удваивает текущее количество валюты.
    /// </summary>
    /// <param name="bonusValue">Множитель (обычно 2).</param>
    public void ApplyDoubleMoney(int bonusValue)
    {
        if (CurrencyManager.Instance.Currency <= 0)
        {
            return;
        }
        CurrencyManager.Instance.SetCurrency(CurrencyManager.Instance.Currency * bonusValue);
    }

    /// <summary>
    /// Применяет улучшение "Бронежилет".
    /// </summary>
    /// <param name="bonusValue">Количество зарядов.</param>
    public void ApplyIronclad(int bonusValue)
    {
        _hero.Stats.ApplyIronVest(bonusValue, _hero.Abilities, _hero.UI);
        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeManager:</color> Применено улучшение 'Бронежилет'. Бонус: <color=lime>{0}</color>.", _ColoredDebug, bonusValue);
    }
    #endregion Публичные методы
}