using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

/// <summary>
/// Контроллер для плавного изменения Hue Shift, Saturation и Brightness 
/// материала спрайта в рантайме с использованием DOTween.
/// </summary>
public class BackgroundHueController : MonoBehaviour
{

    #region Поля: Required

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private SpriteRenderer _backgroundRenderer;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _animationDuration = 3.0f;///Длительность анимации (в секундах)
    [BoxGroup("SETTINGS"), SerializeField] private Ease _easeType = Ease.InOutSine;///Тип анимации DOTween
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentHueShift;///Текущее значение Hue Shift
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentSaturation;///Текущее значение Saturation
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentBrightness;///Текущее значение Brightness
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug = false;//Должен быть в каждом классе
    #endregion Поля

    #region Свойства

    /// <summary>
    /// Возвращает текущий компонент SpriteRenderer фона.
    /// </summary>
    public SpriteRenderer BackgroundRenderer { get => _backgroundRenderer; }

    /// <summary>
    /// Возвращает текущее значение Hue Shift (0-360).
    /// </summary>
    public float CurrentHueShift { get => _currentHueShift; }

    /// <summary>
    /// Возвращает текущее значение Saturation (0-2).
    /// </summary>
    public float CurrentSaturation { get => _currentSaturation; }

    /// <summary>
    /// Возвращает текущее значение Brightness (0-2).
    /// </summary>
    public float CurrentBrightness { get => _currentBrightness; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        // Проверка обязательной ссылки (Required)
        if (_backgroundRenderer == null) DebugUtils.LogMissingReference(this, nameof(_backgroundRenderer));///

        // Клонируем материал, чтобы изменять только его, а не общий ассет материала.
        if (_backgroundRenderer != null)
        {
            // Создание нового материала для предотвращения изменения ассета
            _backgroundRenderer.material = new Material(_backgroundRenderer.material);

            // Считывание начальных значений
            Material mat = _backgroundRenderer.material;
            _currentHueShift = mat.GetFloat("_HsvShift");
            _currentSaturation = mat.GetFloat("_HsvSaturation");
            _currentBrightness = mat.GetFloat("_HsvBright");
        }
    }

    private void Start()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>BackgroundHueController:</color> Инициализация HSV. Hue: <color=yellow>{0}</color>, Sat: <color=yellow>{1}</color>, Brg: <color=yellow>{2}</color>.",
            _ColoredDebug, _currentHueShift, _currentSaturation, _currentBrightness);
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Запускает плавное изменение Hue Shift (0-360).
    /// </summary>
    public void SetHueShift(float newHue)
    {
        // Диапазон: 0-360 
        float clampedHue = Mathf.Clamp(newHue, 0f, 360f);

        DOTween.Kill(this, "_HsvShift"); // Убиваем твин по ID свойства

        _backgroundRenderer.material.DOFloat(clampedHue, "_HsvShift", _animationDuration)
            .SetOptions(true) // Циклическая интерполяция для угла
            .SetEase(_easeType)
            .SetId("_HsvShift") // Задаем ID твина, чтобы можно было его убить
            .SetTarget(this)
            .OnUpdate(() =>
            {
                _currentHueShift = _backgroundRenderer.material.GetFloat("_HsvShift");
            });

        ColoredDebug.CLog(gameObject, "<color=cyan>BackgroundHueController:</color> Запуск DOTween для Hue Shift: <color=yellow>{0}</color> -> <color=yellow>{1}</color> за {2} с.",
            _ColoredDebug, _currentHueShift, clampedHue, _animationDuration);
    }

    /// <summary>
    /// Запускает плавное изменение Saturation (0-2).
    /// </summary>
    public void SetSaturation(float newSaturation)
    {
        // Диапазон: 0-2 
        float clampedSat = Mathf.Clamp(newSaturation, 0f, 2f);

        DOTween.Kill(this, "_HsvSaturation");

        _backgroundRenderer.material.DOFloat(clampedSat, "_HsvSaturation", _animationDuration)
            .SetEase(_easeType)
            .SetId("_HsvSaturation")
            .SetTarget(this)
            .OnUpdate(() =>
            {
                _currentSaturation = _backgroundRenderer.material.GetFloat("_HsvSaturation");
            });

        ColoredDebug.CLog(gameObject, "<color=cyan>BackgroundHueController:</color> Запуск DOTween для Saturation: <color=yellow>{0}</color> -> <color=yellow>{1}</color> за {2} с.",
            _ColoredDebug, _currentSaturation, clampedSat, _animationDuration);
    }

    /// <summary>
    /// Запускает плавное изменение Brightness (0-2).
    /// </summary>
    public void SetBrightness(float newBrightness)
    {
        // Диапазон: 0-2 
        float clampedBright = Mathf.Clamp(newBrightness, 0f, 2f);

        DOTween.Kill(this, "_HsvBright");

        _backgroundRenderer.material.DOFloat(clampedBright, "_HsvBright", _animationDuration)
            .SetEase(_easeType)
            .SetId("_HsvBright")
            .SetTarget(this)
            .OnUpdate(() =>
            {
                _currentBrightness = _backgroundRenderer.material.GetFloat("_HsvBright");
            });

        ColoredDebug.CLog(gameObject, "<color=cyan>BackgroundHueController:</color> Запуск DOTween для Brightness: <color=yellow>{0}</color> -> <color=yellow>{1}</color> за {2} с.",
            _ColoredDebug, _currentBrightness, clampedBright, _animationDuration);
    }

    /// <summary>
    /// Устанавливает все три HSV-параметра одновременно.
    /// </summary>
    public void SetAllHSV(float hue, float saturation, float brightness)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>BackgroundHueController:</color> Запуск DOTween для всех HSV-параметров.", _ColoredDebug);
        SetHueShift(hue);
        SetSaturation(saturation);
        SetBrightness(brightness);
    }
    #endregion Публичные методы


    #region Личные методы

    // --- Пресеты для тестирования ---

    [BoxGroup("DEBUG")]
    [Button("Preset: DAY (Normal)")]
    private void PresetDay()
    {
        SetAllHSV(0f, 1f, 1f); // 0° Hue, Sat 1, Bright 1
    }

    [BoxGroup("DEBUG")]
    [Button("Preset: SUNSET (Warm)")]
    private void PresetSunset()
    {
        SetAllHSV(30f, 1.2f, 1.0f); // 30° (теплый оранжевый/красный с небольшой насыщенностью)
    }

    [BoxGroup("DEBUG")]
    [Button("Preset: NIGHT (Blue/Dark)")]
    private void PresetNight()
    {
        SetAllHSV(240f, 0.9f, 0.4f); // 240° (синий/фиолетовый) + темнее + немного меньше насыщенности
    }

    [BoxGroup("DEBUG")]
    [Button("Preset: INVERTED (180°)")]
    private void PresetInverted()
    {
        SetAllHSV(180f, 1f, 1f); // 180° (полная инверсия цвета)
    }
    #endregion Личные методы
}