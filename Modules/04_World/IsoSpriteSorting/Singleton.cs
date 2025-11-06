using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static object _lock = new object();

    public static T InstanceUnsafe
    {
        get
        {
            return _instance;
        }
    }


    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong " +
                            " - there should never be more than 1 singleton!" +
                            " Reopening the scene might fix it.");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();

                        DontDestroyOnLoad(singleton);

                    }
                    else
                    {
                        /*
                        Debug.LogWarning("[Singleton] Using instance already created: " +
                            _instance.gameObject.name);
                        */
                    }
                }

                return _instance;
            }
        }
    }

    private static bool applicationIsQuitting = false;
    void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}