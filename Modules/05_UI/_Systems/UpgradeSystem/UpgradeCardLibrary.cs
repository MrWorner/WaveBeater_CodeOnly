using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;

public class UpgradeCardLibrary : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private List<UpgradeCardDataSO> _allUpgradeCards = new List<UpgradeCardDataSO>();
    #endregion Поля: Required

    #region Поля
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private static UpgradeCardLibrary _instance;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    public static UpgradeCardLibrary Instance { get => _instance; }
    public IReadOnlyList<UpgradeCardDataSO> AllUpgradeCards { get => _allUpgradeCards; }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DebugUtils.LogInstanceAlreadyExists(this);
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (_allUpgradeCards == null || _allUpgradeCards.Count == 0)
        {
            Debug.LogError($"[UpgradeCardLibrary] Список улучшений ({nameof(_allUpgradeCards)}) не заполнен!");
        }
    }
    #endregion Методы UNITY

    #region Публичные методы
    /// <summary>
    /// Возвращает ОДНУ случайную карту из всей библиотеки.
    /// </summary>
    public UpgradeCardDataSO GetRandomCard()
    {
        if (_allUpgradeCards.Count == 0) return null;
        return _allUpgradeCards[Random.Range(0, _allUpgradeCards.Count)];
    }

    /// <summary>
    /// Возвращает список из нескольких СЛУЧАЙНЫХ и УНИКАЛЬНЫХ карт из всех категорий.
    /// </summary>
    /// <param name="count">Количество уникальных карт для выбора.</param>
    public List<UpgradeCardDataSO> GetRandomUniqueCards(int count)
    {
        // Вызываем новый метод, передавая NotSet, чтобы не фильтровать по категории
        return GetRandomUniqueCards(count, UpgradeCardDataSO.CardTCategory.NotSet);
    }

    /// <summary>
    /// Возвращает список из нескольких СЛУЧАЙНЫХ и УНИКАЛЬНЫХ карт указанной категории.
    /// Идеально подходит для заполнения магазина.
    /// </summary>
    /// <param name="count">Количество уникальных карт для выбора.</param>
    /// <param name="category">Категория карт для фильтрации.</param>
    public List<UpgradeCardDataSO> GetRandomUniqueCards(int count, UpgradeCardDataSO.CardTCategory category)
    {
        List<UpgradeCardDataSO> availableCards;

        // Если категория не задана (NotSet), используем все карты. Иначе - фильтруем.
        if (category == UpgradeCardDataSO.CardTCategory.NotSet)
        {
            availableCards = new List<UpgradeCardDataSO>(_allUpgradeCards);
        }
        else
        {
            availableCards = _allUpgradeCards.Where(card => card.Сategory == category).ToList();
            ColoredDebug.CLog(gameObject, $"<color=cyan>UpgradeCardLibrary:</color> Отфильтровано <color=yellow>{availableCards.Count}</color> карт по категории <color=orange>{category}</color>.", _ColoredDebug);
        }

        List<UpgradeCardDataSO> chosenCards = new List<UpgradeCardDataSO>();

        if (count > availableCards.Count)
        {
            ColoredDebug.CLog(gameObject, "<color=orange>UpgradeCardLibrary:</color> Запрошено карт <color=yellow>{0}</color> (категория: {3}), но доступно только <color=yellow>{1}</color>. Возвращаю все, что есть.", _ColoredDebug, count, availableCards.Count, category);
            count = availableCards.Count;
        }

        for (int i = 0; i < count; i++)
        {
            if (availableCards.Count == 0) break;

            int randomIndex = Random.Range(0, availableCards.Count);
            UpgradeCardDataSO selectedCard = availableCards[randomIndex];

            chosenCards.Add(selectedCard);
            availableCards.RemoveAt(randomIndex);
        }

        ColoredDebug.CLog(gameObject, "<color=cyan>UpgradeCardLibrary:</color> Выбрано <color=yellow>{0}</color> уникальных карт для магазина (категория: {1}).", _ColoredDebug, chosenCards.Count, category);
        return chosenCards;
    }


    /// <summary>
    /// Находит и возвращает первую попавшуюся карту по ее типу бонуса.
    /// </summary>
    public UpgradeCardDataSO GetCardByType(UpgradeCardDataSO.CardTypeBonus type)
    {
        return _allUpgradeCards.FirstOrDefault(card => card.BonusType == type);
    }
    #endregion Публичные методы



#if UNITY_EDITOR
    [Button("Find All Cards")]
    [GUIColor(0.3f, 0.8f, 1f)]
    private void FindAllCardsInProject()
    {
        _allUpgradeCards.Clear();

        string[] guids = AssetDatabase.FindAssets("t:UpgradeCardDataSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UpgradeCardDataSO card = AssetDatabase.LoadAssetAtPath<UpgradeCardDataSO>(path);
            if (card != null)
            {
                _allUpgradeCards.Add(card);
            }
        }

        Debug.Log($"[UpgradeCardLibrary] Найдено и добавлено {_allUpgradeCards.Count} UpgradeCardDataSO.");
        EditorUtility.SetDirty(this);
    }
#endif
}