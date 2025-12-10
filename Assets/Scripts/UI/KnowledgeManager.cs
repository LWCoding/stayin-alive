using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages knowledge data loaded from ScriptableObjects.
/// Loads all KnowledgeData ScriptableObjects from Resources/Knowledge/ folder.
/// </summary>
public class KnowledgeManager : Singleton<KnowledgeManager> {
  public enum KnowledgeOperationResult {
    UNEXPECTED_ERROR,
    KNOWLEDGE_DOES_NOT_EXIST,
    KNOWLEDGE_UNKNOWN,
    KNOWLEDGE_LEARNED,
    KNOWLEDGE_ALREADY_LEARNED
  }

  public event Action<bool> OnNewKnowledgeFlagChange;

  public void InvokeOnNewKnowledgeFlagChange(bool flag) {
    OnNewKnowledgeFlagChange?.Invoke(flag);
  }

  private List<KnowledgeData> _allKnowledgeData = new List<KnowledgeData>();
  private Dictionary<string, KnowledgeData> _allknowledgeDataDictionary = new Dictionary<string, KnowledgeData>();

  private Dictionary<string, KnowledgeData> _learnedKnowledgeDataDictionary = new Dictionary<string, KnowledgeData>();

  /// <summary>
  /// Gets all loaded knowledge data.
  /// </summary>
  public List<KnowledgeData> AllKnowledgeData => new List<KnowledgeData>(_allKnowledgeData);

  /// <summary>
  /// Gets knowledge data by title/name.
  /// </summary>
  /// <param name="title">The title of the knowledge entry</param>
  /// <returns>The KnowledgeData if found, null otherwise</returns>
  public KnowledgeData GetKnowledgeDataByTitle(string title) {
    if (string.IsNullOrEmpty(title)) {
      return null;
    }

    _allknowledgeDataDictionary.TryGetValue(title, out KnowledgeData data);
    return data;
  }

  /// <summary>
  /// Checks if a knowledge entry exists with the given title.
  /// </summary>
  /// <param name="title">The title to check</param>
  /// <returns>True if the knowledge entry exists</returns>
  public bool KnowledgeExists(string title) {
    return !string.IsNullOrEmpty(title) && _allknowledgeDataDictionary.ContainsKey(title);
  }

  /// <summary>
  /// Checks if knowledge is learned by the player
  /// </summary>
  /// <param name="title">The title of the knowledge to be checked</param>
  /// <returns>
  /// <see cref="KnowledgeOperationResult.KNOWLEDGE_DOES_NOT_EXIST">KNOWLEDGE_DOES_NOT_EXIST</see><br/>
  /// <see cref="KnowledgeOperationResult.KNOWLEDGE_UNKNOWN">KNOWLEDGE_UNKNOWN</see> - Player hasn't learned yet <br/>
  /// <see cref="KnowledgeOperationResult.KNOWLEDGE_LEARNED">KNOWLEDGE_LEARNED</see> <br/>
  /// <see cref="KnowledgeOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see>
  /// </returns>
  public KnowledgeOperationResult IsKnowledgeLearned(string title) {
    if (string.IsNullOrEmpty(title)) {
      return KnowledgeOperationResult.UNEXPECTED_ERROR;
    }

    if (!KnowledgeExists(title)) {
      return KnowledgeOperationResult.KNOWLEDGE_DOES_NOT_EXIST;
    }

    return _learnedKnowledgeDataDictionary.ContainsKey(title)
      ? KnowledgeOperationResult.KNOWLEDGE_LEARNED
      : KnowledgeOperationResult.KNOWLEDGE_UNKNOWN;
  }

  /// <summary>
  /// Try to learn the specified <see cref="KnowledgeData"/>
  /// </summary>
  /// <param name="title">Title of the <see cref="KnowledgeData"/> to be learned</param>
  /// <returns>
  /// <see cref="KnowledgeOperationResult.KNOWLEDGE_LEARNED">KNOWLEDGE_LEARNED</see><br/>
  /// <see cref="KnowledgeOperationResult.KNOWLEDGE_ALREADY_LEARNED">KNOWLEDGE_ALREADY_LEARNED</see><br/>
  /// <see cref="KnowledgeOperationResult.KNOWLEDGE_DOES_NOT_EXIST">KNOWLEDGE_DOES_NOT_EXIST</see><br/>
  /// <see cref="KnowledgeOperationResult.UNEXPECTED_ERROR">UNEXPECTED_ERROR</see>
  /// </returns>
  public KnowledgeOperationResult LearnKnowledgeData(string title) {
    if (string.IsNullOrEmpty(title)) {
      return KnowledgeOperationResult.UNEXPECTED_ERROR;
    }

    var result = IsKnowledgeLearned(title);

    switch (result) {
      case KnowledgeOperationResult.KNOWLEDGE_DOES_NOT_EXIST:
        return KnowledgeOperationResult.KNOWLEDGE_DOES_NOT_EXIST;

      case KnowledgeOperationResult.KNOWLEDGE_UNKNOWN:
        KnowledgeData data = GetKnowledgeDataByTitle(title);
        _learnedKnowledgeDataDictionary[title] = data;
        OnNewKnowledgeFlagChange?.Invoke(true);
        // Don't play sound during tutorial
        if (TutorialManager.Instance == null)
        {
          AudioManager.Instance?.PlaySFX(AudioManager.SFXType.NewKnowledgeDiscovered);
        }
        return KnowledgeOperationResult.KNOWLEDGE_LEARNED;

      case KnowledgeOperationResult.KNOWLEDGE_LEARNED:
        return KnowledgeOperationResult.KNOWLEDGE_ALREADY_LEARNED;
    }

    return KnowledgeOperationResult.UNEXPECTED_ERROR;
  }

  // Void signature to pass into event
  public void LearnKnowledgeDataVoid(string title) {
    LearnKnowledgeData(title);
  }

  protected override void Awake() {
    base.Awake();

    // Load all KnowledgeData from Resources/Knowledge/ folder
    LoadKnowledgeData();
  }

  private void Start() {
    InventoryManager.Instance.OnItemAdded += LearnKnowledgeDataVoid;
  }

  /// <summary>
  /// Loads all KnowledgeData ScriptableObjects from the Resources/Knowledge/ folder.
  /// </summary>
  private void LoadKnowledgeData() {
    KnowledgeData[] knowledgeDataArray = Resources.LoadAll<KnowledgeData>("Knowledge");

    if (knowledgeDataArray == null || knowledgeDataArray.Length == 0) {
      Debug.LogWarning(
        "KnowledgeManager: No KnowledgeData found in Resources/Knowledge/ folder! Please create KnowledgeData ScriptableObjects and place them in Resources/Knowledge/.");
      return;
    }

    _allKnowledgeData.Clear();
    _allknowledgeDataDictionary.Clear();

    foreach (KnowledgeData knowledgeData in knowledgeDataArray) {
      if (knowledgeData == null) {
        continue;
      }

      if (string.IsNullOrEmpty(knowledgeData.title)) {
        Debug.LogWarning($"KnowledgeManager: Found KnowledgeData with null or empty title: {knowledgeData.name}");
        continue;
      }

      if (_allknowledgeDataDictionary.ContainsKey(knowledgeData.title)) {
        Debug.LogWarning(
          $"KnowledgeManager: Duplicate knowledge title found: {knowledgeData.title}. Skipping duplicate.");
        continue;
      }

      _allKnowledgeData.Add(knowledgeData);
      _allknowledgeDataDictionary[knowledgeData.title] = knowledgeData;
    }

    Debug.Log($"KnowledgeManager: Loaded {_allKnowledgeData.Count} knowledge entries from Resources/Knowledge/.");
  }
}