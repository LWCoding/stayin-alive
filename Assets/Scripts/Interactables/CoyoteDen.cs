using UnityEngine;

/// <summary>
/// A den for coyote predators. Ensures coyotes keep spawning if the den is empty.
/// </summary>
public class CoyoteDen : PredatorDen
{
    private bool _populationSystemInitialized;

    public override string GetKnowledgeTitle() {
      return "CoyoteDen";
    }
    
    private void OnEnable()
    {
        SubscribeToTurnEvents();
    }

    private void Start()
    {
        EnsurePopulationInitialization();
        EnsurePredatorPopulation();
    }

    private void OnDisable()
    {
        UnsubscribeFromTurnEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTurnEvents();
    }

    public override void Initialize(Vector2Int gridPosition)
    {
        base.Initialize(gridPosition, "Coyote");
        _populationSystemInitialized = true;
    }

    public override void Initialize(Vector2Int gridPosition, string predatorType)
    {
        base.Initialize(gridPosition, predatorType);
        _populationSystemInitialized = true;
    }

    private void SubscribeToTurnEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= HandleTurnAdvanced;
            TimeManager.Instance.OnTurnAdvanced += HandleTurnAdvanced;
        }
    }

    private void UnsubscribeFromTurnEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTurnAdvanced -= HandleTurnAdvanced;
        }
    }

    private void EnsurePopulationInitialization()
    {
        if (_populationSystemInitialized)
        {
            return;
        }

        if (EnvironmentManager.Instance != null)
        {
            _gridPosition = EnvironmentManager.Instance.WorldToGridPosition(transform.position);
        }
        else
        {
            Vector3 pos = transform.position;
            _gridPosition = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        }

        _populationSystemInitialized = true;
    }

    private void HandleTurnAdvanced(int currentTurn)
    {
        EnsurePopulationInitialization();

        if (!_populationSystemInitialized)
        {
            return;
        }

        EnsurePredatorPopulation();
    }
}

