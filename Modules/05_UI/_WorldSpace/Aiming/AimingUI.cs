// НАЗНАЧЕНИЕ: Управляет UI-элементом, который отображает процесс прицеливания или перезарядки юнита, показывая оставшееся количество ходов.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: DOTween для анимаций.
// ПРИМЕЧАНИЕ: Компонент спроектирован так, чтобы быть пассивным. Его видимость полностью контролируется извне (например, из BattleUnitUI).
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class AimingUI : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private CanvasGroup _canvasGroup;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Image _iconImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextMeshProUGUI _turnsText;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _fadeDuration = 0.3f;
    [BoxGroup("SETTINGS"), SerializeField] private Vector3 _punchScale = new Vector3(0.2f, 0.2f, 0.2f);
    [BoxGroup("SETTINGS"), SerializeField] private float _punchDuration = 0.2f;
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0;
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Показывает UI с начальным количеством ходов.
    /// </summary>
    /// <param name="turns">Количество ходов для отображения.</param>
    public void Show(int turns)
    {
        UpdateTurnsText(turns);
        gameObject.SetActive(true); // Включаем объект

        _canvasGroup.DOKill(); // Убиваем предыдущие анимации на случай быстрого повторного вызова
        _canvasGroup.alpha = 0;
        _canvasGroup.DOFade(1f, _fadeDuration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Обновляет количество оставшихся ходов с анимацией.
    /// </summary>
    /// <param name="turnsLeft">Оставшееся количество ходов.</param>
    public void UpdateTurns(int turnsLeft)
    {
        UpdateTurnsText(turnsLeft);
        transform.DOPunchScale(_punchScale, _punchDuration, 1, 0.5f);
    }

    /// <summary>
    /// Скрывает UI и деактивирует его после анимации.
    /// </summary>
    public void Hide()
    {
        // Если объект уже неактивен, ничего не делаем
        if (!gameObject.activeInHierarchy) return;

        _canvasGroup.DOKill();
        _canvasGroup.DOFade(0f, _fadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (this != null && gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            });
    }
    #endregion Публичные методы

    #region Личные методы
    private void UpdateTurnsText(int value)
    {
        if (_turnsText != null)
        {
            _turnsText.text = value.ToString();
        }
    }
    #endregion Личные методы
}