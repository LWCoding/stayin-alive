using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogEntryGuiController : MonoBehaviour {
  public enum DenLogType {
    ADD_FOOD,
    ADD_OTHER_ITEM,
    ADD_FOOD_AND_OTHER_ITEM,
    TAKE_FOOD,
    WORKER_STARVE,
    WORKER_EATEN
  }

  private static readonly List<DenLogType> pluralLogTypes = new List<DenLogType> { DenLogType.ADD_OTHER_ITEM, DenLogType.ADD_FOOD_AND_OTHER_ITEM };
  
  private static readonly string addFoodLog = "deposited {0} food";
  private static readonly string addItemLog = "deposited {0} item{1}";
  private static readonly string addFoodAndItemLog = "deposited {0} food and {1} item{2}";
  private static readonly string takeFoodLog = "ate 1 food from the den";
  private static readonly string workerStarveLog = "died of starvation";
  private static readonly string workerEatenLog = "was eaten";
  
  private static readonly Color goodColor = Color.white;
  private static readonly Color badColor = Color.red;

  private static readonly float logLifetime = 4f;
  private static readonly int numAlphaSteps = 20;

  private static string GetLogString(DenLogType logType) {
    switch (logType) {
      case DenLogType.ADD_FOOD:
        return addFoodLog;
      case DenLogType.ADD_OTHER_ITEM:
        return addItemLog;
      case DenLogType.ADD_FOOD_AND_OTHER_ITEM:
        return addFoodAndItemLog;
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

  private static bool GetPluralLog(DenLogType logType) {
    return pluralLogTypes.Contains(logType);
  }
  
  private static Color GetLogColor(DenLogType logType) {
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

  private static string PluralSuffix(int num) {
    return num > 1 ? "s" : "";
  }
  
  public void Setup(DenLogType logType, int num) {
    if (GetPluralLog(logType)) {
      logText.text =  string.Format(GetLogString(logType), num, PluralSuffix(num));
    }
    else {
      logText.text =  string.Format(GetLogString(logType), num);
    }
    logText.color = GetLogColor(logType);
    StartCoroutine(WaitThenKillSelf());
  }
  
  // Currently only supports plural for num2 cause there's only one case that happens at the moment, if we keep working on this for some godforsaken reason, I'll fix
  public void Setup(DenLogType logType, int num1, int num2) {
    if (GetPluralLog(logType)) {
      logText.text =  string.Format(GetLogString(logType), num1, num2, PluralSuffix(num2));
    }
    else {
      logText.text =  string.Format(GetLogString(logType), num1, num2);
    }
    logText.color = GetLogColor(logType);
    StartCoroutine(WaitThenKillSelf());
  }

  private IEnumerator WaitThenKillSelf() {
    float logHalfLife = logLifetime / 2f;
    if (logText.color == badColor)
    {
      StartCoroutine(Shaker.ShakeCoroutine(transform, _shakeDuration: logHalfLife));
    }
    yield return new WaitForSeconds(logHalfLife);
    for (int i = 0; i < numAlphaSteps; i++) {
      yield return new WaitForSeconds(logHalfLife/numAlphaSteps);
      visibilityController.alpha = (numAlphaSteps-(float)i)/numAlphaSteps;
    }
    Destroy(gameObject);
  } 
}
