using UnityEngine;

/// <summary>
/// Generic singleton base class for MonoBehaviour-derived classes.
/// Ensures only one instance exists in the scene.
/// </summary>
/// <typeparam name="T">The type of the singleton class</typeparam>
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

