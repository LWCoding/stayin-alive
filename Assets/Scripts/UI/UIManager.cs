using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

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
    
    [Header("MVP Progress")]
    [Tooltip("TextMeshProUGUI component that displays the MVP progress (current MVP population / goal).")]
    [SerializeField] private TextMeshProUGUI _mvpProgressText;
    
    [Header("Points Display")]
    [Tooltip("TextMeshProUGUI component to display the readiness points")]
    [SerializeField] private TextMeshProUGUI _pointsText;
    
    [Tooltip("TextMeshProUGUI component to display the readiness points in menu")]
    [SerializeField] private TextMeshProUGUI _pointsTextMenu;
    
    [Tooltip("Format string for displaying points. Use {0} for current points.")]
    [SerializeField] private string _pointsFormat = "{0}";
    
    [Header("Knowledge Panel")]
    [Tooltip("Button that toggles the knowledge panel visibility and interactability.")]
    [SerializeField] private Button _knowledgeButton;

    [Tooltip("Exclamation Mark when a new Knowledge is learned")]
    [SerializeField]
    private Image _knowledgeExclaimImage;
    
    [Tooltip("KnowledgeMenuGuiController that manages the knowledge panel UI.")]
    [SerializeField] private KnowledgeMenuGuiController _knowledgeMenuGuiController;
    
    [Header("Post-Processing")]
    [Tooltip("Global post-processing volume to modify vignette based on hunger. If not assigned, will attempt to find a global volume.")]
    [SerializeField] private Volume _postProcessingVolume;
    
    [Tooltip("Vignette intensity when hunger is critical (below threshold).")]
    [Range(0f, 1f)]
    [SerializeField] private float _criticalHungerVignetteIntensity = 0.7f;
    
    [Tooltip("Vignette color when hunger is critical (below threshold).")]
    [SerializeField] private Color _criticalHungerVignetteColor = Color.red;
    
    [Tooltip("Default vignette color when hunger is not critical.")]
    [SerializeField] private Color _defaultVignetteColor = Color.black;
    
    private ControllableAnimal _trackedAnimal = null;
    private Color _originalColor = Color.white;
    private Color _clear = Color.clear;
    private bool _hasStoredOriginalColor = false;
    private bool _isLowHunger = false;
    private bool _isVignetteCritical = false;
    private Vignette _vignette = null;
    private float _defaultVignetteIntensity = 0.4f;
    private bool _vignetteInitialized = false;
    private bool _hasPlayedHungerSoundForCurrentState = true;
  

    public ControllableAnimal TrackedAnimal => _trackedAnimal;

    public Canvas GetRootCanvas()
    {
      return GetComponent<Canvas>();
    }
    
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
        
        // Subscribe to knowledge button click event
        if (_knowledgeButton != null)
        {
            _knowledgeButton.onClick.AddListener(ToggleKnowledgePanel);
        }
    }

    private void Start() {
      KnowledgeManager.Instance.OnNewKnowledgeFlagChange += SetKnowledgeNotif;
      
      // Subscribe to turn advancement to update UI after player moves
      if (TimeManager.Instance != null)
      {
        TimeManager.Instance.OnTurnAdvanced += OnTurnAdvanced;
      }
      
      // Subscribe to events that change MVP population
      if (DenSystemManager.Instance != null)
      {
        DenSystemManager.Instance.OnWorkerCreated += UpdateMvpProgress;
        DenSystemManager.Instance.OnWorkerAssigned += UpdateMvpProgress;
      }
      
      // Initial updates
      UpdateMvpProgress();
      UpdatePointsDisplay();
    }
    
    private void OnDestroy()
    {
      // Unsubscribe from events
      if (TimeManager.Instance != null)
      {
        TimeManager.Instance.OnTurnAdvanced -= OnTurnAdvanced;
      }
      
      if (DenSystemManager.Instance != null)
      {
        DenSystemManager.Instance.OnWorkerCreated -= UpdateMvpProgress;
        DenSystemManager.Instance.OnWorkerAssigned -= UpdateMvpProgress;
      }
    }
    
    /// <summary>
    /// Called when a turn advances (after player moves and all animals have taken their turn).
    /// Updates UI values that might have changed during the turn.
    /// </summary>
    private void OnTurnAdvanced(int turnCount)
    {
      // Update UI after player moves - ensures player movement completes first
      UpdateMvpProgress();
      UpdatePointsDisplay();
    }

    private void Update()
    {
        // Open knowledge panel with K key
        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleKnowledgePanel();
        }
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
                // Use profile color if serialized default is still at default (black), otherwise use serialized value
                if (_vignette.color.overrideState && _defaultVignetteColor == Color.black)
                {
                    _defaultVignetteColor = _vignette.color.value;
                }
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
        
        // Reset hunger sound flag when tracking a new animal
        _hasPlayedHungerSoundForCurrentState = false;
        
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
            rectTransform.localScale = new Vector3(hungerRatio, currentScale.y, currentScale.z);
        }

        if (hungerRatio <= _lowHungerThreshold)
        {
            SetLowHungerColor(true);
            SetCriticalHungerVignette(true);
            
            // Only play hunger sound when transitioning from not-low-hunger to low-hunger
            if (!_hasPlayedHungerSoundForCurrentState)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFXType.Hunger);
                _hasPlayedHungerSoundForCurrentState = true;
            }
        }
        else
        {
            SetLowHungerColor(false);
            SetCriticalHungerVignette(false);
            
            // Reset the flag when hunger recovers above threshold
            _hasPlayedHungerSoundForCurrentState = false;
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
    /// Sets the vignette intensity and color depending on whether hunger is critical.
    /// </summary>
    private void SetCriticalHungerVignette(bool isCritical)
    {
        if (!_vignetteInitialized || _vignette == null || _postProcessingVolume == null || _isVignetteCritical == isCritical)
        {
            return;
        }
        
        // Set vignette intensity and color based on critical hunger state
        _isVignetteCritical = isCritical;
        _vignette.intensity.overrideState = true;
        _vignette.intensity.value = isCritical ? _criticalHungerVignetteIntensity : _defaultVignetteIntensity;
        
        // Set vignette color to critical color when critical, restore default otherwise
        _vignette.color.overrideState = true;
        _vignette.color.value = isCritical ? _criticalHungerVignetteColor : _defaultVignetteColor;
    }
    
    /// <summary>
    /// Updates the MVP progress text to show the current MVP population vs the goal.
    /// Format: "MVP:\n\nY/X" where Y is the current MVP population and X is the goal.
    /// </summary>
    private void UpdateMvpProgress()
    {
        if (_mvpProgressText == null)
        {
            return;
        }
        
        // Check if DenSystemManager is available
        if (DenSystemManager.Instance == null)
        {
            _mvpProgressText.text = "<size=36>MVP:</size>\n0/" + Globals.MvpWorkerGoal;
            return;
        }
        
        // Update the text with the formatted string
        _mvpProgressText.text = $"<size=36>MVP:</size>\n{WorkerManager.Instance.CurrentMvpPopulation}/{Globals.MvpWorkerGoal}";
    }
    
    /// <summary>
    /// Updates the UI text to display the current points.
    /// </summary>
    private void UpdatePointsDisplay()
    {
        if (DenSystemManager.Instance == null)
        {
            return;
        }
        
        int currentPoints = DenSystemManager.Instance.FoodInDen;
        
        if (_pointsText != null)
        {
            _pointsText.text = string.Format(_pointsFormat, currentPoints);
        }
        
        if (_pointsTextMenu != null)
        {
            _pointsTextMenu.text = string.Format(_pointsFormat, currentPoints);
        }
    }
    
    /// <summary>
    /// Toggles the knowledge panel visibility and interactability.
    /// </summary>
    private void ToggleKnowledgePanel()
    {
        Debug.Log("UIManager: ToggleKnowledgePanel called");
        
        if (_knowledgeMenuGuiController == null)
        {
            Debug.LogWarning("UIManager: KnowledgeMenuGuiController is null!");
            return;
        }
        
        // Check visibility before toggling
        bool isVisible = _knowledgeMenuGuiController.IsVisible();
        
        // If we're about to show the knowledge panel, close the den admin panel to keep menus exclusive
        if (!isVisible && DenSystemManager.Instance != null && DenSystemManager.Instance.PanelOpen)
        {
            DenSystemManager.Instance.ClosePanel();
        }
        
        // Ensure the GameObject is active
        if (!_knowledgeMenuGuiController.gameObject.activeInHierarchy)
        {
            _knowledgeMenuGuiController.gameObject.SetActive(true);
        }
        
        // Toggle visibility: if currently visible, hide it; otherwise show it
        Debug.Log($"UIManager: Panel isVisible = {isVisible}");
        
        if (isVisible)
        {
            Debug.Log("UIManager: Hiding knowledge panel");
            _knowledgeMenuGuiController.Hide();
        }
        else
        {
            Debug.Log("UIManager: Showing knowledge panel");
            _knowledgeMenuGuiController.Show();
        }
    }

    /// <summary>
    /// Checks if the knowledge panel is currently visible.
    /// </summary>
    public bool IsKnowledgePanelVisible()
    {
        return _knowledgeMenuGuiController != null && _knowledgeMenuGuiController.IsVisible();
    }

    /// <summary>
    /// Hides the knowledge panel if it is currently visible.
    /// </summary>
    public void HideKnowledgePanelIfVisible()
    {
        if (_knowledgeMenuGuiController != null && _knowledgeMenuGuiController.IsVisible())
        {
            _knowledgeMenuGuiController.Hide();
        }
    }

    private void SetKnowledgeNotif(bool shouldNotif) {
      _knowledgeExclaimImage.color = shouldNotif ? _originalColor : _clear;
    }
}

