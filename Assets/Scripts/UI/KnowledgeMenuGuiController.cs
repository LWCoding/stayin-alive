using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the knowledge panel UI elements and their display logic.
/// Similar to DenAdminMenuGuiController, handles the GUI logic for the knowledge panel.
/// </summary>
public class KnowledgeMenuGuiController : MonoBehaviour
{
    [Header("UI Element Game Objects")]
    [SerializeField]
    private Image mainPanelBackground;

    [SerializeField]
    private Image dimBackground;

    [SerializeField]
    private CanvasGroup visibilityController;

    public void Start()
    {
        Hide();
    }

    private void Update()
    {
        // Close panel with E key when visible
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsVisible())
            {
                Hide();
            }
        }
    }

    /// <summary>
    /// Checks if the knowledge panel is currently visible.
    /// </summary>
    public bool IsVisible()
    {
        return visibilityController != null && visibilityController.alpha > 0f;
    }

    /// <summary>
    /// Shows the knowledge panel by setting visibility and interactability.
    /// </summary>
    public void Show()
    {
        Debug.Log($"KnowledgeMenuGuiController: Show() called. GameObject active: {gameObject.activeInHierarchy}, activeSelf: {gameObject.activeSelf}");
        
        // Ensure GameObject is active
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        Debug.Log($"KnowledgeMenuGuiController: Setting localPosition to zero. Current position: {transform.localPosition}");
        transform.localPosition = Vector3.zero;
        
        if (visibilityController != null)
        {
            Debug.Log($"KnowledgeMenuGuiController: Setting CanvasGroup alpha to 1. Current alpha: {visibilityController.alpha}");
            visibilityController.alpha = 1;
            visibilityController.interactable = true;
            visibilityController.blocksRaycasts = true;
        }
        else
        {
            Debug.LogWarning("KnowledgeMenuGuiController: visibilityController is null!");
        }
        
        // Pause time to prevent player movement, similar to den admin menu
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Pause();
        }
        
        UpdateGui();
        Debug.Log($"KnowledgeMenuGuiController: Show() completed. Final position: {transform.localPosition}, alpha: {(visibilityController != null ? visibilityController.alpha.ToString() : "N/A")}");
    }

    /// <summary>
    /// Hides the knowledge panel by moving it off-screen and disabling visibility/interactivity.
    /// </summary>
    public void Hide()
    {
        transform.localPosition = Vector3.one * 10000;
        if (visibilityController != null)
        {
            visibilityController.alpha = 0;
            visibilityController.interactable = false;
            visibilityController.blocksRaycasts = false;
        }
        
        // Resume time to allow player movement, similar to den admin menu
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.Resume();
        }
    }

    /// <summary>
    /// Updates all GUI elements in the knowledge panel.
    /// Call this whenever the panel data needs to be refreshed.
    /// </summary>
    public void UpdateGui()
    {
    }
}
