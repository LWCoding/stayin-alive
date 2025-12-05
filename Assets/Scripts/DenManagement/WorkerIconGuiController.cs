using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorkerIconGuiController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
  [SerializeField]
  private Image spriteRenderer;
  
  [SerializeField]
  private TextMeshProUGUI arrowText;
  
  [SerializeField]
  private Button button;
  
  [SerializeField]
  private Sprite workerSprite;

  [SerializeField]
  private Color defaultColor;
  
  [SerializeField]
  private Color mouseOverColor;

  private string currentDenText = "↓";
  private string unassignedDenText = "↑";
  
  private Animal animal;

  public void InitializeWorkerIcon(Animal animal) {
    this.animal = animal;
    TextInit();
    spriteRenderer.sprite = workerSprite;
  }

  private void TextInit() {
    arrowText.color = Color.clear;
    if (WorkerManager.Instance.WorkersToDens[animal] == WorkerManager.UNASSIGNED_DEN_ID) {
      arrowText.text = unassignedDenText;
      return;
    }
    arrowText.text = currentDenText;
  }
  
  public void OnPointerEnter(PointerEventData pointerEventData) {
    spriteRenderer.color = mouseOverColor;
    arrowText.color = Color.white;
  }

  public void OnPointerExit(PointerEventData pointerEventData) {
    spriteRenderer.color = defaultColor;
    arrowText.color = Color.clear;
  }

  public void TransferWorker() {
    if (arrowText.text == unassignedDenText) {
      WorkerManager.WorkerOperationResult result = WorkerManager.Instance.Assign(animal, DenSystemManager.Instance.CurrentAdminDen.GetDenInfo().denId);
      if (result != WorkerManager.WorkerOperationResult.WORKER_ASSIGNED) {
        // Spawn FadeText message if assignment failed (likely due to not enough space)
        if (ParticleManager.Instance != null) {
          RectTransform workerIconRect = GetComponent<RectTransform>();
          if (workerIconRect != null) {
            ParticleManager.Instance.SpawnFadeTextAtRectTransform("Not enough space in den", workerIconRect);
          }
        }
      }
      InitializeWorkerIcon(animal);
    }

    else if (arrowText.text == currentDenText) {
      WorkerManager.WorkerOperationResult result = WorkerManager.Instance.Unassign(animal);
      if (result != WorkerManager.WorkerOperationResult.WORKER_UNASSIGNED) {
        if (ParticleManager.Instance != null) {
          RectTransform workerIconRect = GetComponent<RectTransform>();
          if (workerIconRect != null) {
            ParticleManager.Instance.SpawnFadeTextAtRectTransform("You have too many followers", workerIconRect);
          }
        }
      }
      InitializeWorkerIcon(animal);
    }
    
    DenSystemManager.Instance.DenAdminMenu.UpdateCurrentDenWorkers();
    DenSystemManager.Instance.DenAdminMenu.UpdatePurchaseWorkerButton();
  }


}