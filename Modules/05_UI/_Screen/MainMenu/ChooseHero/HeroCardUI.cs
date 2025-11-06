using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class HeroCardUI : MonoBehaviour
{
    public UnityAction<HeroDataSO> OnHeroCardClicked;

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _heroNameLabel;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _heroDescritpionLabel;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _heroHP;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _heroDamage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _heroLevel;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _heroImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _lockedOverlay;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private GameObject _isChosenOverlay;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _selectButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private StatCubesUI _heroProgressCubesUI;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private HeroDataSO _heroData;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isLocked;
    #endregion Поля

    #region Свойства
    public HeroDataSO HeroData { get => _heroData; }
    public bool IsLocked { get => _isLocked; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_heroNameLabel == null) DebugUtils.LogMissingReference(this, nameof(_heroNameLabel));
        if (_heroDescritpionLabel == null) DebugUtils.LogMissingReference(this, nameof(_heroDescritpionLabel));
        if (_heroHP == null) DebugUtils.LogMissingReference(this, nameof(_heroHP));
        if (_heroDamage == null) DebugUtils.LogMissingReference(this, nameof(_heroDamage));
        if (_heroImage == null) DebugUtils.LogMissingReference(this, nameof(_heroImage));
        if (_heroLevel == null) DebugUtils.LogMissingReference(this, nameof(_heroLevel));
        if (_lockedOverlay == null) DebugUtils.LogMissingReference(this, nameof(_lockedOverlay));
        if (_selectButton == null) DebugUtils.LogMissingReference(this, nameof(_selectButton));
        if (_heroProgressCubesUI == null) DebugUtils.LogMissingReference(this, nameof(_heroProgressCubesUI));
        if (_isChosenOverlay == null) DebugUtils.LogMissingReference(this, nameof(_isChosenOverlay));

        _selectButton.onClick.AddListener(HandleClick);
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroCardUI:</color> Awake, Listener добавлен.", _ColoredDebug);
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void Initialize(HeroDataSO heroData, bool isLocked, UnityAction<HeroDataSO> clickAction)
    {
        _heroData = heroData;
        _isLocked = isLocked;
        OnHeroCardClicked = clickAction;

        //ColoredDebug.CLog(gameObject, "<color=cyan>HeroCardUI:</color> Инициализация карточки для <color=yellow>{0}</color>. Заблокировано: <color=orange>{1}</color>.", _ColoredDebug, _heroData.HeroName, _isLocked);

        _heroNameLabel.text = _heroData.HeroName;
        _heroDescritpionLabel.text = _heroData.Description;
        //_heroHP.text = _heroData.InitialHealth.ToString();
        //_heroDamage.text = _heroData.InitialAttackDamage.ToString();

        _heroLevel.text = "1";

        _heroProgressCubesUI.Initialize(10);
        _heroProgressCubesUI.SetCurrentValue(0);

        _heroImage.sprite = _heroData.HeroSprite;

        _selectButton.interactable = !_isLocked;
        _lockedOverlay.SetActive(_isLocked);
        if (_isLocked)
        {
            ///HideChildrenExcept();
        }
    }

    /// <summary>
    /// Shows or hides the "chosen" overlay on the card.
    /// </summary>
    /// <param name="isChosen">True to show, false to hide.</param>
    public void SetChosen(bool isChosen)
    {
        _isChosenOverlay.SetActive(isChosen);
    }
    #endregion Публичные методы

    #region Личные методы
    private void HandleClick()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroCardUI:</color> Карточка <color=yellow>{0}</color> нажата.", _ColoredDebug, _heroData.HeroName);
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f, 10, 1);
        OnHeroCardClicked?.Invoke(_heroData);
    }

    private void HideChildrenExcept()
    {
        // Create a list of children to keep active
        List<string> childrenToKeep = new List<string>
    {
        "HeroBabground Border",
        "HeroNameLabel",
        "HeroDescriptionLabel",
        "BUTTON",
        "Locked"
    };

        // Loop through all direct children of the CardBackground
        for (int i = 0; i < transform.GetChild(0).transform.childCount; i++)
        {
            Transform child = transform.GetChild(0).transform.GetChild(i);

            // If the child's name is not in the list of children to keep, set it to inactive
            if (!childrenToKeep.Contains(child.name))
            {
                child.gameObject.SetActive(false);
            }
        }
    }
    #endregion Личные методы
}
