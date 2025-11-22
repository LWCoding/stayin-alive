using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Component that automatically plays a hover sound when the pointer enters a UI element
/// and a UI sound when the element is clicked.
/// Attach this to any Button or Selectable UI element to add hover and click sound functionality.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sound Settings")]
    [Tooltip("If checked, will only play sound when the button is interactable")]
    [SerializeField] private bool onlyPlayWhenInteractable = true;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (selectable == null)
        {
            Debug.LogWarning($"ButtonHoverSound on {gameObject.name}: No Selectable component found!");
        }
    }

    /// <summary>
    /// Called when the pointer enters the UI element.
    /// Plays the hover sound if conditions are met.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Don't play if we should only play when interactable and the button is not interactable
        if (onlyPlayWhenInteractable && (selectable == null || !selectable.interactable))
        {
            return;
        }

        // Play the hover sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.SFXType.Hover);
        }
        else
        {
            Debug.LogWarning($"ButtonHoverSound on {gameObject.name}: AudioManager.Instance is null!");
        }
    }

    /// <summary>
    /// Called when the UI element is clicked.
    /// Plays the UI sound if conditions are met.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Don't play if we should only play when interactable and the button is not interactable
        if (onlyPlayWhenInteractable && (selectable == null || !selectable.interactable))
        {
            return;
        }

        // Play the UI click sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.SFXType.UI);
        }
        else
        {
            Debug.LogWarning($"ButtonHoverSound on {gameObject.name}: AudioManager.Instance is null!");
        }
    }
}

