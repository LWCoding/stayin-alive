using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogHolderGuiController : MonoBehaviour {
  [Header("UI Elements")]
  [SerializeField]
  private VerticalLayoutGroup logHolder;

  [Header("Prefabs")]
  [SerializeField]
  private GameObject logEntryPrefab;

  public void SpawnLog(LogEntryGuiController.DenLogType logType) {
    GameObject logEntryObj = Instantiate(logEntryPrefab, logHolder.transform);
    LogEntryGuiController logEntryGuiController = logEntryObj.GetComponent<LogEntryGuiController>();
    logEntryGuiController.Setup(logType);
  }
}