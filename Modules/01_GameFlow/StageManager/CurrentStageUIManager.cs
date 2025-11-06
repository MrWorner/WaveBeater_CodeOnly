using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CurrentStageUIManager : MonoBehaviour
{
    [System.Serializable]
    public class StageUIConfig
    {
        public StageType Type;
        public Sprite Icon;
        public string DisplayName = "Этап";
        public Color BackgroundColor = Color.white;
    }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private List<StageUIConfig> _stageConfigs;
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly]
    private List<CurrentStageUI> _listeners = new List<CurrentStageUI>();
    [BoxGroup("DEBUG"), SerializeField]
    protected bool _ColoredDebug;
    #endregion

    #region Свойства
    public static CurrentStageUIManager Instance { get; private set; }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_stageConfigs == null) DebugUtils.LogMissingReference(this, nameof(_stageConfigs));
    }

    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged += HandleStageChange;
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged -= HandleStageChange;
        }
    }
    #endregion

    #region Публичные методы
    public void RegisterListener(CurrentStageUI listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    public void UnregisterListener(CurrentStageUI listener)
    {
        if (_listeners.Contains(listener))
        {
            _listeners.Remove(listener);
        }
    }
    #endregion

    #region Личные методы
    private void HandleStageChange(StageType newStageType)
    {
        StageUIConfig config = _stageConfigs.FirstOrDefault(c => c.Type == newStageType);
        if (config == null)
        {
            ColoredDebug.CLog(gameObject, $"Конфигурация для этапа '{newStageType}' не найдена!", _ColoredDebug);
            return;
        }

        foreach (var listener in _listeners)
        {
            listener.UpdateUI(config);
        }
    }
    #endregion

#if UNITY_EDITOR
    [Title("Инструменты Редактора")]
    [InfoBox("Нажмите, чтобы автоматически скопировать настройки иконок и цветов из StageProgressBar в этот менеджер.")]
    [Button("Синхронизировать из StageProgressBar", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 1f)]
    private void SyncFromProgressBar()
    {
        // 1. Найти StageProgressBar на сцене
        StageProgressBar progressBar = FindFirstObjectByType<StageProgressBar>();
        if (progressBar == null)
        {
            Debug.LogError("[CurrentStageUIManager] Не удалось найти объект StageProgressBar на сцене!");
            return;
        }

        // 2. Очистить текущий список
        _stageConfigs.Clear();

        // 3. Пройти по всем конфигам в прогресс баре
        foreach (var markerConfig in progressBar.MarkerConfigs)
        {
            if (markerConfig.Prefab == null)
            {
                Debug.LogWarning($"[CurrentStageUIManager] У маркера для типа '{markerConfig.Type}' не назначен префаб. Пропускаем.");
                continue;
            }

            Image prefabImage = markerConfig.Prefab.GetComponent<Image>();
            if (prefabImage == null)
            {
                Debug.LogWarning($"[CurrentStageUIManager] В префабе для типа '{markerConfig.Type}' не найден компонент Image. Пропускаем.");
                continue;
            }

            var prefabIcon = markerConfig.Prefab.GetComponentsInChildren<Image>();
            if (prefabImage == null)
            {
                Debug.LogWarning($"[CurrentStageUIManager] В префабе для типа '{markerConfig.Type}' не найден компонент prefabIcon. Пропускаем.");
                continue;
            }

            // 4. Создать новый конфиг для UI
            StageUIConfig newConfig = new StageUIConfig
            {
                Type = markerConfig.Type,
                Icon = prefabIcon[1].sprite,
                BackgroundColor = prefabImage.color,
                // Превращаем "BossFight" в "Boss Fight"
                DisplayName = System.Text.RegularExpressions.Regex.Replace(markerConfig.Type.ToString(), "(\\B[A-Z])", " $1")
            };

            _stageConfigs.Add(newConfig);
        }

        // 5. Пометить объект как измененный для сохранения
        EditorUtility.SetDirty(this);
        Debug.Log($"[CurrentStageUIManager] Синхронизация завершена. Загружено {_stageConfigs.Count} конфигураций из StageProgressBar.");
    }
#endif
}

