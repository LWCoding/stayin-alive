using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static KnowledgeManager;

/// <summary>
/// Represents a single knowledge item in the knowledge menu.
/// Displays a piece of information the player knows.
/// </summary>
public class KnowledgeItem : MonoBehaviour
{
    /// <summary>
    /// Basic struct containing sprite, name, and description for a knowledge entry.
    /// </summary>
    public struct KnowledgeData
    {
        public Sprite sprite;
        public string name;
        public string description;

        public KnowledgeData(Sprite sprite, string name, string description)
        {
            this.sprite = sprite;
            this.name = name;
            this.description = description;
        }
    }

    [SerializeField]
    private Image spriteImage;

    [SerializeField]
    private TextMeshProUGUI nameText;

    [SerializeField]
    private TextMeshProUGUI descriptionText;
    
    private string _title;
    
    private Color known = Color.white;
    private Color unknown = Color.black;

    private const string unknownName = "?????";
    private const string unknownDescription = "<i>I haven't encountered this yet. I need to keep exploring!</i>";
    /// <summary>
    /// Initializes the knowledge item with the given knowledge data.
    /// </summary>
    /// <param name="knowledgeData">The knowledge data containing sprite, name, and description</param>
    public void Initialize(KnowledgeData knowledgeData)
    {
        if (spriteImage != null)
        {
            spriteImage.sprite = knowledgeData.sprite;
            spriteImage.enabled = knowledgeData.sprite != null;
        }
        else
        {
            Debug.LogWarning("KnowledgeItem: Sprite image component is not assigned!");
        }

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
    
    public bool Initialize(string title)
    {
      _title = title;

      return Refresh();
    }

    public bool Refresh() {
      KnowledgeOperationResult result = KnowledgeManager.Instance.IsKnowledgeLearned(_title);

      switch (result) {
        case KnowledgeOperationResult.UNEXPECTED_ERROR:
        case KnowledgeOperationResult.KNOWLEDGE_DOES_NOT_EXIST:
          Destroy(gameObject);
          return false;
        case KnowledgeOperationResult.KNOWLEDGE_UNKNOWN:
          var knowledgeData = KnowledgeManager.Instance.GetKnowledgeDataByTitle(_title);
          spriteImage.sprite = knowledgeData.sprite;
          spriteImage.color = unknown;
          nameText.text = unknownName;
          descriptionText.text = unknownDescription;
          break;
        case KnowledgeOperationResult.KNOWLEDGE_LEARNED:
          var learnedKnowledgeData = KnowledgeManager.Instance.GetKnowledgeDataByTitle(_title);
          spriteImage.sprite = learnedKnowledgeData.sprite;
          spriteImage.color = known;
          nameText.text = learnedKnowledgeData.title;
          descriptionText.text = learnedKnowledgeData.description;
          break;
      }
      
      return true;
    }
}
