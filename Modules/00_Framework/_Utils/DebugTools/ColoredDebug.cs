using UnityEngine;
using System.IO;
using System;
using System.Text.RegularExpressions;

public static class ColoredDebug
{
    private static readonly string logFilePath;
    private static bool _isOldLogDeleted = false;

    static ColoredDebug()
    {
        try
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string targetDirectory = Directory.GetParent(projectPath)?.Parent?.FullName;

            if (string.IsNullOrEmpty(targetDirectory))
            {
                targetDirectory = projectPath;
                Debug.LogWarning("Не удалось найти директорию на два уровня выше. Лог-файл будет создан в папке проекта.");
            }

            logFilePath = Path.Combine(targetDirectory, "debug_log.txt");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ColoredDebug] Не удалось определить путь к лог-файлу: {e.Message}");
            logFilePath = null;
        }
    }

    public static void CLog(GameObject gameObject, string message, bool isOn, params object[] args)
    {
        bool forceShow = false;
        if (Settings.Instance != null) // Проверяем, что синглтон уже существует
        {
            forceShow = Settings.Debug_ForceShowColordebug;
        }

        if (!isOn && !forceShow)
        {
            return;
        }
		
        string gameObjectName = "(NOT MONOBEHAVIOUR)";
        if (gameObject != null && gameObject)
        {
            gameObjectName = gameObject.name;
        }

        string formattedMessage;
        try
        {
            if (args == null || args.Length == 0)
            {
                formattedMessage = message;
            }
            else
            {
                formattedMessage = string.Format(message, args);
            }
        }
        catch (FormatException ex)
        {
            Debug.LogError($"<color=red>[ColoredDebug] ОШИБКА ФОРМАТИРОВАНИЯ в логе от [{gameObjectName}]! Шаблон: \"{message}\". Ошибка: {ex.Message}</color>");
            formattedMessage = message;
        }

        Debug.Log($"<color=#D4A5A5>[{gameObjectName}]</color> {formattedMessage}");
        WriteToLogFile(gameObjectName, message, args);
    }

    private static void WriteToLogFile(string gameObjectName, string rawMessage, params object[] args)
    {
        if (string.IsNullOrEmpty(logFilePath))
        {
            return;
        }

        if (!_isOldLogDeleted)
        {
            _isOldLogDeleted = true;
            DeleteLogFile();
        }

        try
        {
            string cleanMessage = Regex.Replace(rawMessage, "<.*?>", string.Empty);
            string formattedCleanMessage = string.Format(cleanMessage, args);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] [{gameObjectName}] {formattedCleanMessage}{Environment.NewLine}";
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ColoredDebug] Ошибка записи в лог-файл ({logFilePath}): {e.Message}");
        }
    }

    public static void DeleteLogFile()
    {
        if (string.IsNullOrEmpty(logFilePath))
        {
            Debug.LogWarning("[ColoredDebug] Путь к лог-файлу не определен, удаление невозможно.");
            return;
        }

        try
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
                Debug.Log($"[ColoredDebug] Лог-файл успешно удален: {logFilePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ColoredDebug] Не удалось удалить лог-файл ({logFilePath}): {e.Message}");
        }
    }
}