using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages UI elements for the game, including the hunger bar for controllable animals.
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [Header("Hunger Bar")]
    [Tooltip("Image component that displays the hunger bar. The Y scale will be set based on hunger ratio (0 = dead, 1 = full).")]
    [SerializeField] private Image _hungerBarImage;
    
    [Tooltip("Hunger ratio threshold below which the bar will turn red (0.0 to 1.0).")]
    [Range(0f, 1f)]
    [SerializeField] private float _lowHungerThreshold = 0.33f;
    
    private ControllableAnimal _trackedAnimal = null;
    private Color _originalColor = Color.white;
    private bool _hasStoredOriginalColor = false;
    private bool _isLowHunger = false;

    public ControllableAnimal TrackedAnimal => _trackedAnimal;

    protected override void Awake()
    {
        base.Awake();
        
        // Store original color if hunger bar image exists
        if (_hungerBarImage != null)
        {
            _originalColor = _hungerBarImage.color;
            _hasStoredOriginalColor = true;
        }
    }

    /// <summary>
    /// Sets the controllable animal to track for hunger display.
    /// </summary>
    public void SetTrackedAnimal(ControllableAnimal animal)
    {
        if (_trackedAnimal == animal)
        {
            return;
        }

        _trackedAnimal = animal;
        
        // Store original color if we haven't already (in case image was assigned after Awake)
        if (!_hasStoredOriginalColor && _hungerBarImage != null)
        {
            _originalColor = _hungerBarImage.color;
            _hasStoredOriginalColor = true;
        }
        
        UpdateHungerBar();
    }

    /// <summary>
    /// Returns true if the UI is currently tracking the specified animal.
    /// </summary>
    public bool IsTrackingAnimal(ControllableAnimal animal)
    {
        return _trackedAnimal == animal;
    }

    /// <summary>
    /// Updates the hunger bar display based on the tracked animal's hunger.
    /// Sets the Y scale of the bar (0 = dead, 1 = full).
    /// </summary>
    public void UpdateHungerBar()
    {
        if (_hungerBarImage == null)
        {
            return;
        }

        float hungerRatio = 0f;
        if (_trackedAnimal != null)
        {
            hungerRatio = _trackedAnimal.HungerRatio;
        }

        // Clamp to 0-1 range
        hungerRatio = Mathf.Clamp01(hungerRatio);

        // Update the Y scale of the hunger bar
        RectTransform rectTransform = _hungerBarImage.rectTransform;
        if (rectTransform != null)
        {
            Vector3 currentScale = rectTransform.localScale;
            rectTransform.localScale = new Vector3(currentScale.x, hungerRatio, currentScale.z);
        }

        // Handle color change based on hunger level
        if (hungerRatio <= _lowHungerThreshold)
        {
            SetLowHungerColor(true);
        }
        else
        {
            SetLowHungerColor(false);
        }
    }

    /// <summary>
    /// Sets the hunger bar color depending on whether hunger is low.
    /// </summary>
    private void SetLowHungerColor(bool isLow)
    {
        if (_hungerBarImage == null || _isLowHunger == isLow)
        {
            return;
        }

        _isLowHunger = isLow;
        _hungerBarImage.color = _isLowHunger ? Color.red : _originalColor;
    }

    private void Update()
    {
        // Update hunger bar every frame to reflect current hunger
        if (_trackedAnimal != null)
        {
            UpdateHungerBar();
        }
    }
}

