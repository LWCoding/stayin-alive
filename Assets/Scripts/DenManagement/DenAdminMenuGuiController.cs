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
    visibilityController.alpha = 1;
    visibilityController.interactable = true;
    UpdateGui();
  }

  public void Hide() {
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
    foreach (Animal worker in DenSystemManager.Instance.WorkersToDens.Keys.ToList()) {
      RectTransform workerIconParent = unassignedWorkers;
      if (DenSystemManager.Instance.WorkersToDens[worker] ==
          DenSystemManager.Instance.CurrentAdminDen.GetDenInfo().denId) {
        workerIconParent = currentDenWorkers;
      }
      else if (DenSystemManager.Instance.WorkersToDens[worker] !=
          DenSystemManager.Instance.UNASSIGNED_DEN_ID) {
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
    int currentWorkerPrice = DenSystemManager.Instance.GetCurrentWorkerPrice();
    
    // Update button text with dynamic cost using {0} placeholder
    TextMeshProUGUI buttonText = purchaseWorkerButton.GetComponentInChildren<TextMeshProUGUI>();
    if (buttonText != null) {
      string formattedText = purchaseWorkerButtonText.Replace("\\n", "\n").Replace("{0}", currentWorkerPrice.ToString());
      buttonText.text = formattedText;
    }
    
    // Only check if player can afford - allow breeding even if den is full (workers go to unassigned pool)
    bool canAfford = DenSystemManager.Instance.FoodInDen >= currentWorkerPrice;
    purchaseWorkerButton.interactable = canAfford;
  }
}