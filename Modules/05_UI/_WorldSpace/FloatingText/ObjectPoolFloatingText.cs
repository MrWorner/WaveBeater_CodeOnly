// НАЗНАЧЕНИЕ: Пул объектов для плавающего текста (Object Pool)
// ЗАВИСИМОСТИ: FloatingText (предполагается), Queue<GameObject>
// ПРИМЕЧАНИЕ: Singleton. Расширяется, если пул пуст.
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using System.Collections.Generic;
using System;

public class ObjectPoolFloatingText : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField]
    private GameObject _floatingTextPrefab;
    #endregion

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField]
    private int _poolSize = 5;

    [BoxGroup("DEBUG"), SerializeField, ReadOnly]
    private static ObjectPoolFloatingText _instance;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly]
    private Queue<GameObject> _pool = new();
    [BoxGroup("DEBUG"), SerializeField]
    protected bool _ColoredDebug = true;
    #endregion

    #region Свойства
    public static ObjectPoolFloatingText Instance
    {
        get => _instance;
    }
    #endregion

    #region Методы UNITY
    private void Awake()
    {
        if (_instance) DebugUtils.LogInstanceAlreadyExists(this, _instance);
        else _instance = this;

        if (_floatingTextPrefab == null) DebugUtils.LogMissingReference(this, nameof(_floatingTextPrefab));

        InitializePool();

        ColoredDebug.CLog(gameObject, "<color=orange>SYSTEM:</color> Object Pool <color=cyan>{0}</color> initialized with size <color=yellow>{1}</color>.", _ColoredDebug, nameof(ObjectPoolFloatingText), _poolSize);
    }
    #endregion

    #region Публичные методы
    public GameObject RetrieveObject()
    {
        GameObject obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
            ColoredDebug.CLog(gameObject, "<color=lime>[ACTION]</color> Извлечен объект из пула. Осталось: <color=yellow>{0}</color>", _ColoredDebug, _pool.Count);
        }
        else
        {
            obj = Instantiate(_floatingTextPrefab);
            obj.transform.SetParent(transform);
            ColoredDebug.CLog(gameObject, "<color=orange>[SYSTEM]</color> Пул пуст. Создан новый объект: <color=yellow>{0}</color>.", _ColoredDebug, obj.name);
        }

        obj.SetActive(true);
        return obj;
    }

    public void ReturnObjectToPool(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _pool.Enqueue(obj);

        ColoredDebug.CLog(gameObject, "<color=lime>[ACTION]</color> Объект возвращен в пул. Размер пула: <color=yellow>{0}</color>", _ColoredDebug, _pool.Count);
    }
    #endregion

    #region Личные методы
    private void InitializePool()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            GameObject obj = Instantiate(_floatingTextPrefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            _pool.Enqueue(obj);
            obj.SetActive(false);
        }
    }
    #endregion
}