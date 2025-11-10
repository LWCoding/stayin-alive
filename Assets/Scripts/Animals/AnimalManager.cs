using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages animal instantiation and lifecycle. Handles creating animals from level data.
/// </summary>
public class AnimalManager : Singleton<AnimalManager>
{
    [Header("Animal Settings")]
    [SerializeField] private GameObject _animalPrefab;
    [SerializeField] private Transform _animalParent;

    private List<Animal> _animals = new List<Animal>();

    protected override void Awake()
    {
        base.Awake();
        
        // Create parent if not assigned
        if (_animalParent == null)
        {
            GameObject parentObj = new GameObject("Animals");
            _animalParent = parentObj.transform;
        }
    }

    /// <summary>
    /// Clears all animals from the scene.
    /// </summary>
    public void ClearAllAnimals()
    {
        foreach (Animal animal in _animals)
        {
            if (animal != null)
            {
                Destroy(animal.gameObject);
            }
        }
        _animals.Clear();
    }

    /// <summary>
    /// Spawns an animal at the specified grid position.
    /// </summary>
    /// <param name="animalId">Unique ID for the animal</param>
    /// <param name="gridPosition">Grid position to spawn at</param>
    /// <returns>The spawned Animal component, or null if prefab is not set</returns>
    public Animal SpawnAnimal(int animalId, Vector2Int gridPosition)
    {
        if (_animalPrefab == null)
        {
            Debug.LogError("AnimalManager: Animal prefab is not assigned!");
            return null;
        }

        if (EnvironmentManager.Instance == null)
        {
            Debug.LogError("AnimalManager: EnvironmentManager instance not found!");
            return null;
        }

        // Instantiate the animal
        GameObject animalObj = Instantiate(_animalPrefab, _animalParent);
        Animal animal = animalObj.GetComponent<Animal>();
        
        if (animal == null)
        {
            Debug.LogError("AnimalManager: Animal prefab does not have an Animal component!");
            Destroy(animalObj);
            return null;
        }

        // Initialize the animal
        animal.Initialize(animalId, gridPosition);
        _animals.Add(animal);

        return animal;
    }

    /// <summary>
    /// Spawns multiple animals from level data.
    /// </summary>
    public void SpawnAnimalsFromLevelData(List<(int animalId, int x, int y)> animals)
    {
        ClearAllAnimals();

        foreach (var (animalId, x, y) in animals)
        {
            Vector2Int gridPos = new Vector2Int(x, y);
            if (EnvironmentManager.Instance != null && EnvironmentManager.Instance.IsValidPosition(gridPos))
            {
                SpawnAnimal(animalId, gridPos);
            }
            else
            {
                Debug.LogWarning($"AnimalManager: Animal {animalId} at ({x}, {y}) is out of bounds!");
            }
        }
    }

    /// <summary>
    /// Gets all animals in the scene.
    /// </summary>
    public List<Animal> GetAllAnimals()
    {
        return new List<Animal>(_animals);
    }

    /// <summary>
    /// Gets an animal by its ID.
    /// </summary>
    public Animal GetAnimalById(int animalId)
    {
        return _animals.Find(a => a != null && a.AnimalId == animalId);
    }
}

