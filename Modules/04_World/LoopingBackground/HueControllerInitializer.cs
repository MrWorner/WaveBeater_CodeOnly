using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// Утилита для автоматического добавления компонента LayerHueController 
/// ко всем объектам-слоям (имеющим LoopingBackground) и установки ссылок на дочерние SpriteRenderers.
/// </summary>
public class HueControllerInitializer : MonoBehaviour
{

    #region Поля: Required

    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private Material _targetMaterial;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<LayerHueController> _initializedControllers = new List<LayerHueController>();
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug = false;//Должен быть в каждом классе
    #endregion Поля

    #region Методы UNITY
    private void Awake()
    {
        if (_targetMaterial == null) DebugUtils.LogMissingReference(this, nameof(_targetMaterial));///
    }
    #endregion Методы UNITY

    #region Публичные методы

    [BoxGroup("SETTINGS")]
    [Button("1. Инициализировать КОНТРОЛЛЕРЫ СЛОЕВ", ButtonSizes.Large)]
    public void InitializeLayerControllers()
    {
        ColoredDebug.CLog(gameObject, "<color=magenta>HueControllerInitializer:</color> Запуск инициализации контроллеров СЛОЕВ в иерархии.", _ColoredDebug);

        if (_targetMaterial == null)
        {
            ColoredDebug.CLog(gameObject, "<color=red>HueControllerInitializer:</color> Инициализация ПРОВАЛЕНА! Свойство _targetMaterial не задано.", _ColoredDebug);
            return;
        }

        _initializedControllers.Clear();
        int addedCount = 0;

        // Находим все компоненты, которые по вашей структуре являются слоями (Layer SKY, Layer Siluette и т.д.)
        // Предполагаем, что все слои имеют компонент LoopingBackground.
        MonoBehaviour[] layerCandidates = GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour candidate in layerCandidates)
        {
            // Проверяем, что это объект-слой, который нас интересует
            if (candidate.GetType().Name == "LoopingBackground")
            {
                // Получаем родительский GameObject слоя
                GameObject layerObject = candidate.gameObject;

                // Находим все SpriteRenderer в детях этого слоя
                SpriteRenderer[] childRenderers = layerObject.GetComponentsInChildren<SpriteRenderer>(false); // Ищем только в непосредственных детях (false)

                if (childRenderers.Length == 0) continue; // Пропускаем слои без спрайтов

                // 1. Устанавливаем целевой материал для всех рендереров
                foreach (SpriteRenderer renderer in childRenderers)
                {
                    renderer.sharedMaterial = _targetMaterial;
                }

                // 2. Добавляем LayerHueController на сам объект-слой
                LayerHueController controller = layerObject.GetComponent<LayerHueController>();
                if (controller == null)
                {
                    controller = layerObject.AddComponent<LayerHueController>();
                    addedCount++;
                }

                // 3. Собираем и устанавливаем ссылки на рендереры в контроллере
                List<SpriteRenderer> renderersList = new List<SpriteRenderer>(childRenderers);

                System.Reflection.FieldInfo renderersField = typeof(LayerHueController).GetField("_controlledRenderers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (renderersField != null)
                {
                    renderersField.SetValue(controller, renderersList);
                }

                // 4. Устанавливаем значение _ColoredDebug
                System.Reflection.FieldInfo debugField = typeof(LayerHueController).GetField("_ColoredDebug",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (debugField != null)
                {
                    debugField.SetValue(controller, _ColoredDebug);
                }

                _initializedControllers.Add(controller);
            }
        }

        ColoredDebug.CLog(gameObject, "<color=magenta>HueControllerInitializer:</color> Успешно добавлено/обновлено <color=lime>{0}</color> контроллеров слоев.", _ColoredDebug, addedCount);
        ColoredDebug.CLog(gameObject, "<color=lime>HueControllerInitializer:</color> Инициализация завершена. Теперь вы можете настраивать LayerHueController на слоях.", _ColoredDebug);
    }

    [BoxGroup("SETTINGS")]
    [Button("2. Удалить ВСЕ LayerHueControllers", ButtonSizes.Medium)]
    public void RemoveAllLayerControllers()
    {
        ColoredDebug.CLog(gameObject, "<color=red>HueControllerInitializer:</color> Запуск удаления контроллеров слоев.", _ColoredDebug);

        LayerHueController[] allControllers = GetComponentsInChildren<LayerHueController>(true);

        int removedCount = 0;
        foreach (LayerHueController controller in allControllers)
        {
            DestroyImmediate(controller);
            removedCount++;
        }

        _initializedControllers.Clear();
        ColoredDebug.CLog(gameObject, "<color=red>HueControllerInitializer:</color> Успешно удалено <color=red>{0}</color> контроллеров.", _ColoredDebug, removedCount);
    }

    #endregion Публичные методы
}