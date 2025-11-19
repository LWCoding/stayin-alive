using UnityEngine;

/// <summary>
/// Component that lets any <see cref="Animal"/> act like a resource-carrying worker.
/// It records how much hunger was restored while foraging, then deposits the equivalent
/// amount of materials when the worker returns to its home den (readiness points).
/// </summary>
[RequireComponent(typeof(Animal))]
public class WorkerResourceCarrier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animal _animal;

    [Header("Carrying Settings")]
    [Tooltip("Readiness points awarded per unit of food carried back.")]
    [SerializeField] private int _pointsPerFoodUnit = 1;

    private int _currentCarriedFood;

    private void Awake()
    {
        if (_animal == null)
        {
            _animal = GetComponent<Animal>();
        }
    }

    private void OnEnable()
    {
        if (_animal != null)
        {
            _animal.OnFoodConsumed += OnAnimalFoodConsumed;
        }
    }

    private void OnDisable()
    {
        if (_animal != null)
        {
            _animal.OnFoodConsumed -= OnAnimalFoodConsumed;
        }
    }

    private void Update()
    {
        if (_animal == null)
        {
            return;
        }

        TryDepositAtHome();
    }

    private void OnAnimalFoodConsumed(int hungerRestored)
    {
        _currentCarriedFood += 1; 
    }

    private void TryDepositAtHome()
    {
        if (_currentCarriedFood <= 0)
        {
            return;
        }

        Den homeDen = _animal?.HomeHideable as Den;
        if (homeDen == null)
        {
            return;
        }

        bool isInHome = ReferenceEquals(_animal.CurrentHideable, homeDen) ||
                        _animal.GridPosition == homeDen.GridPosition;

        if (!isInHome)
        {
            return;
        }

        DepositCarriedFood(homeDen);
    }

    private void DepositCarriedFood(Den den)
    {
        if (_currentCarriedFood <= 0)
        {
            return;
        }

        if (PointsManager.Instance != null) 
        {
            PointsManager.Instance.AddPoints(_currentCarriedFood);
        }

        _currentCarriedFood = 0;
    }
}

