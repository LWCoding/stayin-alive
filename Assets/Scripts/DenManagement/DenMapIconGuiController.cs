using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DenMapIconGuiController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
  [SerializeField]
  private Image icon;

  [SerializeField]
  private Sprite emptyDenSprite;

  [SerializeField]
  private Sprite inhabitedDenSprite;

  [SerializeField]
  private Button denIconButton;
  
  private DenSystemManager.DenInformation denInfo;

  public void InitializeDenMapIcon(DenSystemManager.DenInformation denInfo) {
    this.denInfo = denInfo;
    Debug.LogWarning(denInfo.denId);
    SetDenImage();
    SetPosition();
  }

  private void SetDenImage() {
    if (denInfo.denId == DenSystemManager.Instance.CurrentAdminDen?.GetDenInfo().denId) {
      icon.sprite = inhabitedDenSprite;
      return;
    }
    icon.sprite = emptyDenSprite;
  }

  private void SetPosition() {
    Vector2 gameGridDimensions = EnvironmentManager.Instance.GridSize;
    Vector2 mapPanelDimensions = DenSystemManager.Instance.DenAdminMenu.mapPanelDimensions;
    Vector2 barycentricDenCoords = new Vector2(
      denInfo.denObject.GridPosition.x/(gameGridDimensions.x/2),  
      denInfo.denObject.GridPosition.y/(gameGridDimensions.y/2)
    );
    Debug.LogWarning(barycentricDenCoords);
    Vector3 mapPanelDenCoords = new Vector3(
      barycentricDenCoords.x * (mapPanelDimensions.x/2), barycentricDenCoords.y * (mapPanelDimensions.y/2), transform.position.z
    );
    transform.localPosition = mapPanelDenCoords;
  }

  public void TeleportPlayerToDen() {
    DenSystemManager.Instance.CurrentDenAdministrator.DenTeleport(denInfo.denId);
  }
  
  public void OnPointerEnter(PointerEventData pointerEventData) {
    icon.color = Color.gray;
  }

  public void OnPointerExit(PointerEventData pointerEventData) {
    icon.color = Color.white;
  }
}
