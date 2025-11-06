// НАЗНАЧЕНИЕ: Предоставляет инструменты в редакторе Unity для массового создания и обновления ArenaTemplateSO из одного текстового файла.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: ArenaTemplateSO, ArenaTextMapSO, UnityEditor, EditorPrefs.
// ПРИМЕЧАНИЕ: Этот скрипт должен находиться в папке с именем "Editor". Он добавляет новый пункт меню в Unity.
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using System.Reflection;
using System.Text;

public class ArenaTemplateProcessor : EditorWindow
{
    #region Константы
    // Ключи для сохранения настроек окна
    private const string PREFS_SOURCE_PATH = "ArenaTemplateProcessor.SourcePath";
    private const string PREFS_MAP_PATH = "ArenaTemplateProcessor.MapPath";
    private const string PREFS_OUTPUT_PATH = "ArenaTemplateProcessor.OutputPath";
    #endregion Константы

    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private TextAsset _sourceFile;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private ArenaTextMapSO _textMap;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField, FolderPath] private string _outputPath = "Assets/Modules/02_Battle/ArenaSystem/TemplatesSO";
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector2 _scrollPosition;
    #endregion Поля

    #region Методы UNITY
    /// <summary>
    /// Отображает окно редактора.
    /// </summary>
    [MenuItem("Tools/WaveBeater/Process Arena Templates")]
    public static void ShowWindow()
    {
        GetWindow<ArenaTemplateProcessor>("Arena Template Processor");
    }

    /// <summary>
    /// Вызывается при включении или перезагрузке окна. Загружает сохраненные пути.
    /// </summary>
    private void OnEnable()
    {
        // Загружаем путь к папке
        // Используем существующее значение _outputPath как значение по умолчанию, если ничего не найдено
        _outputPath = EditorPrefs.GetString(PREFS_OUTPUT_PATH, _outputPath);

        // Загружаем путь к исходному файлу
        string sourceAssetPath = EditorPrefs.GetString(PREFS_SOURCE_PATH, null);
        if (!string.IsNullOrEmpty(sourceAssetPath))
        {
            _sourceFile = AssetDatabase.LoadAssetAtPath<TextAsset>(sourceAssetPath);
        }

        // Загружаем путь к карте символов
        string mapAssetPath = EditorPrefs.GetString(PREFS_MAP_PATH, null);
        if (!string.IsNullOrEmpty(mapAssetPath))
        {
            _textMap = AssetDatabase.LoadAssetAtPath<ArenaTextMapSO>(mapAssetPath);
        }

        ColoredDebug.CLog(null, "<color=cyan>ArenaTemplateProcessor:</color> Окно открыто, настройки путей загружены.", _ColoredDebug);
    }

    /// <summary>
    /// Вызывается при выключении или закрытии окна. Сохраняет текущие пути.
    /// </summary>
    private void OnDisable()
    {
        // Сохраняем путь к папке
        EditorPrefs.SetString(PREFS_OUTPUT_PATH, _outputPath);

        // Сохраняем путь к исходному файлу
        if (_sourceFile != null)
        {
            string sourcePath = AssetDatabase.GetAssetPath(_sourceFile);
            EditorPrefs.SetString(PREFS_SOURCE_PATH, sourcePath);
        }
        else
        {
            EditorPrefs.DeleteKey(PREFS_SOURCE_PATH); // Очищаем ключ, если поле пустое
        }

        // Сохраняем путь к карте символов
        if (_textMap != null)
        {
            string mapPath = AssetDatabase.GetAssetPath(_textMap);
            EditorPrefs.SetString(PREFS_MAP_PATH, mapPath);
        }
        else
        {
            EditorPrefs.DeleteKey(PREFS_MAP_PATH); // Очищаем ключ, если поле пустое
        }

        ColoredDebug.CLog(null, "<color=cyan>ArenaTemplateProcessor:</color> Окно закрыто, настройки путей сохранены.", _ColoredDebug);
    }

    /// <summary>
    /// Отрисовывает интерфейс окна редактора.
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("Batch Arena Template Processor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Этот инструмент обрабатывает текстовый файл, содержащий несколько шаблонов арен, и создает/обновляет соответствующие ScriptableObject ассеты.", MessageType.Info);

        _sourceFile = (TextAsset)EditorGUILayout.ObjectField("Source TXT File", _sourceFile, typeof(TextAsset), false);
        _textMap = (ArenaTextMapSO)EditorGUILayout.ObjectField("Arena Text Map", _textMap, typeof(ArenaTextMapSO), false);

        EditorGUILayout.BeginHorizontal();
        _outputPath = EditorGUILayout.TextField("Output Folder", _outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Убеждаемся, что выбранная папка находится внутри проекта
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    _outputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Пожалуйста, выберите папку внутри папки Assets вашего проекта.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Process Templates", GUILayout.Height(40)))
        {
            if (ValidateInput())
            {
                ProcessFile();
            }
        }

        EditorGUILayout.Space();
        _ColoredDebug = EditorGUILayout.Toggle("Enable Colored Debug", _ColoredDebug);
        EditorGUILayout.Space();

        // Предпросмотр содержимого файла
        if (_sourceFile != null)
        {
            EditorGUILayout.LabelField("File Preview:", EditorStyles.centeredGreyMiniLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(800));
            EditorGUILayout.TextArea(_sourceFile.text, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }
    #endregion Методы UNITY

    #region Личные методы
    /// <summary>
    /// Проверяет, все ли необходимые поля заполнены корректно.
    /// </summary>
    /// <returns>True, если все поля валидны.</returns>
    private bool ValidateInput()
    {
        if (_sourceFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Пожалуйста, укажите исходный TXT файл.", "OK");
            return false;
        }
        if (_textMap == null)
        {
            EditorUtility.DisplayDialog("Error", "Пожалуйста, укажите Arena Text Map.", "OK");
            return false;
        }
        if (string.IsNullOrEmpty(_outputPath) || !Directory.Exists(_outputPath))
        {
            EditorUtility.DisplayDialog("Error", "Указанная папка для сохранения не существует или находится вне проекта.", "OK");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Основной метод, который парсит файл и создает/обновляет ассеты ArenaTemplateSO.
    /// </summary>
    private void ProcessFile()
    {
        ColoredDebug.CLog(null, "<color=cyan>ArenaTemplateProcessor:</color> Начало обработки файла шаблонов.", _ColoredDebug);
        string content = _sourceFile.text;
        var templates = ParseTemplates(content);
        int createdCount = 0;
        int updatedCount = 0;

        if (templates.Count == 0)
        {
            ColoredDebug.CLog(null, "<color=orange>ArenaTemplateProcessor:</color> В файле не найдено корректных блоков шаблонов.", _ColoredDebug);
            EditorUtility.DisplayDialog("Warning", "Не найдено корректных шаблонов в файле.", "OK");
            return;
        }

        foreach (var parsedTemplate in templates)
        {
            // Очищаем имя от недопустимых символов
            string safeName = Regex.Replace(parsedTemplate.Name, @"[^a-zA-Z0-9_]", "_");
            string assetName = $"AT_{safeName}.asset";
            string assetPath = Path.Combine(_outputPath, assetName);

            ArenaTemplateSO templateSO = AssetDatabase.LoadAssetAtPath<ArenaTemplateSO>(assetPath);
            if (templateSO == null)
            {
                templateSO = ScriptableObject.CreateInstance<ArenaTemplateSO>();
                ApplyDataToTemplate(templateSO, parsedTemplate, _textMap);
                AssetDatabase.CreateAsset(templateSO, assetPath);
                createdCount++;
                ColoredDebug.CLog(null, $"<color=green>СОЗДАН:</color> Новый шаблон арены <color=white>'{assetName}'</color> сохранен в '{_outputPath}'.", _ColoredDebug);
            }
            else
            {
                ApplyDataToTemplate(templateSO, parsedTemplate, _textMap);
                EditorUtility.SetDirty(templateSO);
                updatedCount++;
                ColoredDebug.CLog(null, $"<color=yellow>ОБНОВЛЕН:</color> Шаблон арены <color=white>'{assetName}'</color> в '{_outputPath}' был обновлен.", _ColoredDebug);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ColoredDebug.CLog(null, "<color=lime>ArenaTemplateProcessor:</color> Обработка завершена.", _ColoredDebug);
        EditorUtility.DisplayDialog("Processing Complete", $"Готово!\n\nСоздано новых шаблонов: {createdCount}\nОбновлено существующих: {updatedCount}", "OK");
    }

    /// <summary>
    /// Парсит текстовый контент, разделяя его на отдельные именованные шаблоны.
    /// </summary>
    /// <param name="text">Полный текст исходного файла.</param>
    /// <returns>Список распарсенных шаблонов.</returns>
    private List<ParsedTemplate> ParseTemplates(string text)
    {
        var templates = new List<ParsedTemplate>();
        // Разделяем текст на блоки по двойному переносу строки
        string[] templateBlocks = text.Split(new[] { "\r\n\r\n", "\n\n", "\r\r" }, System.StringSplitOptions.RemoveEmptyEntries);
        ColoredDebug.CLog(null, $"<color=cyan>ArenaTemplateProcessor:</color> Найдено <color=yellow>{templateBlocks.Length}</color> блоков текста.", _ColoredDebug);

        foreach (var block in templateBlocks)
        {
            // Разделяем блок на строки
            var lines = block.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1) // Должны быть как минимум имя и одна строка карты
            {
                // Первая строка - имя шаблона
                string nameLine = lines[0].Trim();
                // Очищаем имя: заменяем пробелы на '_' и удаляем все остальные недопустимые символы
                string cleanedName = Regex.Replace(nameLine, @"\s+", "_");
                cleanedName = Regex.Replace(cleanedName, @"[^a-zA-Z0-9_]", "");

                if (!string.IsNullOrEmpty(cleanedName))
                {
                    // Остальные строки - карта
                    var template = new ParsedTemplate
                    {
                        Name = cleanedName,
                        Lines = lines.Skip(1).ToList() // Пропускаем первую строку (имя)
                    };
                    templates.Add(template);
                    ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> Распознан шаблон: <color=white>{cleanedName}</color> ({template.Lines.Count} строк карты).", _ColoredDebug);
                }
                else
                {
                    ColoredDebug.CLog(null, "<color=orange>ArenaTemplateProcessor:</color> Пропущен блок с некорректным именем.", _ColoredDebug);
                }
            }
            else
            {
                ColoredDebug.CLog(null, "<color=orange>ArenaTemplateProcessor:</color> Пропущен блок с недостаточным количеством строк.", _ColoredDebug);
            }
        }
        return templates;
    }

    /// <summary>
    /// Применяет распарсенные данные (карту) к ScriptableObject ArenaTemplateSO.
    /// </summary>
    /// <param name="templateSO">Целевой ScriptableObject.</param>
    /// <param name="parsedData">Распарсенные данные из текстового файла.</param>
    /// <param name="textMap">Карта сопоставления символов.</param>
    private void ApplyDataToTemplate(ArenaTemplateSO templateSO, ParsedTemplate parsedData, ArenaTextMapSO textMap)
    {
        ColoredDebug.CLog(null, $"<color=cyan>ArenaTemplateProcessor:</color> Применение данных к шаблону <color=white>{templateSO.name}</color>...", _ColoredDebug);

        // Используем рефлексию для доступа к приватным полям ScriptableObject
        var propPlacementsField = typeof(ArenaTemplateSO).GetField("_propPlacements", BindingFlags.NonPublic | BindingFlags.Instance);
        var cellStatesField = typeof(ArenaTemplateSO).GetField("_cellStates", BindingFlags.NonPublic | BindingFlags.Instance);
        var cellTypesField = typeof(ArenaTemplateSO).GetField("_cellTypes", BindingFlags.NonPublic | BindingFlags.Instance);
        var missingCellsField = typeof(ArenaTemplateSO).GetField("_missingCells", BindingFlags.NonPublic | BindingFlags.Instance);
        var widthField = typeof(ArenaTemplateSO).GetField("_width", BindingFlags.NonPublic | BindingFlags.Instance);
        var heightField = typeof(ArenaTemplateSO).GetField("_height", BindingFlags.NonPublic | BindingFlags.Instance);
        var textMapField = typeof(ArenaTemplateSO).GetField("_textMap", BindingFlags.NonPublic | BindingFlags.Instance);

        // Получаем ссылки на списки внутри ScriptableObject
        var propPlacements = (List<ArenaTemplateSO.PropPlacementData>)propPlacementsField?.GetValue(templateSO);
        var cellStates = (List<ArenaTemplateSO.CellStateData>)cellStatesField?.GetValue(templateSO);
        var cellTypes = (List<ArenaTemplateSO.CellTypeData>)cellTypesField?.GetValue(templateSO);
        var missingCells = (List<Vector2Int>)missingCellsField?.GetValue(templateSO);

        // Проверяем, удалось ли получить доступ ко всем полям
        if (propPlacements == null || cellStates == null || cellTypes == null || missingCells == null || widthField == null || heightField == null || textMapField == null)
        {
            // Используем Debug.LogError, т.к. это критическая ошибка рефлексии [cite: 29]
            Debug.LogError($"[ArenaTemplateProcessor] Ошибка доступа к приватным полям ArenaTemplateSO '{templateSO.name}' через рефлексию!");
            return;
        }

        // Очищаем старые данные
        propPlacements.Clear();
        cellStates.Clear();
        cellTypes.Clear();
        missingCells.Clear();

        // Определяем размеры сетки
        int height = parsedData.Lines.Count;
        int width = 0;
        if (height > 0)
        {
            // Убираем скобки для корректного подсчета ширины
            string firstCleanedLine = parsedData.Lines[0].Replace("[", "").Replace("]", "");
            width = firstCleanedLine.Length;
        }

        // Устанавливаем размеры и ссылку на карту символов
        heightField.SetValue(templateSO, height);
        widthField.SetValue(templateSO, width);
        textMapField.SetValue(templateSO, textMap);
        ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> Установлены размеры: <color=yellow>{width}x{height}</color>.", _ColoredDebug);

        // Обрабатываем каждую строку карты
        for (int y = 0; y < height; y++)
        {
            string line = parsedData.Lines[y];
            // Удаляем квадратные скобки из строки
            string cleanedLine = line.Replace("[", "").Replace("]", "");

            // Обрабатываем каждый символ в строке
            for (int x = 0; x < width; x++)
            {
                // Если строка короче ожидаемой ширины, считаем остаток отсутствующими клетками
                if (x >= cleanedLine.Length)
                {
                    // Используем 'y' напрямую, чтобы не зеркалить по вертикали
                    var missingPos = new Vector2Int(x, y);
                    missingCells.Add(missingPos);
                    ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> Позиция ({x},{y}) отмечена как 'отсутствующая' (строка короче).", _ColoredDebug);
                    continue;
                }

                char c = cleanedLine[x];
                // Используем 'y' напрямую, чтобы не зеркалить по вертикали
                var pos = new Vector2Int(x, y);

                if (c == ' ') // Пробел - отсутствующая клетка
                {
                    missingCells.Add(pos);
                    ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> Позиция ({x},{y}) отмечена как 'отсутствующая' (пробел).", _ColoredDebug);
                }
                else if (c == '~') // Тильда - треснувшая клетка
                {
                    cellStates.Add(new ArenaTemplateSO.CellStateData { Position = pos, State = BattleCell.CellState.Cracked });
                    ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> Позиция ({x},{y}) отмечена как 'треснувшая'.", _ColoredDebug);
                }
                else if (c == 'O' || c == 'o') // Буква O - дыра
                {
                    cellStates.Add(new ArenaTemplateSO.CellStateData { Position = pos, State = BattleCell.CellState.Hole });
                    ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> Позиция ({x},{y}) отмечена как 'дыра'.", _ColoredDebug);
                }
                else
                {
                    // Проверяем ТИП клетки (Indestructible, Glass, etc.)
                    BattleCell.CellType? specificType = textMap.GetCellType(c);
                    if (specificType.HasValue)
                    {
                        cellTypes.Add(new ArenaTemplateSO.CellTypeData { Position = pos, Type = specificType.Value });
                        ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> Позиция ({x},{y}) отмечена как тип '{specificType.Value}'.", _ColoredDebug);
                    }
                    else
                    {
                        // Если это не тип, проверяем ПРОП
                        PropSO prop = textMap.GetProp(c);
                        if (prop != null)
                        {
                            propPlacements.Add(new ArenaTemplateSO.PropPlacementData { Prop = prop, Position = pos });
                            ColoredDebug.CLog(null, $"<color=grey>ArenaTemplateProcessor:</color> В позицию ({x},{y}) помещен проп '{prop.name}'.", _ColoredDebug);
                        }
                        else if (c != '.') // Если это не '.', '.' - стандартная клетка
                        {
                            // Если символ не распознан ни как тип, ни как проп, ни как спец. символ
                            ColoredDebug.CLog(null, $"<color=orange>ArenaTemplateProcessor:</color> Не найдено сопоставление для символа '{c}' в шаблоне '{parsedData.Name}' в позиции ({x},{y}). Клетка будет Standard/Intact.", _ColoredDebug);
                        }
                        // else: Символ '.' означает Standard/Intact - ничего не делаем
                    }
                }
            }
        }
        ColoredDebug.CLog(null, $"<color=lime>ArenaTemplateProcessor:</color> Данные для <color=white>{templateSO.name}</color> успешно применены.", _ColoredDebug);
    }

    /// <summary>
    /// Вспомогательная структура для хранения распарсенных данных одного шаблона.
    /// </summary>
    private class ParsedTemplate
    {
        public string Name;
        public List<string> Lines;
    }
    #endregion Личные методы
}