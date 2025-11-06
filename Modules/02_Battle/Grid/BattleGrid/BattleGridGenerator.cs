// НАЗНАЧЕНИЕ: Отвечает за процедурную генерацию логической сетки боя (массива BattleCell), учитывая типы клеток.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: BattleCell, BattleGrid, ArenaManager (для получения типов клеток).
// ПРИМЕЧАНИЕ: Создает экземпляры BattleCell и передает им их тип.
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BattleGridGenerator : MonoBehaviour
{
    private static BattleGridGenerator _instance;
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleCell _battleCellPrefab;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private BattleGrid _battleGrid;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField, ReadOnly] private int _width = 4; 
    [BoxGroup("SETTINGS"), SerializeField, ReadOnly] private int _height = 4;
    [BoxGroup("SETTINGS"), SerializeField] private Vector2 _cellSize = new Vector2(2f, 1f);
    [BoxGroup("SETTINGS"), Tooltip("Горизонтальное смещение для каждой следующей строки по вертикали"), SerializeField] private float _slantFactor = 1f;
    [BoxGroup("SETTINGS"), ColorUsage(true, true), SerializeField] private Color _startColor = Color.red;
    [BoxGroup("SETTINGS"), ColorUsage(true, true), SerializeField] private Color _midColor = Color.yellow;
    [BoxGroup("SETTINGS"), ColorUsage(true, true), SerializeField] private Color _endColor = Color.blue;
    [BoxGroup("SETTINGS"), Range(0f, 1f), SerializeField] private float _cellAlpha = 1f;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion

    #region Свойства
    public static BattleGridGenerator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BattleGridGenerator>();
            }
            return _instance;
        }
    }
    public int Height => _height;
    public int Width => _width;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null)
        {
            DebugUtils.LogInstanceAlreadyExists(this, _instance);
        }
        else _instance = this;

        if (_battleCellPrefab == null) DebugUtils.LogMissingReference(this, nameof(_battleCellPrefab));
        if (_battleGrid == null) DebugUtils.LogMissingReference(this, nameof(_battleGrid));
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridGenerator:</color> Awake.", _ColoredDebug);
    }
    #endregion

    #region Публичные методы
    /// <summary>
    /// Устанавливает новые размеры для сетки и запускает ее полную перегенерацию, учитывая типы клеток.
    /// </summary>
    /// <param name="width">Новая ширина сетки.</param>
    /// <param name="height">Новая высота сетки.</param>
    /// <param name="cellsToSkip">Список координат клеток, которые не нужно создавать.</param>
    /// <param name="cellTypeMap">Словарь с указанием НЕСТАНДАРТНЫХ типов клеток.</param>
    public void GenerateNewGrid(int width, int height, List<Vector2Int> cellsToSkip, Dictionary<Vector2Int, BattleCell.CellType> cellTypeMap)
    {
        _width = width;
        _height = height;
        GenerateField(cellsToSkip, cellTypeMap); // Передаем карту типов
    }

    /// <summary>
    /// Запускает основной процесс генерации или перегенерации сетки (используется кнопкой в инспекторе).
    /// Генерирует сетку только со стандартными клетками.
    /// </summary>
    [Button(ButtonSizes.Large)]
    public void GenerateField()
    {
        GenerateField(null, null);
    }

    /// <summary>
    /// Рассчитывает градиентный цвет для ячейки по ее координатам.
    /// </summary>
    /// <param name="pos">Координаты ячейки.</param>
    /// <returns>Рассчитанный цвет.</returns>
    public Color GetColorForCell(Vector2Int pos)
    {
        float t = (_width > 1) ? (float)pos.x / (_width - 1) : 0f;
        Color lineColor = (t <= 0.5f) ?
            Color.Lerp(_startColor, _midColor, t * 2) : Color.Lerp(_midColor, _endColor, (t - 0.5f) * 2);
        lineColor.a = _cellAlpha;

        // Этот лог будет слишком частым, если его раскомментировать
        // ColoredDebug.CLog(gameObject, "<color=grey>BattleGridGenerator:</color> Расчет цвета для {0} (t={1:F2}). Результат: {2}.", _ColoredDebug, pos, t, lineColor);

        return lineColor;
    }
    #endregion

    #region Личные методы
    /// <summary>
    /// Запускает основной процесс генерации или перегенерации логической сетки.
    /// </summary>
    /// <param name="cellsToSkip">Список координат клеток, которые не нужно создавать.</param>
    /// <param name="cellTypeMap">Карта с нестандартными типами клеток (может быть null).</param>
    private void GenerateField(List<Vector2Int> cellsToSkip = null, Dictionary<Vector2Int, BattleCell.CellType> cellTypeMap = null)
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridGenerator:</color> Начало генерации поля. Размеры: <color=yellow>{0}x{1}</color>.", _ColoredDebug, _width, _height);
        _battleGrid.ClearGrid();
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridGenerator:</color> Данные сетки очищены.", _ColoredDebug);

        List<BattleCell> allCells = new List<BattleCell>();
        float gridWorldWidth = (_width - 1) * _cellSize.x + (_height - 1) * _slantFactor;
        float gridWorldHeight = (_height - 1) * _cellSize.y;
        Vector3 originOffset = new Vector3(0, gridWorldHeight / 2f, 0);
        for (int x = 0; x < _width; x++)
        {
            GameObject columnParent = new GameObject($"Column_{x}");
            columnParent.transform.SetParent(_battleGrid.transform, false);

            for (int y = 0; y < _height; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                bool isAbyss = cellsToSkip != null && cellsToSkip.Contains(currentPos);

                float worldX = x * _cellSize.x + y * _slantFactor;
                float worldY = -y * _cellSize.y;
                Vector3 cellWorldPos = _battleGrid.transform.position + new Vector3(worldX, worldY, 0) + originOffset;

                BattleCell newCell;
#if UNITY_EDITOR
                newCell = (BattleCell)PrefabUtility.InstantiatePrefab(_battleCellPrefab, columnParent.transform);
#else
                newCell = Instantiate(_battleCellPrefab, columnParent.transform);
#endif

                newCell.name = $"Cell_{x}_{y}";
                BattleCell.CellType currentType = BattleCell.CellType.Standard;
                if (!isAbyss && cellTypeMap != null && cellTypeMap.TryGetValue(currentPos, out BattleCell.CellType specificType))
                {
                    currentType = specificType;
                }

                newCell.Init(currentPos, cellWorldPos, currentType);

                if (isAbyss)
                {
                    newCell.Deactivate();
                }
                else
                {
                    Color lineColor = GetColorForCell(currentPos);
                    newCell.SetColor(lineColor);
                }

                allCells.Add(newCell);
            }
        }

        _battleGrid.SetCells(allCells, _width, _height);
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridGenerator:</color> Все ячейки (<color=yellow>{0}</color>) установлены в BattleGrid.", _ColoredDebug, allCells.Count);
        if (Application.isPlaying && BattleGridAnimator.Instance != null)
        {
            BattleGridAnimator.Instance.HideGridInstantly();
            ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridGenerator:</color> Сетка мгновенно скрыта аниматором.", _ColoredDebug);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(_battleGrid);
        EditorUtility.SetDirty(this);
#endif
        ColoredDebug.CLog(gameObject, "<color=green>BattleGridGenerator:</color> <color=white>Генерация поля успешно завершена.</color>", _ColoredDebug);
    }

    public void IncreaseWidth(int add)
    {
        _width += add;
        ColoredDebug.CLog(gameObject, "<color=cyan>BattleGridGenerator:</color> Ширина увеличена на <color=yellow>{0}</color>. Новая ширина: <color=lime>{1}</color>.", _ColoredDebug, add, _width);
    }
    #endregion
}