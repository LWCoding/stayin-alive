using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableKnowledgeGranterFactory : Singleton<InteractableKnowledgeGranterFactory> {
  public GameObject granterPrefab;

  public void CreateInteractableKnowledgeGranter(GameObject parent) {
    Instantiate(granterPrefab, parent.transform);
  }
}

