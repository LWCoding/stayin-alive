using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableKnowledgeGranter : MonoBehaviour {
  private Interactable interactable;

  private void Start() {
    interactable = GetComponentInParent<Interactable>();
  }

  private void OnTriggerEnter2D(Collider2D other) {
    if (interactable == null) {
      interactable = GetComponentInParent<Interactable>();
    }
    if (other.gameObject.CompareTag("Player")) {
      KnowledgeManager.Instance.LearnKnowledgeData(interactable.GetKnowledgeTitle());
    }
  }
}