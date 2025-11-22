using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogEntryGuiController : MonoBehaviour {
  public enum DenLogType {
    ADD_FOOD,
    TAKE_FOOD,
    WORKER_STARVE,
    WORKER_EATEN
  }

  private static readonly string addFoodLog = "deposited {0} food in the den";
  private static readonly string takeFoodLog = "ate 1 food from the den";
  private static readonly string workerStarveLog = "died of starvation";
  private static readonly string workerEatenLog = "was eaten";
  
  private static readonly Color goodColor = Color.white;
  private static readonly Color badColor = Color.red;

  private static readonly float logLifetime = 4f;
  private static readonly int numAlphaSteps = 20;

  public static string GetLogString(DenLogType logType) {
    switch (logType) {
      case DenLogType.ADD_FOOD:
        return addFoodLog;
      case DenLogType.TAKE_FOOD:
        return takeFoodLog;
      case DenLogType.WORKER_STARVE:
        return workerStarveLog;
      case DenLogType.WORKER_EATEN:
        return workerEatenLog;
      default:
        return "";
    }
  }
  
  public static Color GetLogColor(DenLogType logType) {
    switch (logType) {
      case DenLogType.ADD_FOOD:
        return goodColor;
      case DenLogType.TAKE_FOOD:
        return goodColor;
      case DenLogType.WORKER_STARVE:
        return badColor;
      case DenLogType.WORKER_EATEN:
        return badColor;
      default:
        return goodColor;
    }
  }
  
  [Header("UI Components")]
  [SerializeField]
  private Image logIcon;
  
  [SerializeField]
  private CanvasGroup visibilityController;
  
  [SerializeField]
  private TextMeshProUGUI logText;
  
  public void Setup(DenLogType logType) {
    logText.text =  GetLogString(logType);
    logText.color = GetLogColor(logType);
    StartCoroutine(WaitThenKillSelf());
  }
  
  public void Setup(DenLogType logType, int num) {
    logText.text =  string.Format(GetLogString(logType), num);
    logText.color = GetLogColor(logType);
    StartCoroutine(WaitThenKillSelf());
  }

  private IEnumerator WaitThenKillSelf() {
    float logHalfLife = logLifetime / 2f;
    yield return new WaitForSeconds(logHalfLife);
    for (int i = 0; i < numAlphaSteps; i++) {
      yield return new WaitForSeconds(logHalfLife/numAlphaSteps);
      visibilityController.alpha = (numAlphaSteps-(float)i)/numAlphaSteps;
    }
    Destroy(gameObject);
  } 
}
