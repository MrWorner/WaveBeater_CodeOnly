// НАЗНАЧЕНИЕ: Управляет скоростью течения времени в игре через UI-кнопки, взаимодействуя с синглтоном Settings.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: Settings (для установки скорости), DOTween (для анимаций), TextMeshPro.
// ПРИМЕЧАНИЕ: Кнопка уменьшения скорости работает как немедленный сброс к x1. Добавлен режим "MAX" для пропуска анимаций.

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameSpeedController : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _increaseButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Button _decreaseButton;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _speedIndicatorText;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _animationDuration = 0.2f;
    [BoxGroup("SETTINGS"), SerializeField] private float _animationStrength = 0.2f;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _currentSpeedIndex = 0;
    // NOTE: Последний уровень скорости (индекс 5) - это специальный режим пропуска анимаций.
    private readonly float[] _speedLevels = { 1f, 2f, 4f, 8f, 16f };
    private const string SKIP_ANIMATION_TEXT = "M";
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        // Проверка ссылок
        if (_increaseButton == null) DebugUtils.LogMissingReference(this, nameof(_increaseButton));
        if (_decreaseButton == null) DebugUtils.LogMissingReference(this, nameof(_decreaseButton));
        if (_speedIndicatorText == null) DebugUtils.LogMissingReference(this, nameof(_speedIndicatorText));

        // Назначение слушателей кнопок
        _increaseButton.onClick.AddListener(IncreaseSpeed);
        _decreaseButton.onClick.AddListener(ResetSpeed);
    }

    private void Start()
    {
        // NOTE: Вызываем в Start, чтобы гарантировать, что Settings.Instance уже проинициализирован в своем Awake.
        UpdateUIAndGameSpeed();
        ColoredDebug.CLog(gameObject, "<color=cyan>GameSpeedController:</color> Инициализирован. Начальная скорость: <color=yellow>x{0}</color>.", _ColoredDebug, _speedLevels[0]);
    }

    private void OnDestroy()
    {
        // Отписка от слушателей кнопок
        if (_increaseButton != null)
        {
            _increaseButton.onClick.RemoveListener(IncreaseSpeed);
        }
        if (_decreaseButton != null)
        {
            _decreaseButton.onClick.RemoveListener(ResetSpeed);
        }
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Увеличивает скорость игры до следующего доступного уровня.
    /// </summary>
    public void IncreaseSpeed()
    {
        // _speedLevels.Length - это индекс для режима "MAX"
        if (_currentSpeedIndex < _speedLevels.Length) // Проверяем, что не достигли режима "MAX" (индекс 5)
        {
            _currentSpeedIndex++;
            ColoredDebug.CLog(gameObject, "<color=cyan>GameSpeedController:</color> Скорость увеличена до индекса <color=yellow>{0}</color>.", _ColoredDebug, _currentSpeedIndex);
            UpdateUIAndGameSpeed();
        }
        // Если _currentSpeedIndex равен _speedLevels.Length (5), кнопка увеличения неактивна, код сюда не дойдет
    }

    /// <summary>
    /// Мгновенно сбрасывает скорость игры к начальному значению (x1).
    /// </summary>
    public void ResetSpeed()
    {
        if (_currentSpeedIndex > 0)
        {
            _currentSpeedIndex = 0;
            ColoredDebug.CLog(gameObject, "<color=cyan>GameSpeedController:</color> Скорость <color=orange>сброшена</color> до начальной.", _ColoredDebug);
            UpdateUIAndGameSpeed();
        }
    }
    #endregion

    #region Личные методы
    /// <summary>
    /// Обновляет UI, состояние кнопок и применяет выбранную скорость в глобальных настройках.
    /// </summary>
    private void UpdateUIAndGameSpeed()
    {
        bool isSkipMode = _currentSpeedIndex >= _speedLevels.Length;
        if (Settings.Instance == null)
        {
            // Используем Debug.LogError для критических ошибок
            Debug.LogError("Критическая ошибка: Settings.Instance не найден! Управление скоростью невозможно.");
            return;
        }

        if (isSkipMode)
        {
            // Включаем режим пропуска анимаций
            Settings.Instance.SetSkipAnimation(true);
            _speedIndicatorText.text = SKIP_ANIMATION_TEXT;
            ColoredDebug.CLog(gameObject, "<color=cyan>GameSpeedController:</color> <color=orange>Режим пропуска анимаций активирован.</color>", _ColoredDebug);
        }
        else
        {
            // Включаем обычный режим и устанавливаем множитель скорости
            float currentSpeed = _speedLevels[_currentSpeedIndex];
            Settings.Instance.SetSkipAnimation(false);
            Settings.Instance.SetSpeedMultiplier(currentSpeed);
            _speedIndicatorText.text = $"x{currentSpeed}";
            ColoredDebug.CLog(gameObject, "<color=cyan>GameSpeedController:</color> Установлена скорость <color=yellow>x{0}</color>.", _ColoredDebug, currentSpeed);
        }

        // Анимация текста для обратной связи
        _speedIndicatorText.transform.DOKill();
        _speedIndicatorText.transform.DOPunchScale(Vector3.one * _animationStrength, _animationDuration, 1, 0.5f);

        // Обновляем интерактивность кнопок
        _decreaseButton.interactable = (_currentSpeedIndex > 0);
        _increaseButton.interactable = (_currentSpeedIndex < _speedLevels.Length); // Доступна до последнего уровня + режим "MAX"
    }
    #endregion
}