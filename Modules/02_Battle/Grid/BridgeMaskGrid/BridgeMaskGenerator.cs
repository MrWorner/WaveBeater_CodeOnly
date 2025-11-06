// НАЗНАЧЕНИЕ: Генерирует процедурную спрайтовую маску для моста, повторяя его форму и размеры. Используется для визуальных эффектов.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BridgeMask (контейнер для спрайтов маски).
// ПРИМЕЧАНИЕ: Логика расчета позиций элементов полностью синхронизирована с BridgeGenerator для идеального совпадения.
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BridgeMaskGenerator : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeMask _bridgeMask;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private GameObject _maskElementPrefab;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS")]
    [BoxGroup("SETTINGS/Dimensions"), SerializeField] private int _width = 10;
    [BoxGroup("SETTINGS/Dimensions"), SerializeField] private int _height = 3;
    [BoxGroup("SETTINGS/Appearance"), Tooltip("Размер элемента по X и Y"), SerializeField] private Vector2 _elementSize = new Vector2(2f, 1f);
    [BoxGroup("SETTINGS/Appearance"), Tooltip("Горизонтальное смещение для каждой следующей строки по вертикали"), SerializeField] private float _slantFactor = 1f;
    [BoxGroup("SETTINGS/Appearance"), Tooltip("Цвет, который будет применен ко всем элементам маски"), SerializeField] private Color _maskColor = Color.white;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion

    #region Свойства
    /// <summary>
    /// Ширина генерируемой маски в ячейках.
    /// </summary>
    public int Width { get => _width; }

    /// <summary>
    /// Высота генерируемой маски в ячейках.
    /// </summary>
    public int Height { get => _height; }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_bridgeMask == null) DebugUtils.LogMissingReference(this, nameof(_bridgeMask));
        if (_maskElementPrefab == null) DebugUtils.LogMissingReference(this, nameof(_maskElementPrefab));
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Устанавливает новые размеры для маски и запускает ее полную перегенерацию.
    /// </summary>
    /// <param name="width">Новая ширина маски в ячейках.</param>
    /// <param name="height">Новая высота маски в ячейках.</param>
    /// <param name="cellsToSkip">Список координат клеток, которые не нужно создавать.</param>
    public void GenerateNewMask(int width, int height, List<Vector2Int> cellsToSkip)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeMaskGenerator:</color> Запрос на генерацию новой маски с размерами W:<color=yellow>{0}</color>, H:<color=yellow>{1}</color>.", _ColoredDebug, width, height);
        _width = width;
        _height = height;
        GenerateMask(cellsToSkip);
    }
    #endregion

    #region Личные методы
    /// <summary>
    /// Запускает основной процесс генерации или перегенерации спрайтовой маски на основе текущих настроек.
    /// </summary>
    /// <param name="cellsToSkip">Список координат клеток, которые не нужно создавать.</param>
    [Button(ButtonSizes.Large)]
    private void GenerateMask(List<Vector2Int> cellsToSkip = null)
    {
        ColoredDebug.CLog(gameObject, "<color=lime>BridgeMaskGenerator:</color> Начало генерации маски. Ширина: <color=yellow>{0}</color>, Высота: <color=yellow>{1}</color>.", _ColoredDebug, _width, _height);
        if (_bridgeMask == null)
        {
            Debug.LogError("Критическая ошибка: Ссылка на BridgeMask не установлена! Генерация невозможна.");
            return;
        }
        if (_maskElementPrefab == null)
        {
            Debug.LogError("Критическая ошибка: Префаб элемента маски не установлен! Генерация невозможна.");
            return;
        }

        _bridgeMask.ClearMask();

        List<SpriteRenderer> allElements = new List<SpriteRenderer>();
        float maskWorldWidth = (_width - 1) * _elementSize.x + (_height - 1) * _slantFactor;
        float maskWorldHeight = (_height - 1) * _elementSize.y;
        Vector3 originOffset = new Vector3(0, maskWorldHeight / 2f, 0);

        for (int x = 0; x < _width; x++)
        {
            GameObject columnParent = new GameObject($"Column_{x}");
            columnParent.transform.SetParent(_bridgeMask.transform, false);

            for (int y = 0; y < _height; y++)
            {
                if (cellsToSkip != null && cellsToSkip.Contains(new Vector2Int(x, y)))
                {
                    continue;
                }

                float worldX = x * _elementSize.x + y * _slantFactor;
                float worldY = -y * _elementSize.y;
                Vector3 elementWorldPos = _bridgeMask.transform.position + new Vector3(worldX, worldY, 0) + originOffset;

                GameObject newElementGO;
#if UNITY_EDITOR
                newElementGO = PrefabUtility.InstantiatePrefab(_maskElementPrefab, columnParent.transform) as GameObject;
#else
                newElementGO = Instantiate(_maskElementPrefab, columnParent.transform);
#endif
                newElementGO.name = $"MaskElement_{x}_{y}";
                newElementGO.transform.position = elementWorldPos;

                /*
                SpriteRenderer sr = newElementGO.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = _maskColor;
                    sr.sortingOrder = y - x;
                    allElements.Add(sr);
                }
                else
                {
                    ColoredDebug.CLog(gameObject, $"<color=orange>BridgeMaskGenerator:</color> Префаб '{_maskElementPrefab.name}' не содержит компонента SpriteRenderer!", _ColoredDebug);
                }
                */
            }
        }

        //_bridgeMask.SetElements(allElements);

#if UNITY_EDITOR
        EditorUtility.SetDirty(_bridgeMask);
        EditorUtility.SetDirty(this);
#endif
        ColoredDebug.CLog(gameObject, "<color=green>BridgeMaskGenerator:</color> <color=white>Генерация маски успешно завершена.</color> Создано элементов: <color=yellow>{0}</color>", _ColoredDebug, allElements.Count);
    }
    #endregion
}