using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DenSystemManager : Singleton<DenSystemManager> {
  public int denPrice;

  public List<Den> getDens() {
    return InteractableManager.Instance.GetAllDens();
  }
}
