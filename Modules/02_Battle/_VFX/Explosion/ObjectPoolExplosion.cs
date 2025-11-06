using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolExplosion : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private List<GameObject> _explosionPrefabs;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private int _initialSize = 10;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Queue<GameObject> _pool = new Queue<GameObject>();
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private int _lastUsedPrefabIndex = -1;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Свойства
    private static ObjectPoolExplosion _instance;
    public static ObjectPoolExplosion Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ObjectPoolExplosion>();
            }
            return _instance;
        }
    }
    #endregion Свойства

    #region Методы UNITY
    private void Awake()
    {
        if (_instance != null)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>ObjectPoolExplosion:</color> <color=red>Обнаружен дубликат!</color> Уничтожение объекта <color=yellow>{0}</color>.", _ColoredDebug, name);
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (_explosionPrefabs == null || _explosionPrefabs.Count == 0)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>ObjectPoolExplosion:</color> <color=red>Список префабов взрывов не назначен или пуст!</color>", _ColoredDebug);
            return;
        }

        InitializePool();
    }
    #endregion Методы UNITY

    #region Публичные методы
    public GameObject GetObject()
    {
        if (_pool.Count > 0)
        {
            GameObject obj = _pool.Dequeue();
            obj.SetActive(true);
            obj.GetComponent<ParticleSystem>().Play();
            ColoredDebug.CLog(gameObject, "<color=cyan>ObjectPoolExplosion:</color> Взят объект <color=yellow>{0}</color> из пула. Осталось в пуле: <color=lime>{1}</color>.", _ColoredDebug, obj.name, _pool.Count);
            
            return obj;
        }
        else
        {
            int prefabIndex = GetRandomPrefabIndexWithReducedRepetition();
            _lastUsedPrefabIndex = prefabIndex;
            GameObject prefab = _explosionPrefabs[prefabIndex];
            GameObject obj = Instantiate(prefab, transform);
            ColoredDebug.CLog(gameObject, "<color=cyan>ObjectPoolExplosion:</color> <color=orange>Пул пуст!</color> Создан новый экземпляр <color=yellow>{0}</color>.", _ColoredDebug, prefab.name);

            obj.GetComponent<ParticleSystem>().Play();
            return obj;
        }
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
        ColoredDebug.CLog(gameObject, "<color=cyan>ObjectPoolExplosion:</color> Объект <color=yellow>{0}</color> возвращен в пул. Размер пула: <color=lime>{1}</color>.", _ColoredDebug, obj.name, _pool.Count);
    }
    #endregion Публичные методы

    #region Личные методы
    private void InitializePool()
    {
        ColoredDebug.CLog(gameObject, "<color=cyan>ObjectPoolExplosion:</color> Инициализация пула с начальным размером <color=lime>{0}</color>.", _ColoredDebug, _initialSize);
        for (int i = 0; i < _initialSize; i++)
        {
            int randomIndex = Random.Range(0, _explosionPrefabs.Count);
            GameObject prefab = _explosionPrefabs[randomIndex];
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    private int GetRandomPrefabIndexWithReducedRepetition()
    {
        if (_explosionPrefabs.Count <= 1)
        {
            return 0;
        }

        int randomIndex = Random.Range(0, _explosionPrefabs.Count);

        if (randomIndex == _lastUsedPrefabIndex)
        {
            ColoredDebug.CLog(gameObject, "<color=cyan>ObjectPoolExplosion:</color> <color=orange>Обнаружено повторение префаба!</color> Попытка перевыбора.", _ColoredDebug);
            randomIndex = Random.Range(0, _explosionPrefabs.Count);
        }

        return randomIndex;
    }
    #endregion Личные методы
}