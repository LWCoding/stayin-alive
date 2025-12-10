using UnityEngine;

/// <summary>
/// ScriptableObject that stores data for a knowledge entry.
/// Contains sprite, title, and description for display in the knowledge menu.
/// </summary>
[CreateAssetMenu(fileName = "New Knowledge Data", menuName = "Knowledge/Knowledge Data")]
public class KnowledgeData : ScriptableObject
{
    [Header("Knowledge Info")]
    [Tooltip("Image/sprite representing this knowledge entry")]
    public Sprite sprite;

    [Tooltip("Title/name of this knowledge entry")]
    public string title;

    [Tooltip("Description text explaining this knowledge")]
    [TextArea(3, 10)]
    public string description;
    
    public KnowledgeManager.KnowledgeCategory knowledgeCategory;
}
