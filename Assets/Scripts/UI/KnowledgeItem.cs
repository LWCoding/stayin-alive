using TMPro;
using UnityEngine;

/// <summary>
/// Represents a single knowledge item in the knowledge menu.
/// Displays a piece of information the player knows.
/// </summary>
public class KnowledgeItem : MonoBehaviour
{
    /// <summary>
    /// Basic struct containing name and description for a knowledge entry.
    /// </summary>
    public struct KnowledgeData
    {
        public string name;
        public string description;

        public KnowledgeData(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }

    [SerializeField]
    private TextMeshProUGUI nameText;

    [SerializeField]
    private TextMeshProUGUI descriptionText;

    /// <summary>
    /// Initializes the knowledge item with the given knowledge data.
    /// </summary>
    /// <param name="knowledgeData">The knowledge data containing name and description</param>
    public void Initialize(KnowledgeData knowledgeData)
    {
        if (nameText != null)
        {
            nameText.text = knowledgeData.name;
        }
        else
        {
            Debug.LogWarning("KnowledgeItem: Name text component is not assigned!");
        }

        if (descriptionText != null)
        {
            descriptionText.text = knowledgeData.description;
        }
        else
        {
            Debug.LogWarning("KnowledgeItem: Description text component is not assigned!");
        }
    }
}
