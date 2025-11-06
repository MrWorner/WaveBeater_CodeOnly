// НАЗНАЧЕНИЕ: Контейнер для хранения и управления физическими элементами моста (BridgeCellElement).
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BridgeCellElement, BridgeGenerator.
// ПРИМЕЧАНИЕ: Предоставляет координатный доступ к своим элементам для BattleCell. 
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BridgeGrid : MonoBehaviour
{
    public enum BridgeSet { NotSet, A, B }

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private BridgeGenerator _associatedGenerator;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, Tooltip("К какой группе принадлежит этот мост: A или B.")]
    private BridgeSet _bridgeSet = BridgeSet.NotSet;
    [BoxGroup("DEBUG"), SerializeField]
    private bool _ColoredDebug;

    [BoxGroup("DEBUG"), SerializeField, ReadOnly]
    private List<BridgeCellElement> _allElements = new List<BridgeCellElement>();
    private BridgeCellElement[,] _physicalCellGrid;
    #endregion

    #region Свойства
    public BridgeSet Set => _bridgeSet;
    public BridgeGenerator AssociatedGenerator => _associatedGenerator; // Свойство для доступа к генератору
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_associatedGenerator == null) DebugUtils.LogMissingReference(this, nameof(_associatedGenerator));

        if (_bridgeSet == BridgeSet.NotSet)
        {
            Debug.LogError($"[BridgeGrid] На объекте '{gameObject.name}' не установлен 'Bridge Set'!", gameObject);
        }

        if ((_physicalCellGrid == null || _physicalCellGrid.Length == 0) && _allElements.Count > 0)
        {
            RestoreGridFromList();
        }
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Устанавливает и организует в сетку список сгенерированных физических ячеек.
    /// </summary>
    /// <param name="elements">Список всех BridgeCellElement на мосту.</param>
    public void SetElements(List<BridgeCellElement> elements)
    {
        _allElements.Clear(); // Очищаем перед добавлением
        _allElements.AddRange(elements);
        if (_allElements.Count == 0) return;

        RestoreGridFromList(); // Используем общий метод для построения массива
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeGrid ({0}):</color> Физические элементы установлены.", _ColoredDebug, _bridgeSet);
    }

    /// <summary>
    /// Возвращает физический элемент моста по указанным координатам.
    /// </summary>
    /// <returns>BridgeCellElement или null, если элемент не найден.</returns>
    public BridgeCellElement GetPhysicalCell(Vector2Int position)
    {
        if (_physicalCellGrid == null && _allElements.Count > 0)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BridgeGrid ({0}):</color> Массив сетки был пуст. Запускаю принудительное восстановление...", _ColoredDebug, _bridgeSet);
            RestoreGridFromList();
        }

        if (_physicalCellGrid != null &&
            position.x >= 0 && position.x < _physicalCellGrid.GetLength(0) &&
            position.y >= 0 && position.y < _physicalCellGrid.GetLength(1))
        {
            return _physicalCellGrid[position.x, position.y];
        }

        ColoredDebug.CLog(gameObject, "<color=orange>BridgeGrid ({0}):</color> Запрос GetPhysicalCell({1}) вне границ или сетка пуста.", _ColoredDebug, _bridgeSet, position);
        return null;
    }

    /// <summary>
    /// Создает или заменяет физический элемент моста в указанной позиции.
    /// Используется для восстановления клеток (Repair/Build).
    /// </summary>
    public BridgeCellElement CreateOrReplacePhysicalCell(Vector2Int pos, BattleCell.CellType type)
    {
        if (_associatedGenerator == null)
        {
            Debug.LogError($"[BridgeGrid ({_bridgeSet})] AssociatedGenerator не назначен! Не могу создать/заменить клетку {pos}.");
            return null;
        }

        // 1. Удаляем существующий элемент (если он есть)
        BridgeCellElement existingElement = GetPhysicalCell(pos);
        Transform parentTransform = null;
        if (existingElement != null)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BridgeGrid ({0}):</color> Замена существующего элемента {1} на тип {2}.", _ColoredDebug, _bridgeSet, pos, type);
            parentTransform = existingElement.transform.parent;
            _allElements.Remove(existingElement);
            Destroy(existingElement.gameObject);
            if (_physicalCellGrid != null && pos.x < _physicalCellGrid.GetLength(0) && pos.y < _physicalCellGrid.GetLength(1))
            {
                _physicalCellGrid[pos.x, pos.y] = null;
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=lime>BridgeGrid ({0}):</color> Создание нового элемента {1} типа {2}.", _ColoredDebug, _bridgeSet, pos, type);
            parentTransform = transform.Find($"Column_{pos.x}");
            if (parentTransform == null)
            {
                GameObject columnParent = new GameObject($"Column_{pos.x}");
                columnParent.transform.SetParent(transform, false);
                parentTransform = columnParent.transform;
                ColoredDebug.CLog(gameObject, "<color=yellow>BridgeGrid ({0}):</color> Создана новая родительская колонка для {1}.", _ColoredDebug, _bridgeSet, pos.x);
            }
        }

        // 2. Получаем нужный префаб
        GameObject prefabToInstantiate = _associatedGenerator.GetPrefabForType(type);
        if (prefabToInstantiate == null)
        {
            Debug.LogError($"[BridgeGrid ({_bridgeSet})] Не найден префаб для типа {type}!");
            return null;
        }

        // 3. Создаем новый элемент
        Vector3 elementWorldPos = _associatedGenerator.CalculateWorldPosition(pos);
        GameObject newElementGO;
#if UNITY_EDITOR
        newElementGO = PrefabUtility.InstantiatePrefab(prefabToInstantiate, parentTransform) as GameObject;
#else
        newElementGO = Instantiate(prefabToInstantiate, parentTransform);
#endif
        if (newElementGO == null)
        {
            Debug.LogError($"[BridgeGrid ({_bridgeSet})] Не удалось инстанциировать префаб '{prefabToInstantiate.name}'!");
            return null;
        }

        newElementGO.name = $"Element_{pos.x}_{pos.y}";
        newElementGO.transform.position = elementWorldPos;

        BridgeCellElement newCellElement = newElementGO.GetComponent<BridgeCellElement>();
        if (newCellElement == null)
        {
            newCellElement = newElementGO.AddComponent<BridgeCellElement>();
            Debug.LogError($"На префабе '{prefabToInstantiate.name}' отсутствует BridgeCellElement. Компонент был добавлен автоматически.", prefabToInstantiate);
        }
        newCellElement.Init(pos);

        // 4. Добавляем в список и массив
        _allElements.Add(newCellElement);
        if (_physicalCellGrid != null && pos.x < _physicalCellGrid.GetLength(0) && pos.y < _physicalCellGrid.GetLength(1))
        {
            _physicalCellGrid[pos.x, pos.y] = newCellElement;
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BridgeGrid ({0}):</color> Массив _physicalCellGrid устарел после создания/замены клетки {1}. Пересоздание...", _ColoredDebug, _bridgeSet, pos);
            RestoreGridFromList();
        }

        return newCellElement;
    }

    /// <summary>
    /// Сбрасывает состояние всех физических ячеек на мосту.
    /// </summary>
    public void ResetAllPhysicalCells()
    {
        if (_allElements == null) return;
        foreach (var element in _allElements)
        {
            if (element != null) element.ResetState();
        }
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeGrid ({0}):</color> Все физические ячейки сброшены.", _ColoredDebug, _bridgeSet);
    }

    /// <summary>
    /// Очищает все дочерние объекты и удаляет ссылки на них из списка.
    /// </summary>
    public void ClearElements()
    {
        if (_allElements.Count == 0 && transform.childCount == 0) return;
#if UNITY_EDITOR
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
#else
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
#endif

        _allElements.Clear();
        _physicalCellGrid = null;
        ColoredDebug.CLog(gameObject, "<color=cyan>BridgeGrid ({0}):</color> Ячейка очищена.", _ColoredDebug, _bridgeSet);
    }
    #endregion

    #region Личные методы
    /// <summary>
    /// Восстанавливает 2D-массив _physicalCellGrid из сериализованного списка _allElements.
    /// </summary>
    private void RestoreGridFromList()
    {
        if (_allElements == null || _allElements.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BridgeGrid ({0}):</color> Список _allElements пуст, невозможно восстановить массив.", _ColoredDebug, _bridgeSet);
            _physicalCellGrid = null;
            return;
        }

        _allElements.RemoveAll(item => item == null);
        if (_allElements.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BridgeGrid ({0}):</color> Список _allElements оказался пуст после удаления null элементов.", _ColoredDebug, _bridgeSet);
            _physicalCellGrid = null;
            return;
        }

        int width = _allElements.Max(e => e.Position.x) + 1;
        int height = _allElements.Max(e => e.Position.y) + 1;
        _physicalCellGrid = new BridgeCellElement[width, height];
        foreach (var element in _allElements)
        {
            if (element.Position.x < width && element.Position.y < height && element.Position.x >= 0 && element.Position.y >= 0)
            {
                _physicalCellGrid[element.Position.x, element.Position.y] = element;
            }
            else
            {
                ColoredDebug.CLog(gameObject, "<color=red>BridgeGrid ({0}):</color> Элемент {1} имеет позицию {2}, выходящую за рассчитанные границы {3}x{4}! Пропущен.",
                    true, _bridgeSet, element.name, element.Position, width, height);
            }
        }

        ColoredDebug.CLog(gameObject, "<color=lime>BridgeGrid ({0}):</color> 2D-массив сетки УСПЕШНО ВОССТАНОВЛЕН/СОЗДАН из списка. Размеры: <color=yellow>{1}x{2}</color>.",
            _ColoredDebug, _bridgeSet, width, height);
    }
    #endregion
}