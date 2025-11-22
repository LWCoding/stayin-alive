using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Component that fades out and moves upwards over a set duration.
/// Used for UI text elements that need to disappear gradually.
/// </summary>
public class FadeText : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Duration in seconds for the fade and movement animation")]
    [SerializeField] private float duration = 2f;
    
    [Tooltip("Distance to move upwards during the animation (in UI units)")]
    [SerializeField] private float upwardMovementDistance = 100f;
    
    [Tooltip("Easing curve for the animation")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    private TextMeshProUGUI textMeshPro;
    private RectTransform rectTransform;
    private Vector3 startPosition;
    private Color startColor;
    private Coroutine animationCoroutine;
    
    /// <summary>
    /// Initializes the FadeText with custom settings and starts the animation.
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="duration">Duration of the animation in seconds</param>
    /// <param name="upwardMovementDistance">Distance to move upwards during animation</param>
    public void Initialize(string text, float duration = 2f, float upwardMovementDistance = 100f)
    {
        // Get or add TextMeshProUGUI component
        textMeshPro = GetComponent<TextMeshProUGUI>();
        if (textMeshPro == null)
        {
            textMeshPro = gameObject.AddComponent<TextMeshProUGUI>();
        }
        
        // Get RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
        
        // Set text
        textMeshPro.text = text;
        
        // Store start values
        this.duration = duration;
        this.upwardMovementDistance = upwardMovementDistance;
        startPosition = rectTransform.anchoredPosition;
        startColor = textMeshPro.color;
        
        // Start animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateCoroutine());
    }
    
    private IEnumerator AnimateCoroutine()
    {
        float elapsed = 0f;
        Vector3 endPosition = startPosition + Vector3.up * upwardMovementDistance;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = fadeCurve.Evaluate(t);
            
            // Update alpha
            Color currentColor = startColor;
            currentColor.a = startColor.a * curveValue;
            textMeshPro.color = currentColor;
            
            // Update position
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, endPosition, t);
            
            yield return null;
        }
        
        // Ensure final state
        Color finalColor = startColor;
        finalColor.a = 0f;
        textMeshPro.color = finalColor;
        rectTransform.anchoredPosition = endPosition;
        
        // Destroy when animation completes
        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
    }
}
