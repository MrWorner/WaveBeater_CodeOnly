using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class UpgradeCardUI : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), SerializeField, ReadOnly] private UpgradeCardDataSO _cardData;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _textTitle;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _textBonus;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _textCost;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _iconImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _coinIconImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _borderImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _backgroundImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _buttonGetBonus;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentCost;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector3 _originalPosition;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public int CurrentCost { get => _currentCost; }
    public UpgradeCardDataSO CardData { get => _cardData; }
    public Button ButtonGetBonus { get => _buttonGetBonus; }
    public Vector3 OriginalPosition { get => _originalPosition; }
    public TextMeshProUGUI TextTitle { get => _textTitle; }
    public Image IconImage { get => _iconImage; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_cardData == null) DebugUtils.LogMissingReference(this, nameof(_cardData));
        if (_textTitle == null) DebugUtils.LogMissingReference(this, nameof(_textTitle));
        if (_textBonus == null) DebugUtils.LogMissingReference(this, nameof(_textBonus));
        if (_textCost == null) DebugUtils.LogMissingReference(this, nameof(_textCost));
        if (_iconImage == null) DebugUtils.LogMissingReference(this, nameof(_iconImage));
        if (_coinIconImage == null) DebugUtils.LogMissingReference(this, nameof(_coinIconImage)); // ДОБАВЛЕНО: Проверка на null
        if (_borderImage == null) DebugUtils.LogMissingReference(this, nameof(_borderImage));
        if (_backgroundImage == null) DebugUtils.LogMissingReference(this, nameof(_backgroundImage));
        if (_buttonGetBonus == null) DebugUtils.LogMissingReference(this, nameof(_buttonGetBonus));

        _originalPosition = transform.localPosition;
    }

    private void Start()
    {
        _buttonGetBonus.onClick.AddListener(() => UpgradeShopController.Instance.OnCardSelected(this));
        UpdateIndicators();
    }

    #endregion Методы UNITY

    #region Публичные методы
    public void UpdateIndicators()
    {
        if (_currentCost == 0)
        {
            _textCost.text = "FREE";
            _textCost.color = Color.white;
            _coinIconImage.gameObject.SetActive(false); // Скрываем иконку монеты
        }
        else
        {
            _coinIconImage.gameObject.SetActive(true); // Показываем иконку монеты
            _textCost.text = _currentCost.ToString();

            if (CurrencyManager.Instance.Currency >= _currentCost)
            {
                _textCost.color = Color.white;
            }
            else
            {
                _textCost.color = Color.red;
            }
        }
    }

    public void IncreaseCost()
    {
        _currentCost += _cardData.CostIncrease;
        UpdateIndicators();
    }

    public void Initialize(UpgradeCardDataSO data, int currentCost)
    {
        _cardData = data;
        _currentCost = currentCost; // Сохраняем переданную стоимость
        SetupFromScriptableObject();
    }
    #endregion Публичные методы

    #region Личные методы
    private void SetupFromScriptableObject()
    {
        if (_cardData == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>UpgradeCardUI:</color> ScriptableObject <color=yellow>_cardData</color> не назначен! Карта не может быть инициализирована.", _ColoredDebug);
            gameObject.SetActive(false);
            return;
        }

        _textTitle.text = _cardData.Title;
        _iconImage.sprite = _cardData.Icon;
        _borderImage.color = _cardData.BorderColor;
        _backgroundImage.color = _cardData.BackgroundColor;

        // Форматирование текста бонуса в зависимости от типа
        string bonusPrefix = "+";
        string bonusSymbolEnding = ""; // Например, "%" если нужно

        if (_cardData.BonusType == UpgradeCardDataSO.CardTypeBonus.Heal || _cardData.BonusType == UpgradeCardDataSO.CardTypeBonus.Backlash || _cardData.BonusType == UpgradeCardDataSO.CardTypeBonus.CriticalHit)
        {
            bonusSymbolEnding = "%";
        }

        _textBonus.text = bonusPrefix + _cardData.BonusValue + bonusSymbolEnding;

        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeCardUI:</color> Карта <color=yellow>{0}</color> успешно инициализирована.", _ColoredDebug, _cardData.Title);
    }
    #endregion Личные методы
}