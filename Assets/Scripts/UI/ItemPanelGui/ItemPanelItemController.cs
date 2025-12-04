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

  private Item itemRepresented;
  public Item ItemRepresented => itemRepresented;
  
  private Color itemBackgroundDefaultColor = Color.white;
  private Color itemBackgroundHoverColor = Color.yellow;

  private void Start() {
    itemBackground.color = itemBackgroundDefaultColor;
    
    //Bump X Position a bit randomly Slightly so things don't stack perfectly vertically
    transform.position += new Vector3(Random.Range(-5f, 5f), 0, 0);
    
    transferToPlayer.onClick.AddListener(TransferToPlayer);
  }

  public void OnPointerEnter(PointerEventData eventData) {
    itemBackground.color = itemBackgroundHoverColor;
  }

  public void OnPointerExit(PointerEventData eventData) {
    itemBackground.color = itemBackgroundDefaultColor;
  }

  public void Setup(Item item) {
    itemRepresented = item;
    Sprite itemSprite = ItemManager.Instance.GetItemSprite(itemRepresented.ItemType);
    itemIcon.sprite = itemSprite;
  }

  public void TransferToPlayer() {
    DenSystemManager.Instance.TransferItemToPlayer(itemRepresented);
  }
  
  
}