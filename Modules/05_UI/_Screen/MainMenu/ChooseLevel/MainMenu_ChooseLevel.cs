using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu_ChooseLevel : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private LevelProgressionLibrary _levelProgressionLibrary;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Transform _levelCardsContainer;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _levelCardUIPrefab;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _backButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _nextButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _nextButtonAnticlicker;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<LevelCardUI> _spawnedCards = new List<LevelCardUI>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private LevelProgression _selectedProgression;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private LevelCardUI _selectedCardUI;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isStarted;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_levelProgressionLibrary == null) DebugUtils.LogMissingReference(this, nameof(_levelProgressionLibrary));
        if (_levelCardsContainer == null) DebugUtils.LogMissingReference(this, nameof(_levelCardsContainer));
        if (_levelCardUIPrefab == null) DebugUtils.LogMissingReference(this, nameof(_levelCardUIPrefab));
        if (_backButton == null) DebugUtils.LogMissingReference(this, nameof(_backButton));
        if (_nextButton == null) DebugUtils.LogMissingReference(this, nameof(_nextButton));
        if (_nextButtonAnticlicker == null) DebugUtils.LogMissingReference(this, nameof(_nextButtonAnticlicker));

        _backButton.onClick.AddListener(OnBackButtonPressed);
        _nextButton.onClick.AddListener(OnNextButtonPressed);
    }

    private void Start()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>MainMenu_ChooseLevel:</color> Запускаю инициализацию выбора набора уровней.", _ColoredDebug);
        PopulateLevelSelection();
        UpdateNextButtonState();
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void SelectLevelProgression(LevelProgression selectedProgression)
    {
        if (selectedProgression == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>MainMenu_ChooseLevel:</color> Попытка выбрать несуществующий набор уровней. Операция отменена.", _ColoredDebug);
            return;
        }

        if (_selectedCardUI != null)
        {
            _selectedCardUI.SetChosen(false);
        }

        LevelCardUI cardUI = _spawnedCards.FirstOrDefault(c => c.LevelProgression == selectedProgression);
        if (cardUI != null && cardUI.IsLocked)
        {
            ColoredDebug.CLog(gameObject, "<color=red>MainMenu_ChooseLevel:</color> Попытка выбрать заблокированный набор уровней. Операция отменена.", _ColoredDebug);
            return;
        }

        _selectedProgression = selectedProgression;
        _selectedCardUI = cardUI;

        if (_selectedCardUI != null)
        {
            SoundManager.Instance.PlayOneShot(SoundType.ButtonClick);
            _selectedCardUI.SetChosen(true);
        }

        ColoredDebug.CLog(gameObject, $"<color=cyan>MainMenu_ChooseLevel:</color> Выбран набор уровней: <color=lime>{_selectedProgression.name}</color>.", _ColoredDebug);

        GameInstance.Instance.SetSelectedLevelProgression(_selectedProgression);
        ColoredDebug.CLog(gameObject, $"<color=lime>MainMenu_ChooseLevel:</color> Данные о наборе <color=yellow>{_selectedProgression.name}</color> сохранены в <color=orange>GameInstance</color>.", _ColoredDebug);

        UpdateNextButtonState();
    }
    #endregion Публичные методы

    #region Личные методы
    private void OnBackButtonPressed()
    {
        SoundManager.Instance.PlayOneShot(SoundType.ButtonClick);
        MainMenu.Instance.GoToHeroSelection();
        ResetSelection(); // Сбрасываем выбор при возвращении
    }

    private void OnNextButtonPressed()
    {
        if (_selectedProgression != null)
        {
            SoundManager.Instance.PlayOneShot(SoundType.ButtonClickAlternative1);
            NewGame();
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=yellow>MainMenu_ChooseLevel:</color> Не выбран набор уровней для продолжения.", _ColoredDebug);
        }
    }

    private void UpdateNextButtonState()
    {
        bool isSelected = _selectedProgression != null;
        _nextButton.interactable = isSelected;
        _nextButtonAnticlicker.SetActive(!isSelected);
    }

    private void PopulateLevelSelection()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>MainMenu_ChooseLevel:</color> Заполняю UI карточками наборов уровней...", _ColoredDebug);
        foreach (Transform child in _levelCardsContainer)
        {
            Destroy(child.gameObject);
        }
        _spawnedCards.Clear();

        var availableProgressions = _levelProgressionLibrary.AvailableProgressions;
        if (availableProgressions == null || availableProgressions.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=red>MainMenu_ChooseLevel:</color> Список доступных наборов уровней пуст! Нечего создавать.", _ColoredDebug);
            return;
        }

        foreach (LevelProgression progression in availableProgressions)
        {
            if (progression == null) continue;

            GameObject cardInstance = Instantiate(_levelCardUIPrefab, _levelCardsContainer);
            LevelCardUI cardUI = cardInstance.GetComponent<LevelCardUI>();

            if (cardUI != null)
            {
                bool isLocked = progression.IsLocked;
                cardUI.Initialize(progression, isLocked);
                // Привязываем слушатель к кнопке, передавая ссылку на текущий объект и выбранный прогресс
                cardUI.SetButtonListener(() => SelectLevelProgression(progression));
                _spawnedCards.Add(cardUI);
            }
        }

        ResetSelection();
    }

    private void ResetSelection()
    {
        if (_selectedCardUI != null)
        {
            _selectedCardUI.SetChosen(false);
        }

        GameInstance.Instance.SetSelectedLevelProgression(null);

        _selectedProgression = null;
        _selectedCardUI = null;
        UpdateNextButtonState();
    }

    private void NewGame()
    {
        if (_isStarted)
        {
            return;
        }

        MusicManager.Instance.StopMusic();
        SoundManager.Instance.PlayOneShot(SoundType.ButtonClick);
        _isStarted = true;
        ///StopAllCoroutines();
        ///StartCoroutine(StartGameRoutine());
        SceneLoader.Instance.LoadNextScene(GameScene.GameScene);
    }

    #endregion Личные методы
}
