using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleClock : MonoBehaviour {
  public float halfPeriodSeconds = 1f;
  public event Action ToggleOn;
  public event Action ToggleOff;

  private void Start() {
    ToggleOn += OnToggleOn;
    ToggleOff += OnToggleOff;
    ToggleOn?.Invoke();
  }

  private void OnToggleOn() {
    StartCoroutine(OnToggleOnCoroutine());
  }

  private void OnToggleOff() {
    StartCoroutine(OnToggleOffCoroutine());
  }

  private IEnumerator OnToggleOnCoroutine() {
    yield return new WaitForSeconds(halfPeriodSeconds);
    ToggleOff?.Invoke();
  }
  
  private IEnumerator OnToggleOffCoroutine() {
    yield return new WaitForSeconds(halfPeriodSeconds);
    ToggleOn?.Invoke();
  }
  
}