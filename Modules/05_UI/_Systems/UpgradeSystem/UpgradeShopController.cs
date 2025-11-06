// НАЗНАЧЕНИЕ: Является главным фасадом для UI магазина улучшений. Оркестрирует работу дочерних компонентов: аниматора, генератора карточек и обработчика покупок.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: ShopAnimator, ShopCardGenerator, ShopPurchaseHandler.
// ПРИМЕЧАНИЕ: Не содержит сложной логики, а лишь делегирует вызовы специализированным классам.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeShopController : MonoBehaviour
{
    public event UnityAction OnCardManagerFinished;

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ShopAnimator _shopAnimator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ShopCardGenerator _cardGenerator;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ShopPurchaseHandler _purchaseHandler;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private static UpgradeShopController _instance;
    #endregion Поля

    #region Свойства
    /// <summary>
    /// Предоставляет глобальный доступ к экземпляру UpgradeShopController.
    /// </summary>
    public static UpgradeShopController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UpgradeShopController>();
            }
            return _instance;
        }
    }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this) { DebugUtils.LogInstanceAlreadyExists(this, _instance); Destroy(gameObject); return; } else _instance = this;

        if (_shopAnimator == null) DebugUtils.LogMissingReference(this, nameof(_shopAnimator));
        if (_cardGenerator == null) DebugUtils.LogMissingReference(this, nameof(_cardGenerator));
        if (_purchaseHandler == null) DebugUtils.LogMissingReference(this, nameof(_purchaseHandler));
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Отображает UI магазина для указанной категории.
    /// </summary>
    /// <param name="category">Категория карт для отображения (Shop, Hospital и т.д.).</param>
    [Button]
    public void ShowShop(UpgradeCardDataSO.CardTCategory category)
    {
        ColoredDebug.CLog(gameObject, $"<color=green>UpgradeShopController:</color> Запрос на показ магазина. Категория: <color=yellow>{category}</color>.", _ColoredDebug);
        _purchaseHandler.ResetSession();
        List<UpgradeCardUI> spawnedCards = _cardGenerator.GenerateCards(category);

        if (Settings.AutoBuyUpgradesInShop)
        {
            _purchaseHandler.AutoBuyUpgrades(spawnedCards);
            OnCardManagerFinished?.Invoke();
        }
        else
        {
            _shopAnimator.Show(spawnedCards, category);
        }
    }

    /// <summary>
    /// Обрабатывает выбор (покупку) карты игроком.
    /// </summary>
    /// <param name="selectedCard">UI-компонент выбранной карты.</param>
    public void OnCardSelected(UpgradeCardUI selectedCard)
    {
        _purchaseHandler.ProcessCardPurchase(selectedCard, _cardGenerator.CurrentCategory, cards =>
        {
            _cardGenerator.RemoveCard(selectedCard);
            _shopAnimator.AnimateCardPurchase(selectedCard, cards);
        });
    }

    /// <summary>
    /// Запускает процесс закрытия магазина.
    /// </summary>
    public void CloseShop()
    {
        SoundManager.Instance.PlayOneShot(SoundType.ButtonClick);
        _shopAnimator.Close(() =>
        {
            OnCardManagerFinished?.Invoke();
        });
    }
    #endregion Публичные методы
}