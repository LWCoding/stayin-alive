using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

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

    [SerializeField]
    [Tooltip("The content parent transform for the scroll view where knowledge items will be instantiated")]
    private RectTransform contentParent;

    [Header("Prefabs")]
    [SerializeField]
    [Tooltip("Prefab to use when spawning knowledge items. Must have a KnowledgeItem component.")]
    private GameObject knowledgeItemPrefab;

    private List<KnowledgeItem> knowledgeItems = new List<KnowledgeItem>();
    
    [SerializeField]
    private ScrollRect scroll;

    [SerializeField] 
    private Image topGradient;
    
    [SerializeField]
    private Image bottomGradient;

    public void Start()
    {
        knowledgeItems = new List<KnowledgeItem>();
        InitializeKnowledgeItems();
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
        
        bottomGradient.color = new Color(bottomGradient.color.r, bottomGradient.color.g, bottomGradient.color.b, 0f);
        topGradient.color = new Color(topGradient.color.r, topGradient.color.g, topGradient.color.b, 0f);

        if (scroll.verticalNormalizedPosition >= 0.05f)
        {
            bottomGradient.color = new Color(bottomGradient.color.r, bottomGradient.color.g, bottomGradient.color.b, 1f);
        }
        
        if (scroll.verticalNormalizedPosition <= 0.95f)
        {
            topGradient.color = new Color(topGradient.color.r, topGradient.color.g, topGradient.color.b, 1f);
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
        
        // Don't show knowledge UI during tutorial (it will be shown at the end)
        if (TutorialManager.Instance != null && !TutorialManager.Instance.ShouldShowKnowledgeUI)
        {
            Debug.Log("KnowledgeMenuGuiController: In tutorial, not showing knowledge UI yet.");
            return;
        }
        
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
        
        KnowledgeManager.Instance.InvokeOnNewKnowledgeFlagChange(false);
        
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
        RefreshKnowledgeItems();
    }

    private void InitializeKnowledgeItems() {
      knowledgeItems = new List<KnowledgeItem>();

      // Validate required references
      if (contentParent == null)
      {
        Debug.LogWarning("KnowledgeMenuGuiController: contentParent is not assigned!");
        return;
      }

      if (knowledgeItemPrefab == null)
      {
        Debug.LogWarning("KnowledgeMenuGuiController: knowledgeItemPrefab is not assigned!");
        return;
      }

      // Get the list of knowledge (for now, use static list)
      List<KnowledgeData> knowledgeList = KnowledgeManager.Instance.AllKnowledgeData;

      // Instantiate a knowledge item for each piece of knowledge
      foreach (KnowledgeData knowledge in knowledgeList)
      {
        GameObject knowledgeItemObj = Instantiate(knowledgeItemPrefab, contentParent);
        KnowledgeItem knowledgeItem = knowledgeItemObj.GetComponent<KnowledgeItem>();
            
        if (knowledgeItem != null)
        {
          if (knowledgeItem.Initialize(knowledge.title)) {
            knowledgeItems.Add(knowledgeItem);
          }
        }
        else
        {
          Debug.LogWarning("KnowledgeMenuGuiController: Instantiated prefab does not have a KnowledgeItem component!");
          Destroy(knowledgeItemObj);
        }
      }
    }

    /// <summary>
    /// Clears existing knowledge items and creates new ones for each piece of knowledge the player knows.
    /// </summary>
    private void RefreshKnowledgeItems()
    {
        // Refresh existing knowledge items
        foreach (KnowledgeItem knowledgeItem in knowledgeItems)
        {
            if (knowledgeItem != null) {
              knowledgeItem.Refresh();
            }
        }
    }

    /// <summary>
    /// Gets the list of knowledge data the player currently knows.
    /// Loads all knowledge from KnowledgeManager. In the future, this will filter based on what the player has actually discovered.
    /// </summary>
    /// <returns>List of knowledge data</returns>
    private List<KnowledgeItem.KnowledgeData> GetPlayerKnowledge()
    {
        List<KnowledgeItem.KnowledgeData> knowledgeList = new List<KnowledgeItem.KnowledgeData>();

        if (KnowledgeManager.Instance == null)
        {
            Debug.LogWarning("KnowledgeMenuGuiController: KnowledgeManager instance not found! Cannot load knowledge data.");
            return knowledgeList;
        }

        // Get all knowledge data from KnowledgeManager
        // In the future, this should filter based on what the player has actually discovered
        foreach (KnowledgeData knowledgeData in KnowledgeManager.Instance.AllKnowledgeData)
        {
            if (knowledgeData != null)
            {
                knowledgeList.Add(new KnowledgeItem.KnowledgeData(
                    knowledgeData.sprite,
                    knowledgeData.title,
                    knowledgeData.description
                ));
            }
        }

        return knowledgeList;
    }
}
