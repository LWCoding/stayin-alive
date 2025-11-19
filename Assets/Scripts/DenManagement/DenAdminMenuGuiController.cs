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

  public void Start() {
    mapDenMapIcons = new List<DenMapIconGuiController>();
    workerIcons = new List<WorkerIconGuiController>();
    
    // Setup button listener
    if (purchaseWorkerButton != null) {
      purchaseWorkerButton.onClick.AddListener(OnPurchaseWorkerButtonClicked);
    }
  }

  public void Show() {
    visibilityController.alpha = 1;
    visibilityController.interactable = true;
    SetupCurrentDenWorkers();
    UpdatePurchaseWorkerButton();
  }

  public void Hide() {
    visibilityController.alpha = 0;
    visibilityController.interactable = false;
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

  public void SetupCurrentDenRenderTexture() {
    currentDenRender.texture = DenSystemManager.Instance.CurrentAdminDen.DenRenderTexture;
  }

  public void SetupCurrentDenWorkers() {
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
    SetupCurrentDenWorkers();
    UpdatePurchaseWorkerButton();
  }
  
  public void UpdatePurchaseWorkerButton() {
    if (purchaseWorkerButton == null) {
      return;
    }
    
    Den currentDen = DenSystemManager.Instance.CurrentAdminDen;
    bool denIsFull = currentDen != null && currentDen.IsFull();
    
    TextMeshProUGUI buttonText = purchaseWorkerButton.GetComponentInChildren<TextMeshProUGUI>();
    if (buttonText != null) {
      string displayText = denIsFull ? denFullButtonText : purchaseWorkerButtonText;
      buttonText.text = displayText.Replace("\\n", "\n");
    }
    
    bool canAfford = PointsManager.Instance.ReadinessPoints >= DenSystemManager.Instance.workerPrice;
    purchaseWorkerButton.interactable = canAfford && !denIsFull;
  }
}