using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DenAdminMenuGuiController : MonoBehaviour {
  [Header("UI Element Game Objects")]
  [SerializeField]
  private Image mainPanelBackground;

  [SerializeField]
  private Image dimBackground;

  [SerializeField]
  private TextMeshProUGUI denPointsTest;

  [SerializeField]
  private RectTransform mapPanel;

  public Vector2 mapPanelDimensions => mapPanel.rect.size;

  [SerializeField]
  private CanvasGroup visibilityController;

  [SerializeField]
  private RawImage currentDenRender;

  [SerializeField]
  private RectTransform currentDenWorkers;

  [SerializeField]
  private RectTransform unassignedWorkers;
  
  [Header("GUI Controllers")]
  [SerializeField]
  private DenInventoryPanelGuiController inventoryPanel;
  
  [Header("Prefabs")]
  [SerializeField]
  private GameObject denMapIconPrefab;

  [SerializeField]
  private GameObject workerIconPrefab;

  [Header("Buttons")]
  [SerializeField]
  [Tooltip("Button that will purchase a worker when clicked")]
  private Button purchaseWorkerButton;
  
  [SerializeField]
  [Tooltip("Text displayed on the purchase worker button when den is not full")]
  private string purchaseWorkerButtonText;

  [SerializeField]
  [Tooltip("Text displayed on the purchase worker button when den is at maximum capacity")]
  private string denFullButtonText;
  
  private List<DenSystemManager.DenInformation> mapDenInfo;

  private List<DenMapIconGuiController> mapDenMapIcons;

  private List<WorkerIconGuiController> workerIcons;

  public void UpdateGui() {
    UpdateDenMapIcons();
    UpdateCurrentDenRenderTexture();
    UpdateCurrentDenWorkers();
    UpdatePurchaseWorkerButton();
    inventoryPanel.RefreshGui();
  }

  public void Start() {
    mapDenMapIcons = new List<DenMapIconGuiController>();
    workerIcons = new List<WorkerIconGuiController>();
    
    Hide();
    
    // Setup button listener
    if (purchaseWorkerButton != null) {
      purchaseWorkerButton.onClick.AddListener(OnPurchaseWorkerButtonClicked);
    }
  }

  public void Show() {
    transform.localPosition = Vector3.zero;
    visibilityController.alpha = 1;
    visibilityController.interactable = true;
    UpdateGui();
  }

  public void Hide() {
    transform.localPosition = Vector3.one * 10000;
    visibilityController.alpha = 0;
    visibilityController.interactable = false;
  }

  public void UpdateDenMapIcons() {
    CreateDenMapIcons(DenSystemManager.Instance.ConstructDenInfos().Values.ToList());
  }
  
  public void CreateDenMapIcons(List<DenSystemManager.DenInformation> denInfos) {
    foreach (DenMapIconGuiController mapIconGuiController in mapDenMapIcons) {
      Destroy(mapIconGuiController.gameObject);
    }

    mapDenMapIcons = new List<DenMapIconGuiController>();
    foreach (DenSystemManager.DenInformation denInfo in denInfos) {
      GameObject DenMapIconGameObj = Instantiate(denMapIconPrefab, mapPanel);
      DenMapIconGuiController denMapIconController = DenMapIconGameObj.GetComponent<DenMapIconGuiController>();
      denMapIconController.InitializeDenMapIcon(denInfo);
      mapDenMapIcons.Add(denMapIconController);
    }
  }

  public void UpdateCurrentDenRenderTexture() {
    currentDenRender.texture = DenSystemManager.Instance.CurrentAdminDen.DenRenderTexture;
  }

  public void UpdateCurrentDenWorkers() {
    foreach (WorkerIconGuiController workerIcon in workerIcons) {
      Destroy(workerIcon.gameObject);
    }

    workerIcons = new List<WorkerIconGuiController>();
    // TODO: (leythtoubassy) fix this loop to only loop over current admin den workers
    foreach (Animal worker in WorkerManager.Instance.AllWorkers) {
      RectTransform workerIconParent = unassignedWorkers;
      if (WorkerManager.Instance.WorkersToDens[worker] ==
          DenSystemManager.Instance.CurrentAdminDen.GetDenInfo().denId) {
        workerIconParent = currentDenWorkers;
      }
      else if (WorkerManager.Instance.WorkersToDens[worker] !=
          WorkerManager.UNASSIGNED_DEN_ID) {
        continue;
      }
      
      GameObject workerIconGO = Instantiate(workerIconPrefab, workerIconParent);
      WorkerIconGuiController workerIcon = workerIconGO.GetComponent<WorkerIconGuiController>();
      workerIcon.InitializeWorkerIcon(worker);
      workerIcons.Add(workerIcon);
    }
  }
  
  /// <summary>
  /// Called when the purchase worker button is clicked
  /// </summary>
  private void OnPurchaseWorkerButtonClicked() {
    DenSystemManager.Instance.CurrentDenAdministrator.PurchaseWorker();
    UpdateCurrentDenWorkers();
    UpdatePurchaseWorkerButton();
  }
  
  public void UpdatePurchaseWorkerButton() {
    if (purchaseWorkerButton == null) {
      return;
    }
    
    // Get the current dynamic worker price
    int currentWorkerPrice = WorkerManager.Instance.CurrentWorkerPrice;
    
    // Update button text with dynamic cost using {0} placeholder
    TextMeshProUGUI buttonText = purchaseWorkerButton.GetComponentInChildren<TextMeshProUGUI>();
    if (buttonText != null) {
      string formattedText = purchaseWorkerButtonText.Replace("\\n", "\n").Replace("{0}", currentWorkerPrice.ToString());
      buttonText.text = formattedText;
    }
    
    // Only check if player can afford - allow breeding even if den is full (workers go to unassigned pool)
    // JUST KIDDING! Do NOT allow if den is full
    bool canAfford = DenSystemManager.Instance.FoodInDen >= currentWorkerPrice;
    bool hasRoom = WorkerManager.Instance.HaveRoomToCreateWorker();
    if (!hasRoom) {
      buttonText.text = denFullButtonText.Replace("\\n", "\n");
    }
    purchaseWorkerButton.interactable = canAfford && hasRoom;
  }

  /// <summary>
  /// Shakes the den inventory panel to indicate that items cannot be collected.
  /// Refreshes the GUI first to ensure we're shaking the current slots.
  /// </summary>
  public void ShakeInventoryPanel() {
    if (inventoryPanel != null) {
      // Refresh GUI first to ensure we have the current slots -- workaround because it would delete it after I shook it :(
      inventoryPanel.RefreshGui();
      // Then shake the slots (use a coroutine to ensure RefreshGui completes first)
      StartCoroutine(ShakeInventoryPanelCoroutine());
    }
  }
  
  /// <summary>
  /// Coroutine that shakes the inventory panel after ensuring the GUI is refreshed.
  /// </summary>
  private System.Collections.IEnumerator ShakeInventoryPanelCoroutine() {
    // Wait one frame to ensure RefreshGui has completed and slots are created
    yield return null;
    
    if (inventoryPanel != null) {
      inventoryPanel.Shake();
    }
  }
}