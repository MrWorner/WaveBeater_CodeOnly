using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

public class StageProgressBar : MonoBehaviour
{
    [System.Serializable]
    public class MarkerConfig
    {
        public StageType Type;
        public GameObject Prefab;
        public Color CompletedColor = Color.yellow; // Цвет для пройденного этапа
    }

    private class StageMarker
    {
        public Image MarkerImage;
        public float NormalizedPosition;
        public MarkerConfig Config;
    }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Slider _progressBar;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private RectTransform _markerParent;
    [PropertyOrder(-1), BoxGroup("Required"), Required, SerializeField] private List<MarkerConfig> _markerConfigs;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<StageMarker> _markers = new List<StageMarker>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _stageCount;
    private static StageProgressBar _instance;
    #endregion Поля

    #region Свойства
    public static StageProgressBar Instance => _instance;
    public int StageCount => _stageCount;

    public List<MarkerConfig> MarkerConfigs { get => _markerConfigs; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance && _instance != this)
        {
            DebugUtils.LogInstanceAlreadyExists(this);
            Destroy(gameObject);
            return;
        }
        else _instance = this;

        if (_progressBar == null) DebugUtils.LogMissingReference(this, nameof(_progressBar));
        if (_markerParent == null) DebugUtils.LogMissingReference(this, nameof(_markerParent));
        if (_markerConfigs == null || _markerConfigs.Count == 0) Debug.LogError("Marker Configs не настроены в инспекторе!", this);
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void Init(List<StageType> stages)
    {
        // Чистим старые маркеры
        foreach (var marker in _markers)
            if (marker != null && marker.MarkerImage) Destroy(marker.MarkerImage.gameObject);
        _markers.Clear();

        _stageCount = stages.Count;
        if (_stageCount == 0) return;

        _progressBar.minValue = 0f;
        _progressBar.maxValue = 1f;
        _progressBar.value = 0f;

        for (int i = 0; i < _stageCount; i++)
        {
            float normalized = (float)(i + 1) / _stageCount;
            StageType currentStageType = stages[i];

            // 1. Находим нужный конфиг для текущего типа этапа
            MarkerConfig config = _markerConfigs.FirstOrDefault(c => c.Type == currentStageType);
            if (config == null || config.Prefab == null)
            {
                Debug.LogWarning($"Конфигурация для типа этапа {currentStageType} не найдена или префаб не назначен. Этап будет пропущен.", this);
                continue;
            }

            // Создаем маркер из префаба, соответствующего его типу
            GameObject markerObj = Instantiate(config.Prefab, _markerParent);
            markerObj.transform.SetSiblingIndex(0);
            RectTransform rect = markerObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(normalized, 0.5f);
            rect.anchorMax = new Vector2(normalized, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            Image img = markerObj.GetComponent<Image>();

            // Создаем и добавляем новый объект StageMarker в список
            _markers.Add(new StageMarker
            {
                MarkerImage = img,
                NormalizedPosition = normalized,
                Config = config
            });

            // Начальный цвет и спрайт теперь берутся из самого префаба,
            // поэтому старый switch-case для установки цвета удален.
        }

        // Обновляем состояние маркеров на случай, если прогресс не нулевой
        UpdateMarkersState(_progressBar.value);
    }

    public void SetProgress(float normalizedValue)
    {
        _progressBar.value = Mathf.Clamp01(normalizedValue);
        // 2. При каждом обновлении прогресса проверяем состояние маркеров
        UpdateMarkersState(_progressBar.value);
    }

    public float GetStageProgress(int stageIndex)
    {
        if (_stageCount <= 0) return 0f;
        return (float)(stageIndex + 1) / _stageCount;
    }
    #endregion Публичные методы

    #region Личные методы
    // Новый метод для обновления цвета маркеров
    private void UpdateMarkersState(float currentProgress)
    {
        foreach (var marker in _markers)
        {
            // Если прогресс достиг или прошел позицию маркера
            if (currentProgress >= marker.NormalizedPosition)
            {
                // Устанавливаем цвет "пройденного" этапа из его конфига
                marker.MarkerImage.color = marker.Config.CompletedColor;
            }
            // Здесь можно добавить 'else', если нужно возвращать цвет в исходное состояние,
            // но для линейного прогресса вперед это не требуется.
        }
    }
    #endregion Личные методы
}