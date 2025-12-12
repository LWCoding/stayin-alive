using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enforce16x9Scale : MonoBehaviour {
  private RectTransform rectTransform;
  private Vector3 prevDims;
  Vector3[] corners = new Vector3[4];
  private float targetAspect = 16f / 9f;
  private int prevScreenWidth;
  private int prevScreenHeight;

  private void Start() {
    rectTransform = GetComponent<RectTransform>();
    FixAspectRatio();
    rectTransform.anchoredPosition = Vector3.zero;
  }

  private void FixAspectRatio() {
    rectTransform.GetWorldCorners(corners);
    Vector3 dims = corners[2] - corners[0];
    float screenWidth = Screen.width;
    float screenHeight = Screen.height;
    float width = dims.x;
    float height = dims.y;
    float newHeight = 9f * screenWidth / 16f;
    float newWidth = 16f * screenHeight / 9f;
    float currentAspectRatio = width / height;
    float currentScreenAspect = screenWidth / screenHeight;
    if (currentScreenAspect > targetAspect) {
      transform.localScale = new Vector3(newWidth/width * transform.localScale.x, screenHeight/height *transform.localScale.y, 1f);
      prevScreenWidth = Screen.width;
      prevScreenHeight = Screen.height;
      return;
    }
    transform.localScale = new Vector3(screenWidth/width * transform.localScale.x, newHeight/height * transform.localScale.y, 1f);
    prevScreenWidth = Screen.width;
    prevScreenHeight = Screen.height;
  }
  
  private void Update() {
    if (prevScreenWidth != Screen.width || prevScreenHeight != Screen.height) {
      FixAspectRatio();
    } else if (!rectTransform.rect.width.Equals(Screen.width) && !rectTransform.rect.height.Equals(Screen.height))
    {
      FixAspectRatio();
    }
  }
}