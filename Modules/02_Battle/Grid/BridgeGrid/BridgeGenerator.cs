// НАЗНАЧЕНИЕ: Отвечает за процедурную генерацию визуальной сетки моста из префабов-элементов.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BridgeGrid (контейнер для ячеек), BridgeCellElement (компонент ячейки).
// ПРИМЕЧАНИЕ: Вся логика генерации запускается через публичный метод GenerateNewBridge. Может пропускать создание клеток для создания "обрывов".
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BridgeGenerator : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeGrid _bridge;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, AssetsOnly, Tooltip("Префаб для стандартной разрушаемой клетки.")]
    private GameObject _standardElementPrefab;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, AssetsOnly, Tooltip("Префаб для НЕразрушаемой клетки.")]
    private GameObject _indestructibleElementPrefab;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, AssetsOnly, Tooltip("Префаб для стеклянной клетки (разрушается сразу).")]
    private GameObject _glassElementPrefab;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS")]
    [BoxGroup("SETTINGS/Dimensions"), SerializeField] private int _width = 10;
    [BoxGroup("SETTINGS/Dimensions"), SerializeField] private int _height = 3;
    [BoxGroup("SETTINGS/Appearance"), Tooltip("Размер элемента по X и Y"), SerializeField] private Vector2 _elementSize = new Vector2(2f, 1f);
    [BoxGroup("SETTINGS/Appearance"), Tooltip("Горизонтальное смещение для каждой следующей строки по вертикали"), SerializeField] private float _slantFactor = 1f;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion

    #region Свойства
    /// <summary>
    /// Ширина генерируемого моста в ячейках.
    /// </summary>
    public int Width { get => _width; }

    /// <summary>
    /// Высота генерируемого моста в ячейках.
    /// </summary>
    public int Height { get => _height; }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_bridge == null) DebugUtils.LogMissingReference(this, nameof(_bridge));
        if (_standardElementPrefab == null) DebugUtils.LogMissingReference(this, nameof(_standardElementPrefab));
        if (_indestructibleElementPrefab == null) DebugUtils.LogMissingReference(this, nameof(_indestructibleElementPrefab));
        if (_glassElementPrefab == null) DebugUtils.LogMissingReference(this, nameof(_glassElementPrefab));
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Устанавливает новые размеры для моста и запускает его полную перегенерацию, учитывая типы клеток.
    /// </summary>
    /// <param name="width">Новая ширина моста в ячейках.</param>
    /// <param name="height">Новая высота моста в ячейках.</param>
    /// <param name="cellsToSkip">Список координат клеток, которые не нужно создавать.</param>
    /// <param name="cellTypeMap">Словарь с указанием НЕСТАНДАРТНЫХ типов клеток (Indestructible, Glass). Клетки типа Standard не указываются.</param>
    public void GenerateNewBridge(int width, int height, List<Vector2Int> cellsToSkip, Dictionary<Vector2Int, BattleCell.CellType> cellTypeMap)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeGenerator:</color> Запрос на генерацию нового моста с размерами W:<color=yellow>{0}</color>, H:<color=yellow>{1}</color>.", _ColoredDebug, width, height);
        _width = width;
        _height = height;
        Generate(cellsToSkip, cellTypeMap);
    }

    /// <summary>
    /// Возвращает префаб элемента моста для указанного типа клетки.
    /// </summary>
    /// <param name="type">Тип клетки.</param>
    /// <returns>Префаб GameObject.</returns>
    public GameObject GetPrefabForType(BattleCell.CellType type)
    {
        switch (type)
        {
            case BattleCell.CellType.Indestructible:
                return _indestructibleElementPrefab;
            case BattleCell.CellType.Glass:
                return _glassElementPrefab;
            case BattleCell.CellType.Standard:
            default:
                return _standardElementPrefab;
        }
    }

    /// <summary>
    /// Рассчитывает мировую позицию для элемента моста по его координатам.
    /// </summary>
    /// <param name="pos">Координаты X, Y.</param>
    /// <returns>Мировая позиция.</returns>
    public Vector3 CalculateWorldPosition(Vector2Int pos)
    {
        float maskWorldWidth = (_width - 1) * _elementSize.x + (_height - 1) * _slantFactor;
        float maskWorldHeight = (_height - 1) * _elementSize.y;
        Vector3 originOffset = new Vector3(0, maskWorldHeight / 2f, 0);
        float worldX = pos.x * _elementSize.x + pos.y * _slantFactor;
        float worldY = -pos.y * _elementSize.y;
        return _bridge.transform.position + new Vector3(worldX, worldY, 0) + originOffset;
    }
    #endregion

    #region Личные методы
    /// <summary>
    /// Запускает основной процесс генерации или перегенерации сетки моста на основе текущих настроек и типов клеток.
    /// </summary>
    /// <param name="cellsToSkip">Список координат клеток, которые не нужно создавать.</param>
    /// <param name="cellTypeMap">Карта с нестандартными типами клеток.</param>
    [Button(ButtonSizes.Large)]
    private void Generate(List<Vector2Int> cellsToSkip = null, Dictionary<Vector2Int, BattleCell.CellType> cellTypeMap = null)
    {
        ColoredDebug.CLog(gameObject, "<color=lime>BridgeGenerator:</color> Начало генерации моста. Ширина: <color=yellow>{0}</color>, Высота: <color=yellow>{1}</color>.", _ColoredDebug, _width, _height);
        if (_bridge == null)
        {
            Debug.LogError("Критическая ошибка: Ссылка на BridgeGrid не установлена! Генерация невозможна.");
            return;
        }

        _bridge.ClearElements();

        List<BridgeCellElement> allElements = new List<BridgeCellElement>();
        for (int x = 0; x < _width; x++)
        {
            GameObject columnParent = new GameObject($"Column_{x}");
            columnParent.transform.SetParent(_bridge.transform, false);

            for (int y = 0; y < _height; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (cellsToSkip != null && cellsToSkip.Contains(currentPos))
                {
                    continue;
                }

                BattleCell.CellType currentType = BattleCell.CellType.Standard;
                if (cellTypeMap != null && cellTypeMap.TryGetValue(currentPos, out BattleCell.CellType specificType))
                {
                    currentType = specificType;
                }

                GameObject prefabToInstantiate = GetPrefabForType(currentType);
                Vector3 elementWorldPos = CalculateWorldPosition(currentPos);

                GameObject newElementGO;
#if UNITY_EDITOR
                newElementGO = PrefabUtility.InstantiatePrefab(prefabToInstantiate, columnParent.transform) as GameObject;
#else
                newElementGO = Instantiate(prefabToInstantiate, columnParent.transform);
#endif
                newElementGO.name = $"Element_{x}_{y}";
                newElementGO.transform.position = elementWorldPos;

                BridgeCellElement cellElement = newElementGO.GetComponent<BridgeCellElement>();
                if (cellElement == null)
                {
                    cellElement = newElementGO.AddComponent<BridgeCellElement>();
                    Debug.LogError($"На префабе '{prefabToInstantiate.name}' отсутствует BridgeCellElement. Компонент был добавлен автоматически.", prefabToInstantiate);
                }
                cellElement.Init(new Vector2Int(x, y));
                allElements.Add(cellElement);
            }
        }

        _bridge.SetElements(allElements);
#if UNITY_EDITOR
        EditorUtility.SetDirty(_bridge);
        EditorUtility.SetDirty(this);
#endif
        ColoredDebug.CLog(gameObject, "<color=green>BridgeGenerator:</color> <color=white>Генерация успешно завершена.</color> Создано элементов: <color=yellow>{0}</color>", _ColoredDebug, allElements.Count);
    }
    #endregion
}