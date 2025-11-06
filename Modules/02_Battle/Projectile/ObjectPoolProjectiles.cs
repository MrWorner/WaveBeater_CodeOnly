using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Добавим для удобства

/// <summary>
/// Singleton-класс для пула объектов снарядов.
/// </summary>
public class ObjectPoolProjectiles : MonoBehaviour
{
    public static ObjectPoolProjectiles Instance { get; private set; }

    [Header("Настройки пула")]
    [Tooltip("Префаб снаряда, который будет использоваться в пуле.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Начальный размер пула.")]
    [SerializeField] private int poolSize = 10;

    private List<GameObject> pooledObjects;

    // Это свойство будет возвращать список всех активных в данный момент снарядов.
    public List<GameObject> ActiveProjectiles => pooledObjects?.Where(p => p.activeInHierarchy).ToList() ?? new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is not assigned! Object Pool will not be initialized.");
            return;
        }

        pooledObjects = new List<GameObject>();
        GameObject parent = new GameObject("Projectile Pool");
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(projectilePrefab, parent.transform);
            obj.SetActive(false);
            pooledObjects.Add(obj);
        }
    }

    public GameObject GetObject()
    {
        foreach (GameObject obj in pooledObjects)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = Instantiate(projectilePrefab);
        pooledObjects.Add(newObj);
        return newObj;
    }

    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
        }
    }
}