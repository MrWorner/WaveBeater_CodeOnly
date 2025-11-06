// НАЗНАЧЕНИЕ: Отвечает за логику подбора и создания UI-карточек улучшений для магазина.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: UpgradeCardLibrary, UpgradeManager, CurrencyManager.
// ПРИМЕЧАНИЕ: Изолирует сложную логику фильтрации, сортировки и ценообразования карт от основного контроллера магазина.
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCardGenerator : MonoBehaviour
{
    [System.Serializable]
    public class CategoryShopData
    {
        public UpgradeCardDataSO.CardTCategory category;
        public string title;
        public int cardCount = 3;
    }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private UpgradeManager _upgradeManager;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _cardsParent;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _cardPrefab;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _titleText;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private HorizontalLayoutGroup _cardsLayoutGroup;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private List<CategoryShopData> _categoryShopSettings = new List<CategoryShopData>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<UpgradeCardUI> _spawnedCards = new List<UpgradeCardUI>();
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    private UpgradeCardDataSO _temporaryFreeCard = null;
    #endregion Поля

    #region Свойства
    /// <summary>
    /// Возвращает текущую активную категорию магазина.
    /// </summary>
    public UpgradeCardDataSO.CardTCategory CurrentCategory { get; private set; }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_upgradeManager == null) DebugUtils.LogMissingReference(this, nameof(_upgradeManager));
        if (_cardsParent == null) DebugUtils.LogMissingReference(this, nameof(_cardsParent));
        if (_cardPrefab == null) DebugUtils.LogMissingReference(this, nameof(_cardPrefab));
        if (_titleText == null) DebugUtils.LogMissingReference(this, nameof(_titleText));
        if (_cardsLayoutGroup == null) DebugUtils.LogMissingReference(this, nameof(_cardsLayoutGroup));
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Генерирует и отображает карточки для указанной категории.
    /// </summary>
    /// <param name="category">Категория для генерации.</param>
    /// <returns>Список созданных UI-компонентов карточек.</returns>
    public List<UpgradeCardUI> GenerateCards(UpgradeCardDataSO.CardTCategory category)
    {
        CurrentCategory = category;
        _temporaryFreeCard = null;

        var categoryData = _categoryShopSettings.FirstOrDefault(data => data.category == category);
        _titleText.text = categoryData != null ? categoryData.title : "Магазин";

        ClearExistingCards();

        int cardsToGenerate = categoryData?.cardCount ?? 3;
        List<UpgradeCardDataSO> potentialCards = UpgradeCardLibrary.Instance.GetRandomUniqueCards(cardsToGenerate, category);
        if (potentialCards.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>ShopCardGenerator:</color> Не найдено карт для генерации в категории <color=yellow>{0}</color>.", _ColoredDebug, category);
            return _spawnedCards;
        }

        List<UpgradeCardDataSO> cardsToDisplay = FilterAndSelectCards(potentialCards, category);

        foreach (var data in cardsToDisplay)
        {
            GameObject cardObject = Instantiate(_cardPrefab, _cardsParent);
            UpgradeCardUI cardUI = cardObject.GetComponent<UpgradeCardUI>();

            int currentCost = (_temporaryFreeCard != null && data == _temporaryFreeCard) ? 0 : _upgradeManager.GetCardCost(data);

            cardUI.Initialize(data, currentCost);
            _spawnedCards.Add(cardUI);
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>ShopCardGenerator:</color> Сгенерировано <color=yellow>{0}</color> карточек.", _ColoredDebug, _spawnedCards.Count);
        return _spawnedCards;
    }

    /// <summary>
    /// Удаляет карточку из внутреннего списка.
    /// </summary>
    /// <param name="cardToRemove">Карточка для удаления.</param>
    public void RemoveCard(UpgradeCardUI cardToRemove)
    {
        _spawnedCards.Remove(cardToRemove);
    }
    #endregion

    #region Личные методы
    private List<UpgradeCardDataSO> FilterAndSelectCards(List<UpgradeCardDataSO> potentialCards, UpgradeCardDataSO.CardTCategory category)
    {
        List<UpgradeCardDataSO> affordableCards = potentialCards
            .Where(card => _upgradeManager.GetCardCost(card) <= CurrencyManager.Instance.Currency)
            .ToList();

        List<UpgradeCardDataSO> cardsToDisplay;

        if (affordableCards.Count == 0 && category != UpgradeCardDataSO.CardTCategory.Treasure && category != UpgradeCardDataSO.CardTCategory.Award)
        {
            cardsToDisplay = new List<UpgradeCardDataSO>();
            var cheapestCard = potentialCards.OrderBy(c => _upgradeManager.GetCardCost(c)).FirstOrDefault();
            if (cheapestCard != null)
            {
                _temporaryFreeCard = cheapestCard;
                cardsToDisplay.Add(cheapestCard);
                ColoredDebug.CLog(gameObject, $"<color=yellow>ShopCardGenerator:</color> Игрок не может позволить себе ни одной карты. Карта '{cheapestCard.Title}' временно бесплатна.", _ColoredDebug);
            }
        }
        else
        {
            cardsToDisplay = affordableCards.Any() ? affordableCards : potentialCards;
        }

        return cardsToDisplay.OrderBy(data => _upgradeManager.GetCardCost(data)).ToList();
    }

    private void ClearExistingCards()
    {
        _cardsLayoutGroup.enabled = true;
        foreach (Transform child in _cardsParent)
        {
            Destroy(child.gameObject);
        }
        _spawnedCards.Clear();
        ColoredDebug.CLog(gameObject, "<color=orange>ShopCardGenerator:</color> Существующие карточки удалены.", _ColoredDebug);
    }
    #endregion
}