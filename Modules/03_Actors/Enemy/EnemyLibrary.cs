using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector;
#endif

public class EnemyLibrary : MonoBehaviour
{
    // Список ScriptableObject'ов с определениями врагов.
    // Атрибут [ReadOnly] не даст случайно изменить список вручную в инспекторе.
    ///[ReadOnly]
    public List<EnemySO> enemyDefinitions = new List<EnemySO>();

    /// <summary>
    /// Выбор случайного врага из библиотеки, у которого есть хотя бы один вариант, стоящий не больше, чем pointsLeft.
    /// </summary>
    public EnemySO ChooseEnemy(int pointsLeft)
    {
        // Используем FindAll для получения всех подходящих врагов
        // --- ИСПРАВЛЕНО: Проверяем стоимость внутри списка вариантов ---
        List<EnemySO> affordable = enemyDefinitions.FindAll(def => def.AvailableVariants.Any(v => v.threat <= pointsLeft));

        // Если нет доступных врагов, возвращаем null
        if (affordable.Count == 0)
        {
            return null;
        }

        // Возвращаем случайного врага из списка подходящих
        return affordable[Random.Range(0, affordable.Count)];
    }


    // --- ИНСТРУМЕНТ ДЛЯ РЕДАКТОРА ---
    // Весь код ниже будет работать только в редакторе Unity и не попадет в финальную сборку игры.
#if UNITY_EDITOR

    [TitleGroup("Инструменты Редактора")]
    [InfoBox("Укажите папку, в которой хранятся все ваши ассеты EnemySO.")]
    [FolderPath] // Odin атрибут для удобного выбора папки
    public string searchFolder = "Assets/Scripts/Enemy_DEPRECATED/ScriptableObject"; // Укажите ваш путь по умолчанию

    [TitleGroup("Инструменты Редактора")]
    [Button("Найти и заполнить список врагов", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
    private void PopulateListFromFolder()
    {
        // 1. Проверяем, что путь указан
        if (string.IsNullOrEmpty(searchFolder))
        {
            Debug.LogError("Путь к папке (Search Folder) не указан!");
            return;
        }

        // 2. Очищаем текущий список, чтобы избежать дубликатов при повторном нажатии
        enemyDefinitions.Clear();

        // 3. Ищем GUID'ы всех ассетов типа EnemySO в указанной папке
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(EnemySO)}", new[] { searchFolder });

        // 4. Загружаем каждый ассет по его GUID и добавляем в список
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemySO enemySO = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
            if (enemySO != null)
            {
                enemyDefinitions.Add(enemySO);
            }
        }

        // (Опционально) Сортируем список по стоимости для удобства просмотра в инспекторе
        enemyDefinitions = enemyDefinitions.OrderBy(enemy => enemy.AvailableVariants.Any() ? enemy.AvailableVariants.Min(v => v.threat) : int.MaxValue).ToList();

        // 5. "Загрязняем" объект, чтобы Unity понял, что его нужно сохранить
        EditorUtility.SetDirty(this);
        Debug.Log($"<color=green>Список 'enemyDefinitions' успешно обновлен!</color> Найдено и добавлено {enemyDefinitions.Count} врагов из папки '{searchFolder}'.");
    }

#endif
}