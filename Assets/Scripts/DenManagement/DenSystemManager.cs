using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class DenSystemManager : Singleton<DenSystemManager> {
  public struct DenInformation {
    public int denId;
    public int denPopulation;
    public Den denObject;
  }
  
  public static DenInformation ConstructDenInformation(Den den) {
    int denId = den.GridPosition.x * 10000 + den.GridPosition.y;
    int denPopulation = den.NumberAnimalsInDen();
    return new DenInformation { denId = denId, denPopulation = denPopulation, denObject = den };
  }
  
  public int denPrice;

  public List<Den> GetDenList() {
    return InteractableManager.Instance.GetAllDens();
  }
  
  public List<DenInformation> GetDens() {
    List<Den> denList = GetDenList();
    List<DenInformation> denInfos = new List<DenInformation>();
    foreach (Den den in denList) {
      denInfos.Add(ConstructDenInformation(den));
    }
    return denInfos;
  }
}