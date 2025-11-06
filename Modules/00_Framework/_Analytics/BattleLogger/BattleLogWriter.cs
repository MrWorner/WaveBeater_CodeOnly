// НАЗНАЧЕНИЕ: Отвечает за все операции с файловой системой для логгера, включая создание, очистку, запись и открытие файла лога.
// ОСНОВНЫЕ ЗАВИСИМОСТИ: System.IO.
// ПРИМЕЧАНИЕ: Является вспомогательным классом для BattleLogger, изолируя его от деталей работы с файлами.
using System.IO;
using UnityEngine;

public class BattleLogWriter
{
    private string _logFilePath;

    /// <summary>
    /// Инициализирует писатель, задает путь к файлу и очищает старый лог.
    /// </summary>
    /// <param name="fullPath">Полный путь к файлу лога, включая имя файла.</param>
    public void Initialize(string fullPath)
    {
        try
        {
            _logFilePath = fullPath;
            // Убеждаемся, что директория существует, перед тем как пытаться создать в ней файл.
            string directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(_logFilePath))
            {
                File.Delete(_logFilePath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleLogWriter] Ошибка при инициализации файла лога: {e.Message}");
            _logFilePath = null;
        }
    }

    /// <summary>
    /// Добавляет строку в конец файла лога.
    /// </summary>
    /// <param name="logEntry">Строка для добавления.</param>
    public void Append(string logEntry)
    {
        if (string.IsNullOrEmpty(_logFilePath)) return;

        try
        {
            File.AppendAllText(_logFilePath, logEntry);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleLogWriter] Ошибка при записи в файл лога: {e.Message}");
        }
    }

    /// <summary>
    /// Открывает созданный файл лога с помощью приложения по умолчанию.
    /// </summary>
    /// <param name="logPath">Полный путь к файлу, который нужно открыть.</param>
    public void OpenLogFile(string logPath)
    {
        if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
        {
            Debug.LogWarning($"[BattleLogWriter] Файл лога не существует по пути '{logPath}' и не может быть открыт.");
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.OpenWithDefaultApp(logPath);
#else
        Application.OpenURL(logPath);
#endif
    }

    /// <summary>
    /// Открывает папку, содержащую файл лога, в проводнике.
    /// </summary>
    /// <param name="folderPath">Путь к папке для открытия.</param>
    public void OpenLogFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[BattleLogWriter] Папка '{folderPath}' не существует и не может быть открыта.");
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(folderPath);
#else
        Application.OpenURL(folderPath);
#endif
    }
}