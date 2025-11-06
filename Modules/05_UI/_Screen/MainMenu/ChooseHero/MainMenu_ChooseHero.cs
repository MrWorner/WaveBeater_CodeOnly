using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using DG.Tweening;
using System.Linq;

public class MainMenu_ChooseHero : MonoBehaviour
{
    public static MainMenu_ChooseHero Instance { get; private set; }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private HeroLibrary _heroLibrary;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _heroCardsContainer;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _heroCardPrefab;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _backButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _nextButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _nextButtonAnticlicker;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Image _selectedHeroImage;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private TextMeshProUGUI _selectedHeroNameText;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private TextMeshProUGUI _selectedHeroDescriptionText;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<HeroCardUI> _spawnedCards = new List<HeroCardUI>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private HeroCardUI _selectedCard;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (Instance != null) { DebugUtils.LogInstanceAlreadyExists(this); } else { Instance = this; }

        if (_heroLibrary == null) DebugUtils.LogMissingReference(this, nameof(_heroLibrary));
        if (_heroCardsContainer == null) DebugUtils.LogMissingReference(this, nameof(_heroCardsContainer));
        if (_heroCardPrefab == null) DebugUtils.LogMissingReference(this, nameof(_heroCardPrefab));
        if (_backButton == null) DebugUtils.LogMissingReference(this, nameof(_backButton));
        if (_nextButton == null) DebugUtils.LogMissingReference(this, nameof(_nextButton));
        if (_nextButtonAnticlicker == null) DebugUtils.LogMissingReference(this, nameof(_nextButtonAnticlicker));

        _backButton.onClick.AddListener(OnBackButtonPressed);
        _nextButton.onClick.AddListener(OnNextButtonPressed);
    }

    private void Start()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>MainMenu_ChooseHero:</color> Запускаю инициализацию выбора героя.", _ColoredDebug);
        PopulateHeroSelection();
    }

    private void OnEnable()
    {
        // Каждый раз при активации панели сбрасываем выбор
        ResetSelection();
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void SelectHero(HeroDataSO selectedHero)
    {
        if (selectedHero == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>MainMenu_ChooseHero:</color> Попытка выбрать несуществующего героя. Операция отменена.", _ColoredDebug);
            return;
        }

        HeroCardUI clickedCard = _spawnedCards.FirstOrDefault(c => c.HeroData == selectedHero);
        if (clickedCard == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>MainMenu_ChooseHero:</color> Не удалось найти UI карточки для героя.", _ColoredDebug);
            return;
        }

        if (clickedCard.IsLocked)
        {
            ColoredDebug.CLog(gameObject, "<color=yellow>MainMenu_ChooseHero:</color> Выбран заблокированный герой. Выбор не изменен.", _ColoredDebug);
            return;
        }

        SoundManager.Instance.PlayOneShot(SoundType.ButtonClick);

        if (_selectedCard != null && _selectedCard != clickedCard)
        {
            _selectedCard.SetChosen(false);
        }

        _selectedCard = clickedCard;
        _selectedCard.SetChosen(true);

        UpdateNextButtonState(true);

        ColoredDebug.CLog(gameObject, $"<color=cyan>MainMenu_ChooseHero:</color> Выбран герой: <color=lime>{selectedHero.HeroName}</color>.", _ColoredDebug);
        UpdateHeroInfoPanel(selectedHero);

        GameInstance.Instance.SetSelectedHeroData(selectedHero);
        ColoredDebug.CLog(gameObject, $"<color=lime>MainMenu_ChooseHero:</color> Данные о герое <color=yellow>{selectedHero.HeroName}</color> сохранены в <color=orange>GameInstance</color>.", _ColoredDebug);

    }
    #endregion Публичные методы

    #region Личные методы
    private void OnBackButtonPressed()
    {
        ResetSelection(); // Сбрасываем выбор перед выходом
        MainMenu.Instance.GoToMainMenu();
    }

    private void OnNextButtonPressed()
    {
        MainMenu.Instance.GoToLevelSelection();
    }

    private void UpdateNextButtonState(bool heroIsSelected)
    {
        _nextButton.interactable = heroIsSelected;
        _nextButtonAnticlicker.SetActive(!heroIsSelected);
    }

    private void PopulateHeroSelection()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>MainMenu_ChooseHero:</color> Заполняю UI карточками героев...", _ColoredDebug);
        foreach (Transform child in _heroCardsContainer)
        {
            Destroy(child.gameObject);
        }
        _spawnedCards.Clear();

        var availableHeroes = _heroLibrary.AvailableHeroes;
        if (availableHeroes == null || availableHeroes.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=red>MainMenu_ChooseHero:</color> Список доступных героев в HeroLibrary пуст! Нечего создавать.", _ColoredDebug);
            return;
        }

        foreach (HeroDataSO heroData in availableHeroes)
        {
            if (heroData == null) continue;

            GameObject cardInstance = Instantiate(_heroCardPrefab, _heroCardsContainer);
            HeroCardUI cardUI = cardInstance.GetComponent<HeroCardUI>();

            if (cardUI != null)
            {
                bool isLocked = heroData.RequiredPlayerLevelToUnlock > 0;
                cardUI.Initialize(heroData, isLocked, SelectHero);
                _spawnedCards.Add(cardUI);
            }
        }

        ResetSelection();
    }

    private void ResetSelection()
    {
        if (_selectedCard != null)
        {
            _selectedCard.SetChosen(false);
            _selectedCard = null;
        }

        GameInstance.Instance.SetSelectedHeroData(null);

        UpdateNextButtonState(false);

        // Показываем информацию о первом доступном герое, но не выбираем его
        var firstHeroToShow = _heroLibrary.AvailableHeroes.FirstOrDefault(h => h.RequiredPlayerLevelToUnlock <= 0)
                           ?? _heroLibrary.AvailableHeroes.FirstOrDefault();

        if (firstHeroToShow != null)
        {
            UpdateHeroInfoPanel(firstHeroToShow);
        }
    }

    private void UpdateHeroInfoPanel(HeroDataSO heroData)
    {
        if (heroData == null) return;

        if (_selectedHeroNameText != null) _selectedHeroNameText.text = heroData.HeroName;
        if (_selectedHeroDescriptionText != null) _selectedHeroDescriptionText.text = heroData.Description;

        if (_selectedHeroImage != null && heroData.HeroSprite != null)
        {
            _selectedHeroImage.sprite = heroData.HeroSprite;

            _selectedHeroImage.transform.DOKill();
            _selectedHeroImage.DOKill();

            _selectedHeroImage.transform.localScale = Vector3.one * 0.8f;
            Color imgColor = _selectedHeroImage.color;
            imgColor.a = 0;
            _selectedHeroImage.color = imgColor;

            _selectedHeroImage.DOFade(1, 0.3f);
            _selectedHeroImage.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }
    #endregion Личные методы
}

