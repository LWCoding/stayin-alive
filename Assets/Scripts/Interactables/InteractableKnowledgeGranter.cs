using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableOrAnimalKnowledgeGranter : MonoBehaviour {
  private Interactable interactable;
  private Animal animal;

  private void Start() {
    interactable = GetComponentInParent<Interactable>();
    animal = GetComponentInParent<Animal>();
  }

  private void OnTriggerEnter2D(Collider2D other) {
    if (interactable == null && animal == null) {
      interactable = GetComponentInParent<Interactable>();
      if (interactable == null)
      {
        animal = GetComponentInParent<Animal>();
      }
    }
    if (other.gameObject.CompareTag("Player")) {
      if (interactable != null)
      {
        KnowledgeManager.Instance.LearnKnowledgeData(interactable.GetKnowledgeTitle(), transform.position);
      }

      if (animal != null)
      {
        KnowledgeManager.Instance.LearnKnowledgeData(animal.AnimalData.KnowledgeTitle, transform.position);
      }
      
    }
  }
}