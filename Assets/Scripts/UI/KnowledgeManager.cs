using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages knowledge data loaded from ScriptableObjects.
/// Loads all KnowledgeData ScriptableObjects from Resources/Knowledge/ folder.
/// </summary>
public class KnowledgeManager : Singleton<KnowledgeManager>
{
    private List<KnowledgeData> _allKnowledgeData = new List<KnowledgeData>();
    private Dictionary<string, KnowledgeData> _knowledgeDataDictionary = new Dictionary<string, KnowledgeData>();

    /// <summary>
    /// Gets all loaded knowledge data.
    /// </summary>
    public List<KnowledgeData> AllKnowledgeData => new List<KnowledgeData>(_allKnowledgeData);

    /// <summary>
    /// Gets knowledge data by title/name.
    /// </summary>
    /// <param name="title">The title of the knowledge entry</param>
    /// <returns>The KnowledgeData if found, null otherwise</returns>
    public KnowledgeData GetKnowledgeDataByTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        _knowledgeDataDictionary.TryGetValue(title, out KnowledgeData data);
        return data;
    }

    /// <summary>
    /// Checks if a knowledge entry exists with the given title.
    /// </summary>
    /// <param name="title">The title to check</param>
    /// <returns>True if the knowledge entry exists</returns>
    public bool HasKnowledge(string title)
    {
        return !string.IsNullOrEmpty(title) && _knowledgeDataDictionary.ContainsKey(title);
    }

    protected override void Awake()
    {
        base.Awake();
        
        // Load all KnowledgeData from Resources/Knowledge/ folder
        LoadKnowledgeData();
    }

    /// <summary>
    /// Loads all KnowledgeData ScriptableObjects from the Resources/Knowledge/ folder.
    /// </summary>
    private void LoadKnowledgeData()
    {
        KnowledgeData[] knowledgeDataArray = Resources.LoadAll<KnowledgeData>("Knowledge");
        
        if (knowledgeDataArray == null || knowledgeDataArray.Length == 0)
        {
            Debug.LogWarning("KnowledgeManager: No KnowledgeData found in Resources/Knowledge/ folder! Please create KnowledgeData ScriptableObjects and place them in Resources/Knowledge/.");
            return;
        }

        _allKnowledgeData.Clear();
        _knowledgeDataDictionary.Clear();

        foreach (KnowledgeData knowledgeData in knowledgeDataArray)
        {
            if (knowledgeData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(knowledgeData.title))
            {
                Debug.LogWarning($"KnowledgeManager: Found KnowledgeData with null or empty title: {knowledgeData.name}");
                continue;
            }

            if (_knowledgeDataDictionary.ContainsKey(knowledgeData.title))
            {
                Debug.LogWarning($"KnowledgeManager: Duplicate knowledge title found: {knowledgeData.title}. Skipping duplicate.");
                continue;
            }

            _allKnowledgeData.Add(knowledgeData);
            _knowledgeDataDictionary[knowledgeData.title] = knowledgeData;
        }

        Debug.Log($"KnowledgeManager: Loaded {_allKnowledgeData.Count} knowledge entries from Resources/Knowledge/.");
    }
}
