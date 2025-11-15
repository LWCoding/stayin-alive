using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    
    [Header("Post-Processing")]
    [Tooltip("Global post-processing volume to modify vignette based on hunger. If not assigned, will attempt to find a global volume.")]
    [SerializeField] private Volume _postProcessingVolume;
    
    [Tooltip("Vignette intensity when hunger is critical (below threshold).")]
    [Range(0f, 1f)]
    [SerializeField] private float _criticalHungerVignetteIntensity = 0.7f;
    
    private ControllableAnimal _trackedAnimal = null;
    private Color _originalColor = Color.white;
    private bool _hasStoredOriginalColor = false;
    private bool _isLowHunger = false;
    private bool _isVignetteCritical = false;
    private Vignette _vignette = null;
    private float _defaultVignetteIntensity = 0.4f;
    private bool _vignetteInitialized = false;

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
        
        // Initialize post-processing
        InitializePostProcessing();
    }
    
    /// <summary>
    /// Initializes the post-processing volume and gets the Vignette override.
    /// </summary>
    private void InitializePostProcessing()
    {
        if (_postProcessingVolume == null)
        {
            // Try to find a global volume if not assigned
            Volume[] volumes = FindObjectsOfType<Volume>();
            foreach (Volume volume in volumes)
            {
                if (volume.isGlobal)
                {
                    _postProcessingVolume = volume;
                    break;
                }
            }
            
            if (_postProcessingVolume == null)
            {
                Debug.LogWarning("UIManager: No global post-processing volume found. Vignette intensity changes will not be applied.");
                return;
            }
        }
        
        // Get or add Vignette override
        if (_postProcessingVolume.profile != null)
        {
            if (!_postProcessingVolume.profile.TryGet<Vignette>(out _vignette))
            {
                // If Vignette doesn't exist, add it
                _vignette = _postProcessingVolume.profile.Add<Vignette>();
            }
            
            if (_vignette != null)
            {
                _vignette.active = true;
                // Store the default intensity (use current value if already set, otherwise default)
                _defaultVignetteIntensity = _vignette.intensity.overrideState 
                    ? _vignette.intensity.value 
                    : 0.4f; // Default from the profile
                _vignetteInitialized = true;
            }
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

        // Handle color change and vignette based on hunger level
        if (hungerRatio <= _lowHungerThreshold)
        {
            SetLowHungerColor(true);
            SetCriticalHungerVignette(true);
        }
        else
        {
            SetLowHungerColor(false);
            SetCriticalHungerVignette(false);
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
    
    /// <summary>
    /// Sets the vignette intensity depending on whether hunger is critical.
    /// </summary>
    private void SetCriticalHungerVignette(bool isCritical)
    {
        if (!_vignetteInitialized || _vignette == null || _postProcessingVolume == null || _isVignetteCritical == isCritical)
        {
            return;
        }
        
        // Set vignette intensity based on critical hunger state
        _isVignetteCritical = isCritical;
        _vignette.intensity.overrideState = true;
        _vignette.intensity.value = isCritical ? _criticalHungerVignetteIntensity : _defaultVignetteIntensity;
    }
}

