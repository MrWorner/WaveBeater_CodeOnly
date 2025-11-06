// НАЗНАЧЕНИЕ: Управляет всеми визуальными анимациями UI магазина, используя DOTween.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: DOTween, CanvasGroup, RectTransform.
// ПРИМЕЧАНИЕ: Этот класс отвечает исключительно за визуальное представление и не содержит игровой логики.
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShopAnimator : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _background;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private RectTransform _panel;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private CanvasGroup _panelCanvasGroup;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _exitButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private CanvasGroup _exitButtonCanvasGroup;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _backgroundAlpha = 0.5f;
    [BoxGroup("SETTINGS"), SerializeField] private Vector2 _hiddenPosition;
    [BoxGroup("SETTINGS"), SerializeField] private Vector2 _shownPosition;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_background == null) DebugUtils.LogMissingReference(this, nameof(_background));
        if (_panel == null) DebugUtils.LogMissingReference(this, nameof(_panel));
        if (_panelCanvasGroup == null) DebugUtils.LogMissingReference(this, nameof(_panelCanvasGroup));
        if (_exitButton == null) DebugUtils.LogMissingReference(this, nameof(_exitButton));
        if (_exitButtonCanvasGroup == null) DebugUtils.LogMissingReference(this, nameof(_exitButtonCanvasGroup));

        _panel.anchoredPosition = _hiddenPosition;
        Color tempColor = _background.color;
        tempColor.a = 0f;
        _background.color = tempColor;

        _background.gameObject.SetActive(false);
        _panel.gameObject.SetActive(false);
        _exitButtonCanvasGroup.alpha = 0;
        _exitButtonCanvasGroup.interactable = false;

        _exitButton.onClick.AddListener(() => UpgradeShopController.Instance.CloseShop());
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Анимирует появление магазина и всех его элементов.
    /// </summary>
    /// <param name="spawnedCards">Список созданных карточек для анимации.</param>
    /// <param name="category">Текущая категория магазина для определения логики кнопки выхода.</param>
    public void Show(List<UpgradeCardUI> spawnedCards, UpgradeCardDataSO.CardTCategory category)
    {
        ColoredDebug.CLog(gameObject, "<color=green>ShopAnimator:</color> Запуск анимации появления магазина.", _ColoredDebug);
        KillAllTweens();

        Sequence sequence = DOTween.Sequence();
        _background.gameObject.SetActive(true);
        _panel.gameObject.SetActive(true);

        sequence.Append(_background.DOFade(_backgroundAlpha, 0.3f));
        sequence.Join(_panel.DOAnchorPos(_shownPosition, 0.5f).SetEase(Ease.OutCubic));

        if (category == UpgradeCardDataSO.CardTCategory.Shop)
        {
            _exitButton.gameObject.SetActive(true);
            _exitButtonCanvasGroup.interactable = true;
            sequence.Insert(0.5f, _exitButtonCanvasGroup.DOFade(1f, 0.4f));
        }
        else
        {
            _exitButton.gameObject.SetActive(false);
        }

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            UpgradeCardUI card = spawnedCards[i];
            var canvasGroup = card.GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            card.transform.localRotation = Quaternion.Euler(0, 45, 0);
            card.transform.localScale = Vector3.one * 0.9f;
            float delay = 0.5f * 0.5f + (i * 0.1f);
            sequence.Insert(delay, canvasGroup.DOFade(1f, 0.4f));
            sequence.Insert(delay, card.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            sequence.Insert(delay, card.transform.DOLocalRotateQuaternion(Quaternion.identity, 0.4f));
        }
    }

    /// <summary>
    /// Анимирует закрытие магазина.
    /// </summary>
    /// <param name="onComplete">Действие, которое будет вызвано после завершения анимации.</param>
    public void Close(UnityAction onComplete)
    {
        ColoredDebug.CLog(gameObject, "<color=magenta>ShopAnimator:</color> Запуск анимации закрытия магазина.", _ColoredDebug);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(_panelCanvasGroup.DOFade(0f, 0.4f));
        sequence.Join(_background.DOFade(0f, 0.3f));

        if (_exitButton.gameObject.activeSelf)
        {
            _exitButtonCanvasGroup.interactable = false;
            sequence.Join(_exitButtonCanvasGroup.DOFade(0f, 0.3f));
        }

        sequence.OnComplete(() =>
        {
            _background.gameObject.SetActive(false);
            _panel.gameObject.SetActive(false);
            _exitButton.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Анимирует выбор и исчезновение одной карты, после чего закрывает магазин.
    /// </summary>
    /// <param name="selectedCard">Выбранная карта.</param>
    /// <param name="allCards">Полный список карт для анимации.</param>
    /// <param name="onComplete">Действие, которое будет вызвано после завершения анимации.</param>
    public void AnimateSelectionAndClose(UpgradeCardUI selectedCard, List<UpgradeCardUI> allCards, UnityAction onComplete)
    {
        ColoredDebug.CLog(gameObject, "<color=magenta>ShopAnimator:</color> Запуск анимации выбора карты <color=white>{0}</color> и закрытия магазина.", _ColoredDebug, selectedCard.CardData.Title);
        foreach (var card in allCards)
        {
            if (card != null) card.ButtonGetBonus.interactable = false;
        }

        var cardsLayoutGroup = selectedCard.transform.parent.GetComponent<HorizontalLayoutGroup>();
        if (cardsLayoutGroup != null) cardsLayoutGroup.enabled = false;

        Sequence sequence = DOTween.Sequence();
        foreach (var card in allCards)
        {
            if (card != selectedCard)
            {
                sequence.Join(card.transform.DOScale(0.8f, 0.4f).SetEase(Ease.InBack));
                sequence.Join(card.GetComponent<CanvasGroup>().DOFade(0f, 0.3f));
            }
        }

        sequence.Append(selectedCard.transform.DOLocalMove(Vector3.zero, 0.4f).SetEase(Ease.OutCubic));
        sequence.Join(selectedCard.transform.DOScale(1.2f, 0.4f).SetEase(Ease.OutCubic));

        if (selectedCard.TextTitle != null) sequence.Join(selectedCard.TextTitle.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 0.5f));
        if (selectedCard.IconImage != null) sequence.Join(selectedCard.IconImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.4f));

        sequence.AppendInterval(0.8f);

        sequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// Анимирует "покупку" карты в магазине: карта сжимается и исчезает, остальные сдвигаются.
    /// </summary>
    /// <param name="selectedCard">Купленная карта.</param>
    /// <param name="remainingCards">Оставшиеся карты.</param>
    public void AnimateCardPurchase(UpgradeCardUI selectedCard, List<UpgradeCardUI> remainingCards)
    {
        StartCoroutine(AnimateCardPurchaseRoutine(selectedCard, remainingCards));
    }
    #endregion Публичные методы

    #region Личные методы
    private IEnumerator AnimateCardPurchaseRoutine(UpgradeCardUI selectedCard, List<UpgradeCardUI> remainingCards)
    {
        ColoredDebug.CLog(gameObject, "<color=magenta>ShopAnimator:</color> Анимация покупки карты <color=white>{0}</color>.", _ColoredDebug, selectedCard.CardData.Title);
        selectedCard.ButtonGetBonus.interactable = false;
        LayoutElement layoutElement = selectedCard.GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = selectedCard.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = (selectedCard.transform as RectTransform).sizeDelta.x;

        Sequence purchaseSequence = DOTween.Sequence();
        purchaseSequence.Append(DOTween.To(() => layoutElement.preferredWidth, x => layoutElement.preferredWidth = x, 0, 0.4f).SetEase(Ease.Linear));
        purchaseSequence.Join(selectedCard.GetComponent<CanvasGroup>().DOFade(0f, 0.2f));

        yield return purchaseSequence.WaitForCompletion();

        Destroy(selectedCard.gameObject);

        foreach (var card in remainingCards)
        {
            if (card != null && card.gameObject.activeSelf)
            {
                card.UpdateIndicators();
            }
        }

        if (selectedCard.transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(selectedCard.transform.parent as RectTransform);
        }
    }

    private void KillAllTweens()
    {
        _background.DOKill();
        _panel.DOKill();
        _panelCanvasGroup.DOKill();
        _exitButtonCanvasGroup.DOKill();
    }
    #endregion
}