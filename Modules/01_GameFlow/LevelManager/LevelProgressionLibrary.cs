using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelProgressionLibrary : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private List<LevelProgression> _availableProgressions = new List<LevelProgression>();
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), FolderPath(AbsolutePath = false, ParentFolder = "Assets")]
    [ShowIf("@UnityEngine.Application.isEditor")]
    public string searchFolder = "Scripts/LevelManager";

    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion

    #region Свойства
    public static LevelProgressionLibrary Instance { get; private set; }
    public IReadOnlyList<LevelProgression> AvailableProgressions => _availableProgressions;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (Instance != null)
        {
            DebugUtils.LogInstanceAlreadyExists(this);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ColoredDebug.CLog(gameObject, "<color=cyan>LevelProgressionLibrary:</color> Синглтон успешно инициализирован.", _ColoredDebug);
        }

        if (_availableProgressions == null || _availableProgressions.Count == 0)
        {
            ColoredDebug.CLog(gameObject, $"<color=red>[LevelProgressionLibrary]</color> Список наборов уровней ({nameof(_availableProgressions)}) не заполнен!", _ColoredDebug);
        }
    }
    #endregion

    #region Личные методы
#if UNITY_EDITOR
    [Button("Найти все наборы уровней", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.8f)]
    [BoxGroup("SETTINGS")]
    private void PopulateListFromFolder()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>LevelProgressionLibrary:</color> Начинаю поиск наборов уровней в папке <color=yellow>Assets/{0}</color>.", _ColoredDebug, searchFolder);
        _availableProgressions.Clear();

        string[] guids = AssetDatabase.FindAssets($"t:{nameof(LevelProgression)}", new[] { "Assets/" + searchFolder });
        ColoredDebug.CLog(gameObject, "<color=cyan>LevelProgressionLibrary:</color> Найдено <color=yellow>{0}</color> ассетов типа LevelProgression.", _ColoredDebug, guids.Length);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelProgression progression = AssetDatabase.LoadAssetAtPath<LevelProgression>(path);
            if (progression != null)
            {
                _availableProgressions.Add(progression);
            }
        }

        _availableProgressions = _availableProgressions.OrderBy(p => p.name).ToList();

        EditorUtility.SetDirty(this);
        ColoredDebug.CLog(gameObject, $"<color=green>LevelProgressionLibrary:</color> Список '{nameof(_availableProgressions)}' успешно обновлен! Найдено и добавлено <color=yellow>{_availableProgressions.Count}</color> наборов уровней.", _ColoredDebug);
    }
#endif
    #endregion
}

