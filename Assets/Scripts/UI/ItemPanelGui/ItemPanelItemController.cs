using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ItemPanelItemController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
  [SerializeField]
  private Image itemIcon;

  [SerializeField]
  private Image itemBackground;
  
  [SerializeField]
  private Button transferToPlayer;
  
  private GlobalInventoryManager.InventoryItem inventoryItemRepresented;
  public GlobalInventoryManager.InventoryItem InventoryItemRepresented => inventoryItemRepresented;
  
  private Color itemBackgroundDefaultColor = Color.white;
  private Color itemBackgroundHoverColor = Color.yellow;

  private void Start() {
    itemBackground.color = itemBackgroundDefaultColor;
    
    //Parent
    RectTransform parent = transform.parent.GetComponent<RectTransform>();
    
    //Bump X Position a bit randomly Slightly so things don't stack perfectly vertically
    float randomBounds = (parent.rect.width * 0.7f)/2f;
    
    transform.position += new Vector3(Random.Range(-randomBounds, randomBounds), 0, 0);
    
    transferToPlayer.onClick.AddListener(TransferToPlayer);
  }

  public void OnPointerEnter(PointerEventData eventData) {
    itemBackground.color = itemBackgroundHoverColor;
  }

  public void OnPointerExit(PointerEventData eventData) {
    itemBackground.color = itemBackgroundDefaultColor;
  }

  public void Setup(GlobalInventoryManager.InventoryItem inventoryItem) {
    inventoryItemRepresented = inventoryItem;
    Sprite itemSprite = ItemManager.Instance.GetItemSprite(inventoryItemRepresented.id);
    itemIcon.sprite = itemSprite;
  }

  public void TransferToPlayer() {
    // DenSystemManager.Instance.TransferItemToPlayer(itemRepresented);
    GlobalInventoryManager.Instance.TransferItemToPlayer(inventoryItemRepresented);
  }
  
  
}