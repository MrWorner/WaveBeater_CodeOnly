using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HeroLibrary : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private List<HeroDataSO> _availableHeroes = new List<HeroDataSO>();
    #endregion

    #region Поля
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion

    #region Свойства
    public static HeroLibrary Instance { get; private set; }
    public IReadOnlyList<HeroDataSO> AvailableHeroes => _availableHeroes;
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (Instance != null) { DebugUtils.LogInstanceAlreadyExists(this); } else { Instance = this; }
        if (_availableHeroes == null || _availableHeroes.Count == 0)
        {
            Debug.LogError($"[HeroLibrary] Список героев ({nameof(_availableHeroes)}) не заполнен!");
        }
    }
    #endregion

#if UNITY_EDITOR
    [TitleGroup("Инструменты Редактора")]
    [InfoBox("Укажите папку, в которой хранятся все ваши ассеты HeroDataSO.")]
    [FolderPath(AbsolutePath = false, ParentFolder = "Assets")]
    public string searchFolder = "Scripts/Hero_DEPRECATED/ScriptableObject";

    [TitleGroup("Инструменты Редактора")]
    [Button("Найти и заполнить список героев", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 1f)]
    private void PopulateListFromFolder()
    {
        _availableHeroes.Clear();
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(HeroDataSO)}", new[] { "Assets/" + searchFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            HeroDataSO heroSO = AssetDatabase.LoadAssetAtPath<HeroDataSO>(path);
            if (heroSO != null)
            {
                _availableHeroes.Add(heroSO);
            }
        }

        _availableHeroes = _availableHeroes.OrderBy(hero => hero.name).ToList();

        EditorUtility.SetDirty(this);
        Debug.Log($"<color=green>Список '{nameof(_availableHeroes)}' успешно обновлен!</color> Найдено и добавлено {_availableHeroes.Count} героев из папки 'Assets/{searchFolder}'.");
        ColoredDebug.CLog(gameObject, "<color=cyan>HeroLibrary:</color> Список героев обновлен. Найдено: <color=yellow>{0}</color>", _ColoredDebug, _availableHeroes.Count);
    }
#endif
}
