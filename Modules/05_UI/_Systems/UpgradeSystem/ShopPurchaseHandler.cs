// НАЗНАЧЕНИЕ: Обрабатывает всю логику покупки улучшений в магазине.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: UpgradeManager, CurrencyManager, ShopAnimator.
// ПРИМЕЧАНИЕ: Содержит логику проверки валюты, применения бонусов, а также авто-покупки для режима симуляции.
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopPurchaseHandler : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private UpgradeManager _upgradeManager;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ShopAnimator _shopAnimator;
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private HashSet<UpgradeCardDataSO.CardTypeBonus> _purchasedCardTypesThisSession = new HashSet<UpgradeCardDataSO.CardTypeBonus>();
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_upgradeManager == null) DebugUtils.LogMissingReference(this, nameof(_upgradeManager));
        if (_shopAnimator == null) DebugUtils.LogMissingReference(this, nameof(_shopAnimator));
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Сбрасывает состояние покупок для новой сессии магазина.
    /// </summary>
    public void ResetSession()
    {
        _purchasedCardTypesThisSession.Clear();
        ColoredDebug.CLog(gameObject, "<color=cyan>ShopPurchaseHandler:</color> Сессия покупок сброшена.", _ColoredDebug);
    }

    /// <summary>
    /// Обрабатывает покупку выбранной карты.
    /// </summary>
    /// <param name="selectedCard">Выбранная карта.</param>
    /// <param name="category">Текущая категория магазина.</param>
    /// <param name="onPurchaseSuccess">Действие, вызываемое при успешной покупке в режиме магазина.</param>
    public void ProcessCardPurchase(UpgradeCardUI selectedCard, UpgradeCardDataSO.CardTCategory category, Action<List<UpgradeCardUI>> onPurchaseSuccess)
    {
        if (category == UpgradeCardDataSO.CardTCategory.Shop && _purchasedCardTypesThisSession.Contains(selectedCard.CardData.BonusType))
        {
            ColoredDebug.CLog(gameObject, "<color=yellow>ShopPurchaseHandler:</color> Карта типа <color=white>{0}</color> уже была куплена в этой сессии. Покупка отменена.", _ColoredDebug, selectedCard.CardData.BonusType);
            selectedCard.transform.DOShakePosition(0.3f, new Vector3(10, 0, 0), 10, 90, false, true);
            return;
        }

        ColoredDebug.CLog(gameObject, "<color=yellow>ShopPurchaseHandler:</color> Выбрана карта: <color=white>{0}</color>. Стоимость: <color=lime>{1}</color>.", _ColoredDebug, selectedCard.CardData.Title, selectedCard.CurrentCost);

        if (CurrencyManager.Instance.SpendCurrency(selectedCard.CurrentCost))
        {
            SoundManager.Instance.PlayOneShot(SoundType.UpgradePurchase);
            ColoredDebug.CLog(gameObject, "<color=cyan>ShopPurchaseHandler:</color> Покупка совершена. Применение бонуса <color=white>{0}</color>.", _ColoredDebug, selectedCard.CardData.Title);
            _upgradeManager.ApplyBonus(selectedCard.CardData);

            if (category == UpgradeCardDataSO.CardTCategory.Shop)
            {
                _upgradeManager.IncreaseCardCost(selectedCard.CardData);
                _purchasedCardTypesThisSession.Add(selectedCard.CardData.BonusType);
                var remainingCards = selectedCard.transform.parent.GetComponentsInChildren<UpgradeCardUI>().Where(c => c != selectedCard).ToList();
                onPurchaseSuccess?.Invoke(remainingCards);
            }
            else
            {
                var allCards = selectedCard.transform.parent.GetComponentsInChildren<UpgradeCardUI>().ToList();
                _shopAnimator.AnimateSelectionAndClose(selectedCard, allCards, () => UpgradeShopController.Instance.CloseShop());
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=red>ShopPurchaseHandler:</color> Недостаточно средств! Требуется: <color=lime>{0}</color>, имеется: <color=lime>{1}</color>.", _ColoredDebug, selectedCard.CurrentCost, CurrencyManager.Instance.Currency);
        }
    }

    /// <summary>
    /// Запускает автоматическую покупку улучшений по заданному приоритету.
    /// </summary>
    /// <param name="spawnedCards">Список доступных для покупки карт.</param>
    public void AutoBuyUpgrades(List<UpgradeCardUI> spawnedCards)
    {
        var priority = new List<UpgradeCardDataSO.CardTypeBonus>
        {
            UpgradeCardDataSO.CardTypeBonus.AutoHeal, UpgradeCardDataSO.CardTypeBonus.ElectroShield,
            UpgradeCardDataSO.CardTypeBonus.SlowDownWave, UpgradeCardDataSO.CardTypeBonus.EmergencySystem,
            UpgradeCardDataSO.CardTypeBonus.Backlash, UpgradeCardDataSO.CardTypeBonus.CriticalHit,
            UpgradeCardDataSO.CardTypeBonus.Damage, UpgradeCardDataSO.CardTypeBonus.Health,
            UpgradeCardDataSO.CardTypeBonus.Ironclad, UpgradeCardDataSO.CardTypeBonus.Money,
            UpgradeCardDataSO.CardTypeBonus.Heal, UpgradeCardDataSO.CardTypeBonus.DoubleMoney
        };

        bool boughtSomething;
        do
        {
            boughtSomething = false;
            UpgradeCardUI cardToBuy = null;

            foreach (var bonusType in priority)
            {
                cardToBuy = spawnedCards
                    .Where(c => !_purchasedCardTypesThisSession.Contains(c.CardData.BonusType) &&
                                c.CardData.BonusType == bonusType &&
                                CurrencyManager.Instance.Currency >= c.CurrentCost)
                    .OrderBy(c => c.CurrentCost)
                    .FirstOrDefault();
                if (cardToBuy != null) break;
            }

            if (cardToBuy != null)
            {
                CurrencyManager.Instance.SpendCurrency(cardToBuy.CurrentCost);
                _upgradeManager.ApplyBonus(cardToBuy.CardData);
                _upgradeManager.IncreaseCardCost(cardToBuy.CardData);
                _purchasedCardTypesThisSession.Add(cardToBuy.CardData.BonusType);
                boughtSomething = true;
                ColoredDebug.CLog(gameObject, $"<color=green>ShopPurchaseHandler:</color> Авто-покупка: <color=yellow>{cardToBuy.CardData.Title}</color>", _ColoredDebug);
            }
        } while (boughtSomething);
    }
    #endregion
}