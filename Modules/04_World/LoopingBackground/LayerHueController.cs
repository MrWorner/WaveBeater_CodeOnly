using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Настройки Hue/Saturation/Brightness/Contrast для одного пресета (День/Вечер/Ночь).
/// </summary>
[System.Serializable]
public struct HSVSettings
{
    [Range(0f, 360f)]
    public float HueShift;
    [Range(0f, 2f)]
    public float Saturation;
    [Range(0f, 6f)]
    public float Brightness;

    public static HSVSettings Default => new HSVSettings { HueShift = 0f, Saturation = 1f, Brightness = 1f };
}

/// <summary>
/// Контроллер для плавного (или мгновенного) изменения Hue Shift, Saturation, Brightness и Contrast 
/// на ВСЕХ SpriteRenderer в дочерних объектах (например, на всем слое).
/// Анимация автоматически отключается в Edit Mode.
/// </summary>
public class LayerHueController : MonoBehaviour
{
    #region Поля: Required

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private List<SpriteRenderer> _controlledRenderers = new List<SpriteRenderer>();
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _animationDuration = 3.0f;///Длительность анимации (в секундах)
    [BoxGroup("SETTINGS"), SerializeField] private Ease _easeType = Ease.InOutSine;///Тип анимации DOTween

    [BoxGroup("SETTINGS"), LabelText("Day"), SerializeField] private HSVSettings _daySettings = HSVSettings.Default;
    [BoxGroup("SETTINGS"), LabelText("Evening"), SerializeField] private HSVSettings _eveningSettings = HSVSettings.Default;
    [BoxGroup("SETTINGS"), LabelText("Night"), SerializeField] private HSVSettings _nightSettings = HSVSettings.Default;

    [BoxGroup("DEBUG"), OnValueChanged("BroadcastPreviewChange"), EnumToggleButtons, SerializeField] private BackgroundVariant _editorPreview;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentHueShift;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentSaturation;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private float _currentBrightness;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug = false;

    private static bool _isBroadcastingPreview = false;
    #endregion Поля

    #region Свойства

    public List<SpriteRenderer> ControlledRenderers { get => _controlledRenderers; }
    public float CurrentHueShift { get => _currentHueShift; }
    public float CurrentSaturation { get => _currentSaturation; }
    public float CurrentBrightness { get => _currentBrightness; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (_controlledRenderers == null || _controlledRenderers.Count == 0)
            {
                DebugUtils.LogMissingReference(this, nameof(_controlledRenderers));
                return;
            }

            Material mat = null;
            for (int i = 0; i < _controlledRenderers.Count; i++)
            {
                SpriteRenderer r = _controlledRenderers[i];
                if (r != null)
                {
                    r.material = new Material(r.sharedMaterial);
                    mat = r.material;
                }
            }

            if (mat != null)
            {
                _currentHueShift = mat.GetFloat("_HsvShift");
                _currentSaturation = mat.GetFloat("_HsvSaturation");
                _currentBrightness = mat.GetFloat("_HsvBright");
            }
        }

        if (Application.isPlaying)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnBackgroundVariantChanged += ApplyVariant;
                ColoredDebug.CLog(gameObject, "<color=cyan>LayerHueController:</color> Успешно подписан на событие <color=yellow>OnBackgroundVariantChanged</color>.", _ColoredDebug);
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=red>LayerHueController:</color> Не удалось найти StageManager.ActiveInstance для подписки на событие.", _ColoredDebug);
            }
        }
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && StageManager.Instance != null)
        {
            StageManager.Instance.OnBackgroundVariantChanged -= ApplyVariant;
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && _controlledRenderers != null && _controlledRenderers.Count > 0)
        {
            ApplyVariant(_editorPreview);
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    [Button]
    public void ApplyVariant(BackgroundVariant variant)
    {
        HSVSettings settings;
        string variantName;

        float duration = Application.isPlaying ? _animationDuration : 0f;

        switch (variant)
        {
            case BackgroundVariant.Day:
                settings = _daySettings;
                variantName = "Day";
                break;
            case BackgroundVariant.Evening:
                settings = _eveningSettings;
                variantName = "Evening";
                break;
            case BackgroundVariant.Night:
                settings = _nightSettings;
                variantName = "Night";
                break;
            default:
                ColoredDebug.CLog(gameObject, "<color=red>LayerHueController:</color> Неизвестный вариант BackgroundVariant: <color=yellow>{0}</color>.", _ColoredDebug, variant.ToString());
                return;
        }

        ApplySettings(settings, duration, variantName);
    }

    public void ResetToDefault()
    {
        float duration = Application.isPlaying ? _animationDuration : 0f;
        ApplySettings(HSVSettings.Default, duration, "Default");
    }

    #endregion Публичные методы


    #region Личные методы
    private void UpdateCurrentValues(Material mat)
    {
        _currentHueShift = mat.GetFloat("_HsvShift");
        _currentSaturation = mat.GetFloat("_HsvSaturation");
        _currentBrightness = mat.GetFloat("_HsvBright");
    }

    private void ApplySettings(HSVSettings settings, float duration, string settingsName)
    {
        ///ColoredDebug.CLog(gameObject, "<color=cyan>LayerHueController:</color> Применение настроек <color=lime>{0}</color>. Длительность: <color=yellow>{1}</color> с. Режим: <color=yellow>{2}</color>.", _ColoredDebug, settingsName, duration, Application.isPlaying ? "RUNTIME" : "EDITOR");

        if (_controlledRenderers == null || _controlledRenderers.Count == 0) return;

        foreach (SpriteRenderer renderer in _controlledRenderers)
        {
            if (renderer != null)
            {
                Material mat = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
                if (mat == null) continue;

                DOTween.Kill(mat);

                if (duration > 0f)
                {
                    // Получаем текущее значение Hue
                    float currentHue = mat.GetFloat("_HsvShift");
                    float targetHue = settings.HueShift;

                    // Вычисляем разницу
                    float diff = targetHue - currentHue;

                    // Если идти напрямую дольше, чем через 0/360, корректируем цель
                    if (Mathf.Abs(diff) > 180)
                    {
                        if (diff > 0)
                        {
                            // Если цель больше (например, 20 -> 340), идем назад
                            targetHue -= 360f;
                        }
                        else
                        {
                            // Если цель меньше (например, 340 -> 20), идем вперед
                            targetHue += 360f;
                        }
                    }

                    // Анимируем Hue к скорректированному значению
                    mat.DOFloat(targetHue, "_HsvShift", duration)
                       .SetOptions(true)
                       .SetEase(_easeType)
                       .SetTarget(mat)
                       .OnComplete(() => {
                           // После анимации вернем значение в стандартный диапазон 0-360
                           // Это не обязательно, но полезно для отладки
                           float finalHue = mat.GetFloat("_HsvShift") % 360f;
                           if (finalHue < 0) finalHue += 360f;
                           mat.SetFloat("_HsvShift", finalHue);
                       });

                    // Анимация Saturation и Brightness остается без изменений
                    mat.DOFloat(settings.Saturation, "_HsvSaturation", duration).SetEase(_easeType).SetTarget(mat);
                    mat.DOFloat(settings.Brightness, "_HsvBright", duration).SetEase(_easeType).SetTarget(mat).OnUpdate(() =>
                    {
                        if (renderer == _controlledRenderers[0]) UpdateCurrentValues(mat);
                    });
                }
                else
                {
                    // Для мгновенного применения ничего менять не нужно
                    mat.SetFloat("_HsvShift", settings.HueShift);
                    mat.SetFloat("_HsvSaturation", settings.Saturation);
                    mat.SetFloat("_HsvBright", settings.Brightness);

                    if (renderer == _controlledRenderers[0]) UpdateCurrentValues(mat);
                }
            }
        }
    }

    private void BroadcastPreviewChange()
    {
        if (_isBroadcastingPreview) return;

        try
        {
            _isBroadcastingPreview = true;

            if (transform.parent == null)
            {
                ColoredDebug.CLog(gameObject, "<color=red>LayerHueController:</color> Нет родителя для распространения превью.", _ColoredDebug);
                ApplyVariant(_editorPreview);
                return;
            }

            LayerHueController[] allControllers = transform.parent.GetComponentsInChildren<LayerHueController>(false);
            ColoredDebug.CLog(gameObject, "<color=magenta>LayerHueController:</color> Распространение превью <color=lime>{0}</color> на <color=yellow>{1}</color> слоев.",
                _ColoredDebug, _editorPreview.ToString(), allControllers.Length);

            foreach (LayerHueController otherController in allControllers)
            {
                otherController._editorPreview = this._editorPreview;
                otherController.ApplyVariant(this._editorPreview);
            }
        }
        finally
        {
            _isBroadcastingPreview = false;
        }
    }

    [BoxGroup("DEBUG"), Button("Copy & Apply DAY Settings to All Siblings", ButtonSizes.Medium)]
    private void CopyDaySettingsToSiblings()
    {
        CopyAndApplySettingsToSiblings(_daySettings, BackgroundVariant.Day);
    }

    [BoxGroup("DEBUG"), Button("Copy & Apply EVENING Settings to All Siblings", ButtonSizes.Medium)]
    private void CopyEveningSettingsToSiblings()
    {
        CopyAndApplySettingsToSiblings(_eveningSettings, BackgroundVariant.Evening);
    }

    [BoxGroup("DEBUG"), Button("Copy & Apply NIGHT Settings to All Siblings", ButtonSizes.Medium)]
    private void CopyNightSettingsToSiblings()
    {
        CopyAndApplySettingsToSiblings(_nightSettings, BackgroundVariant.Night);
    }

    private void CopyAndApplySettingsToSiblings(HSVSettings settingsToCopy, BackgroundVariant variant)
    {
        if (transform.parent == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>LayerHueController:</color> Невозможно скопировать. У объекта нет родителя.", _ColoredDebug);
            return;
        }

        LayerHueController[] allControllers = transform.parent.GetComponentsInChildren<LayerHueController>(false);
        int updateCount = 0;

        ColoredDebug.CLog(gameObject, "<color=magenta>LayerHueController:</color> Копирование и применение настроек <color=lime>{0}</color> на сиблингов (слоев: <color=yellow>{1}</color>).",
            _ColoredDebug, variant.ToString(), allControllers.Length);

        foreach (LayerHueController otherController in allControllers)
        {
            switch (variant)
            {
                case BackgroundVariant.Day:
                    otherController._daySettings = settingsToCopy;
                    break;
                case BackgroundVariant.Evening:
                    otherController._eveningSettings = settingsToCopy;
                    break;
                case BackgroundVariant.Night:
                    otherController._nightSettings = settingsToCopy;
                    break;
            }
            otherController.ApplyVariant(variant);
            updateCount++;
        }

        ColoredDebug.CLog(gameObject, "<color=magenta>LayerHueController:</color> Успешно скопированы и применены настройки на <color=lime>{0}</color> слоев.", _ColoredDebug, updateCount);
    }
    #endregion Личные методы
}