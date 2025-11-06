// ================== НАЧАЛО ФАЙЛА: BattleGrid.cs ================== //
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;

public class BattleGrid : MonoBehaviour
{
    private static BattleGrid _instance;
    #region Поля
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private BattleCell[,] _grid;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _width;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _height;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private List<BattleCell> _allCells = new List<BattleCell>();
    #endregion

    #region Свойства
    public static BattleGrid Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BattleGrid>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(BattleGrid).Name);
                    _instance = singletonObject.AddComponent<BattleGrid>();
                }
            }
            return _instance;
        }
    }

    public List<BattleCell> AllCells => _allCells;
    public BattleCell[,] Grid => _grid;
    public int Width => _width;
    public int Height => _height;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DebugUtils.LogInstanceAlreadyExists(this, _instance);
            Destroy(gameObject);
            return;
        }
        _instance = this;
        ColoredDebug.CLog(gameObject, "<color=lime>BattleGrid:</color> Singleton инициализирован.", _ColoredDebug);
        if (_grid == null || _grid.Length == 0)
        {
            RestoreGridFromChildren();
        }
    }
    #endregion

    #region Публичные методы
    public void SetCells(List<BattleCell> cells, int width, int height)
    {
        _allCells = cells;
        _grid = new BattleCell[width, height];
        _width = width;
        _height = height;
        foreach (var cell in cells)
        {
            if (cell != null)
                _grid[cell.Position.x, cell.Position.y] = cell;
        }
        ColoredDebug.CLog(gameObject, "<color=lime>BattleGrid:</color> Сетка установлена с размерами <color=yellow>{0}x{1}</color>. Всего ячеек: <color=yellow>{2}</color>.", _ColoredDebug, width, height, cells.Count);
    }

    /// <summary>
    /// Деактивирует клетку по указанным координатам.
    /// </summary>
    /// <param name="position">Координаты клетки для деактивации.</param>
    public void DeactivateCell(Vector2Int position)
    {
        if (position.x >= 0 && position.x < _width && position.y >= 0 && position.y < _height)
        {
            BattleCell cell = _grid[position.x, position.y];
            if (cell != null)
            {
                cell.Deactivate();
            }
        }
    }

    /// <summary>
    /// Получает клетку по координатам
    /// </summary>
    /// <param name="position">Координаты клетки</param>
    /// <returns>BattleCell или null, если клетка не существует или за пределами сетки</returns>
    public BattleCell GetCell(Vector2Int position)
    {
        if (position.x < 0 || position.x >= _width || position.y < 0 || position.y >= _height)
            return null;

        return _grid[position.x, position.y];
    }

    public void ClearGrid()
    {
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
        _allCells.Clear();
        _grid = null;
        ColoredDebug.CLog(gameObject, "<color=lime>BattleGrid:</color> Сетка очищена.", _ColoredDebug);
    }

    /// <summary>
    /// Находит и возвращает ближайшую к указанной мировой позиции ячейку.
    /// </summary>
    /// <returns>Найденная BattleCell или null.</returns>
    public BattleCell GetCellFromWorldPosition(Vector3 worldPos)
    {
        BattleCell closestCell = null;
        float minDistance = float.MaxValue;

        foreach (var cell in _allCells)
        {
            if (cell == null) continue;
            float distance = Vector3.Distance(worldPos, cell.WorldPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCell = cell;
            }
        }

        if (closestCell != null)
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleGrid:</color> Найдена ближайшая ячейка <color=yellow>{0}</color> к позиции <color=yellow>{1}</color>.", _ColoredDebug, closestCell.name, worldPos);
        return closestCell;
    }

    /// <summary>
    /// Сбрасывает состояние всех ячеек в сетке до исходного.
    /// </summary>
    public void ResetAllCells()
    {
        if (_allCells == null) return;
        foreach (var cell in _allCells)
        {
            if (cell != null)
            {
                cell.ResetState();
            }
        }
        ColoredDebug.CLog(gameObject, "<color=orange>BattleGrid:</color> Все <color=yellow>{0}</color> ячеек были сброшены в исходное состояние.", _ColoredDebug, _allCells.Count);
    }

    [Button(ButtonSizes.Medium)]
    public void ShowVisualization()
    {
        if (_grid == null)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleGrid:</color> Невозможно отобразить визуализацию, сетка пуста.", true);
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Grid Visualization:");
        for (int y = _grid.GetLength(1) - 1; y >= 0; y--)
        {
            for (int x = 0; x < _grid.GetLength(0); x++)
            {
                var cell = _grid[x, y];
                if (cell == null || cell.IsDeactivated) { sb.Append(" . "); continue; }
                sb.Append(cell.IsOccupied() ? " 1 " : " 0 ");
            }
            sb.AppendLine();
        }
        ColoredDebug.CLog(gameObject, sb.ToString(), true);
    }
    #endregion

    #region Личные методы
    private void RestoreGridFromChildren()
    {
        _allCells.Clear();
        var found = GetComponentsInChildren<BattleCell>(true);

        foreach (var cell in found)
        {
            _allCells.Add(cell);
        }

        if (_allCells.Count > 0)
        {
            int maxX = 0, maxY = 0;
            foreach (var c in _allCells)
            {
                if (c.Position.x > maxX) maxX = c.Position.x;
                if (c.Position.y > maxY) maxY = c.Position.y;
            }

            _width = maxX + 1;
            _height = maxY + 1;
            _grid = new BattleCell[_width, _height];
            foreach (var c in _allCells)
            {
                _grid[c.Position.x, c.Position.y] = c;
            }
            ColoredDebug.CLog(gameObject, "<color=lime>BattleGrid:</color> Сетка восстановлена из дочерних объектов. Размеры: <color=yellow>{0}x{1}</color>.", _ColoredDebug, _width, _height);
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=orange>BattleGrid:</color> Не найдены дочерние ячейки для восстановления сетки.", _ColoredDebug);
        }
    }
    #endregion
}