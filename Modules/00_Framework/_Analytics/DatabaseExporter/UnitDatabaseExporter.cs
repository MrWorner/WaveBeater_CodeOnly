// НАЗНАЧЕНИЕ: Утилита для экспорта полной базы данных по всем юнитам (героям и врагам) в машиночитаемый формат для анализа нейросетями.
// ОСНОВНЫЕ ЗАВИСИСИМОСТИ: EnemyLibrary, HeroLibrary, UnitDatabaseFormatter.
// ПРИМЕЧАНИЕ: Является инструментом для разработчика и работает только в редакторе Unity.
using Sirenix.OdinInspector;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UnitDatabaseExporter : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private EnemyLibrary _enemyLibrary;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private HeroLibrary _heroLibrary;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), Tooltip("Папка, в которую будет сохранен файл базы данных.")]
    [FolderPath(AbsolutePath = true), SerializeField] private string _outputFolderPath;
    [BoxGroup("SETTINGS"), Tooltip("Имя файла для базы данных.")]
    [SerializeField] private string _outputFileName = "UnitDatabase.txt";
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Автоматический запуск в редакторе
#if UNITY_EDITOR
    [InitializeOnLoad]
    private static class EditorAutorun
    {
        /**
        static EditorAutorun()
        {
            // NOTE: Вызываем экспорт с задержкой, чтобы убедиться, что все ассеты и сцена загружены.
            EditorApplication.delayCall += () =>
            {
                // NOTE: Экспорт будет вызван только если в сцене есть активный экземпляр экспортера.
                var exporter = FindFirstObjectByType<UnitDatabaseExporter>();
                if (exporter != null)
                {
                    Debug.Log("<color=cyan>[UnitDatabaseExporter]</color> Автоматический экспорт базы данных после перезагрузки скриптов...");
                    exporter.ExportDatabase();
                }
            };
        }
        **/
    }
#endif
    #endregion

    #region Публичные методы
    /// <summary>
    /// Запускает процесс сбора данных и сохранения их в файл.
    /// </summary>
    [Button("Export Unit Database", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 1f)]
    [BoxGroup("ACTIONS", ShowLabel = false)]
    public void ExportDatabase()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>UnitDatabaseExporter:</color> Начало экспорта базы данных юнитов...", _ColoredDebug);
        if (_enemyLibrary == null || _heroLibrary == null)
        {
            Debug.LogError("[UnitDatabaseExporter] Библиотеки врагов или героев не назначены!");
            return;
        }

        var formatter = new UnitDatabaseFormatter();
        var sb = new StringBuilder();

        sb.Append(formatter.FormatHeader());
        // Форматируем героев
        foreach (var heroData in _heroLibrary.AvailableHeroes)
        {
            if (heroData != null && heroData.HeroPrefab != null)
            {
                ColoredDebug.CLog(gameObject, "<color=cyan>UnitDatabaseExporter:</color> Экспортирую данные героя <color=yellow>{0}</color>...", _ColoredDebug, heroData.HeroPrefab.name);
                sb.Append(formatter.FormatUnit(heroData.HeroPrefab.GetComponent<BattleUnit>()));
            }
        }

        // Форматируем врагов
        foreach (var enemySO in _enemyLibrary.enemyDefinitions)
        {
            if (enemySO != null && enemySO.prefab != null)
            {
                ColoredDebug.CLog(gameObject, "<color=cyan>UnitDatabaseExporter:</color> Экспортирую данные врага <color=yellow>{0}</color>...", _ColoredDebug, enemySO.prefab.name);
                sb.Append(formatter.FormatUnit(enemySO.prefab));
            }
        }

        try
        {
            if (!Directory.Exists(_outputFolderPath))
            {
                Directory.CreateDirectory(_outputFolderPath);
            }
            string fullPath = Path.Combine(_outputFolderPath, _outputFileName);
            File.WriteAllText(fullPath, sb.ToString());
            ColoredDebug.CLog(gameObject, "<color=lime>UnitDatabaseExporter:</color> База данных успешно экспортирована в <color=white>{0}</color>", _ColoredDebug, fullPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UnitDatabaseExporter] Ошибка при записи файла: {e.Message}");
        }
    }

    /// <summary>
    /// Открывает папку, содержащую файл базы данных.
    /// </summary>
    [Button("Open Output Folder"), BoxGroup("ACTIONS")]
    public void OpenOutputFolder()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(_outputFolderPath) || !Directory.Exists(_outputFolderPath))
        {
            Debug.LogWarning("[UnitDatabaseExporter] Папка для вывода не существует.");
            return;
        }
        UnityEditor.EditorUtility.RevealInFinder(_outputFolderPath);
#endif
    }
    #endregion Публичные методы
}