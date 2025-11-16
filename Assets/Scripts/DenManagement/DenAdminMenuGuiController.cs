using System;
using System.Collections;
using System.Collections.Generic;
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


  [Header("Prefabs")]
  [SerializeField]
  private GameObject denMapIconPrefab;
  
  private List<DenSystemManager.DenInformation> mapDenInfo;
  
  private List<DenMapIconGuiController> mapDenMapIcons;

  public void Start() {
    mapDenMapIcons = new List<DenMapIconGuiController>();
  }

  public void Show() {
    visibilityController.alpha = 1;
    visibilityController.interactable = true;
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
  
}