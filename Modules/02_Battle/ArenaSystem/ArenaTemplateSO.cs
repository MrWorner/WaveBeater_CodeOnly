// НАЗНАЧЕНИЕ: Хранит данные для одного шаблона боевой арены, включая размер, расположение пропов, состояние и тип клеток.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: PropSO, BattleCell, ArenaTextMapSO.
// ПРИМЕЧАНИЕ: Является ScriptableObject'ом. Создается и обновляется через ArenaTemplateProcessor.
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ArenaTemplate_New", menuName = "WaveBeater/Arena Template")]
public class ArenaTemplateSO : ScriptableObject
{
    [System.Serializable]
    public struct PropPlacementData
    {
        [AssetsOnly]
        public PropSO Prop;
        public Vector2Int Position;
    }

    [System.Serializable]
    public struct CellStateData
    {
        public Vector2Int Position;
        [EnumToggleButtons]
        public BattleCell.CellState State;
    }

    [System.Serializable]
    public struct CellTypeData
    {
        public Vector2Int Position;
        [EnumToggleButtons]
        public BattleCell.CellType Type;
    }

    #region Поля
    [BoxGroup("SETTINGS")]
    [BoxGroup("SETTINGS/Grid Size"), SerializeField, Range(4, 20), ReadOnly] private int _width = 15;
    [BoxGroup("SETTINGS/Grid Size"), SerializeField, Range(3, 8), ReadOnly] private int _height = 4;
    [BoxGroup("SETTINGS/Grid Content"), Tooltip("Список пропов и их точные координаты на сетке.")]
    [SerializeField, ReadOnly] private List<PropPlacementData> _propPlacements = new List<PropPlacementData>();
    [BoxGroup("SETTINGS/Grid Content"), Tooltip("Список клеток с предустановленным состоянием (треснувшие или дыры).")]
    [SerializeField, ReadOnly] private List<CellStateData> _cellStates = new List<CellStateData>();
    // --- НОВОЕ ПОЛЕ ---
    [BoxGroup("SETTINGS/Grid Content"), Tooltip("Список клеток с НЕСТАНДАРТНЫМ типом (Indestructible, Glass). Клетки типа Standard не хранятся.")]
    [SerializeField, ReadOnly] private List<CellTypeData> _cellTypes = new List<CellTypeData>();
    // ------------------
    [BoxGroup("SETTINGS/Grid Content"), Tooltip("Список координат клеток, которые будут отсутствовать на поле, создавая обрывы.")]
    [SerializeField, ReadOnly] private List<Vector2Int> _missingCells = new List<Vector2Int>();
    [BoxGroup("SETTINGS/Import & Export"), Tooltip("Карта сопоставления символов и пропов/типов для импорта/экспорта."), Required(InfoMessageType.Error)]
    [SerializeField] private ArenaTextMapSO _textMap;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    /// <summary> Ширина арены в клетках. </summary>
    public int Width => _width;
    /// <summary> Высота арены в клетках. </summary>
    public int Height => _height;
    /// <summary> Предоставляет доступ только для чтения к списку данных о размещении пропов. </summary>
    public IReadOnlyList<PropPlacementData> PropPlacements => _propPlacements;
    /// <summary> Предоставляет доступ только для чтения к списку данных о состоянии клеток. </summary>
    public IReadOnlyList<CellStateData> CellStates => _cellStates;
    /// <summary> Предоставляет доступ только для чтения к списку данных о типе клеток (только не-Standard). </summary>
    public IReadOnlyList<CellTypeData> CellTypes => _cellTypes;
    // --------------------
    /// <summary> Предоставляет доступ только для чтения к списку координат отсутствующих клеток. </summary>
    public IReadOnlyList<Vector2Int> MissingCells => _missingCells;
    #endregion Свойства

#if UNITY_EDITOR
    #region Редактор

    [Button("Экспорт в TXT", ButtonSizes.Large), GUIColor(1f, 0.9f, 0.6f)]
    [BoxGroup("SETTINGS/Import & Export")]
    private void ExportToTextFile()
    {
        if (_textMap == null)
        {
            Debug.LogError("[ArenaTemplateSO] Не назначен ArenaTextMapSO! Экспорт невозможен.");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Экспортировать шаблон арены", "", $"{this.name}.txt", "txt");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            // Используем Dictionary для удобства поиска типа/пропа/состояния по координатам
            var cellChars = new Dictionary<Vector2Int, char>();

            // 1. Заполняем символами по умолчанию (проходимая)
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    cellChars[new Vector2Int(x, y)] = '.'; // '.' по умолчанию
                }
            }

            // 2. Отмечаем отсутствующие клетки
            foreach (var pos in _missingCells) cellChars[pos] = ' ';

            // 3. Устанавливаем типы клеток (перезапишут '.' или ' ')
            foreach (var typeData in _cellTypes)
            {
                char? typeChar = _textMap.GetChar(typeData.Type);
                if (typeChar.HasValue) cellChars[typeData.Position] = typeChar.Value;
            }

            // 4. Устанавливаем состояния клеток (перезапишут тип или '.')
            foreach (var stateData in _cellStates)
            {
                switch (stateData.State)
                {
                    case BattleCell.CellState.Cracked: cellChars[stateData.Position] = '~'; break;
                    case BattleCell.CellState.Hole: cellChars[stateData.Position] = 'O'; break;
                }
            }

            // 5. Размещаем пропы (перезапишут всё остальное)
            foreach (var propData in _propPlacements)
            {
                cellChars[propData.Position] = _textMap.GetChar(propData.Prop);
            }


            var sb = new StringBuilder();
            sb.AppendLine(this.name.Replace("AT_", "").Replace("_", " "));

            // Итерируем с верхнего ряда (height - 1) вниз до 0
            for (int y = _height - 1; y >= 0; y--)
            {
                for (int x = 0; x < _width; x++)
                {
                    // Получаем символ из словаря, если он там есть, иначе '?' (хотя не должно быть)
                    char symbol = cellChars.TryGetValue(new Vector2Int(x, y), out char s) ? s : '?';
                    sb.Append($"[{symbol}]"); // Оборачиваем символ в скобки 
                }
                if (y > 0)
                {
                    sb.AppendLine();
                }
            }

            File.WriteAllText(path, sb.ToString());
            ColoredDebug.CLog(null, $"<color=green>Успешно экспортировано:</color> Шаблон '{this.name}' сохранен в '{Path.GetFileName(path)}'.", _ColoredDebug);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ArenaTemplateSO] Ошибка при экспорте файла: {e.Message}");
        }
    }
    #endregion Редактор
#endif
}