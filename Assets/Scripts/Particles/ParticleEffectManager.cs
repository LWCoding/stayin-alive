using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages particle effects in the game. Handles spawning and pooling of particle effects.
/// Particle effects are represented by sprites that move in a direction and fade out.
/// </summary>
public class ParticleEffectManager : Singleton<ParticleEffectManager>
{
    [Header("Particle Effect Definitions")]
    [Tooltip("Define your particle effects here. Each effect has a name, sprite, direction, and fade delay.")]
    [SerializeField] private ParticleEffectDefinition[] _particleEffects;

    [Header("Object Pooling")]
    [Tooltip("Initial pool size for particles")]
    [Min(1)]
    [SerializeField] private int _initialPoolSize = 20;
    
    [Tooltip("Maximum pool size. If exceeded, particles will be destroyed instead of pooled.")]
    [Min(1)]
    [SerializeField] private int _maxPoolSize = 100;

    [Header("Spray Settings")]
    [Tooltip("Random angle spread in degrees for particle spray (e.g., 45 = ±45 degrees from base direction)")]
    [Range(0f, 180f)]
    [SerializeField] private float _sprayAngle = 45f;
    
    [Tooltip("Random speed multiplier range (e.g., 0.5 to 1.5 means 50% to 150% of base speed)")]
    [SerializeField] private Vector2 _speedRange = new Vector2(0.5f, 1.5f);

    [Header("Particle Prefab")]
    [Tooltip("Prefab for particle GameObjects. If not assigned, will be created automatically.")]
    [SerializeField] private GameObject _particlePrefab;

    private Dictionary<string, ParticleEffectData> _effectDictionary;
    private Queue<Particle> _particlePool;
    private Transform _particleParent;
    private int _activeParticleCount;

    protected override void Awake()
    {
        base.Awake();
        
        // Create parent for particles
        GameObject parentObj = new GameObject("Particles");
        _particleParent = parentObj.transform;
        
        // Initialize pool
        _particlePool = new Queue<Particle>();
        _effectDictionary = new Dictionary<string, ParticleEffectData>();
        
        // Build effect dictionary from serialized fields
        BuildEffectDictionary();
        
        // Pre-populate pool
        WarmupPool();
    }

    /// <summary>
    /// Builds the effect dictionary from serialized particle effect definitions.
    /// </summary>
    private void BuildEffectDictionary()
    {
        _effectDictionary.Clear();
        
        if (_particleEffects == null || _particleEffects.Length == 0)
        {
            Debug.LogWarning("ParticleEffectManager: No particle effects defined! Add particle effects in the inspector.");
            return;
        }

        foreach (var definition in _particleEffects)
        {
            if (string.IsNullOrEmpty(definition.name))
            {
                Debug.LogWarning("ParticleEffectManager: Found particle effect with empty name. Skipping.");
                continue;
            }

            if (_effectDictionary.ContainsKey(definition.name))
            {
                Debug.LogWarning($"ParticleEffectManager: Duplicate particle effect name '{definition.name}'. Overwriting previous definition.");
            }

            _effectDictionary[definition.name] = definition.data;
        }
    }

    /// <summary>
    /// Pre-populates the particle pool with initial particles.
    /// </summary>
    private void WarmupPool()
    {
        for (int i = 0; i < _initialPoolSize; i++)
        {
            Particle particle = CreateNewParticle();
            particle.gameObject.SetActive(false);
            _particlePool.Enqueue(particle);
        }
    }

    /// <summary>
    /// Spawns particle effects at the specified position.
    /// </summary>
    /// <param name="effectName">Name of the particle effect to spawn</param>
    /// <param name="position">World position to spawn particles at</param>
    /// <param name="count">Number of particles to spawn</param>
    public void SpawnParticleEffect(string effectName, Vector3 position, int count = 10)
    {
        if (!_effectDictionary.TryGetValue(effectName, out ParticleEffectData effectData))
        {
            Debug.LogWarning($"ParticleEffectManager: Particle effect '{effectName}' not found!");
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning($"ParticleEffectManager: Cannot spawn {count} particles. Count must be greater than 0.");
            return;
        }

        // Spawn particles
        for (int i = 0; i < count; i++)
        {
            Particle particle = GetParticleFromPool();
            if (particle == null)
            {
                continue;
            }

            // Calculate random direction within spray angle
            Vector2 baseDirection = effectData.direction.normalized;
            float randomAngle = Random.Range(-_sprayAngle / 2f, _sprayAngle / 2f) * Mathf.Deg2Rad;
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x);
            float finalAngle = baseAngle + randomAngle;
            Vector2 direction = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));

            // Calculate random speed
            float randomSpeed = Random.Range(_speedRange.x, _speedRange.y);
            Vector2 velocity = direction * randomSpeed;

            // Initialize and activate particle
            particle.transform.position = position;
            particle.gameObject.SetActive(true);
            particle.Initialize(effectData.sprite, velocity, effectData.fadeDelay);
            _activeParticleCount++;
        }
    }

    /// <summary>
    /// Gets a particle from the pool, or creates a new one if the pool is empty.
    /// </summary>
    private Particle GetParticleFromPool()
    {
        Particle particle = null;

        // Try to get from pool
        while (_particlePool.Count > 0 && particle == null)
        {
            particle = _particlePool.Dequeue();
            if (particle == null)
            {
                continue; // Pooled object was destroyed
            }
        }

        // Create new if pool is empty
        if (particle == null)
        {
            particle = CreateNewParticle();
        }

        return particle;
    }

    /// <summary>
    /// Creates a new particle GameObject.
    /// </summary>
    private Particle CreateNewParticle()
    {
        GameObject particleObj;
        
        if (_particlePrefab != null)
        {
            particleObj = Instantiate(_particlePrefab, _particleParent);
        }
        else
        {
            // Create basic particle GameObject
            particleObj = new GameObject("Particle");
            particleObj.transform.SetParent(_particleParent);
        }

        Particle particle = particleObj.GetComponent<Particle>();
        if (particle == null)
        {
            particle = particleObj.AddComponent<Particle>();
        }

        return particle;
    }

    /// <summary>
    /// Returns a particle to the pool. Called by Particle component when it finishes.
    /// </summary>
    public void ReturnParticle(Particle particle)
    {
        if (particle == null)
        {
            return;
        }

        // Only pool if under max size
        if (_particlePool.Count < _maxPoolSize)
        {
            particle.gameObject.SetActive(false);
            _particlePool.Enqueue(particle);
        }
        else
        {
            // Pool is full, destroy the particle
            Destroy(particle.gameObject);
        }

        _activeParticleCount--;
    }

    /// <summary>
    /// Gets the number of active particles currently in the scene.
    /// </summary>
    public int GetActiveParticleCount()
    {
        return _activeParticleCount;
    }

    /// <summary>
    /// Gets the number of particles in the pool.
    /// </summary>
    public int GetPoolSize()
    {
        return _particlePool.Count;
    }
}

/// <summary>
/// Serializable class for defining particle effects in the inspector.
/// </summary>
[System.Serializable]
public class ParticleEffectDefinition
{
    [Tooltip("Name used to reference this particle effect when spawning")]
    public string name;
    
    [Tooltip("Particle effect data (sprite, direction, fade delay)")]
    public ParticleEffectData data;
}



