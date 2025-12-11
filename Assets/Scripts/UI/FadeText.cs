using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    
    [Tooltip("I tried to get emoji support for like an hour with no luck, so it's an image")]
    [SerializeField]
    private Image brainImage;
    
    private TextMeshProUGUI textMeshPro;
    private RectTransform rectTransform;
    private Vector3 startPosition;
    private Color startColor;
    private Coroutine animationCoroutine;
    
    // Pooling support
    private System.Action<FadeText> onAnimationComplete;
    private bool isPooled = false;

    private Vector3 startPositionWorld;
    
    /// <summary>
    /// Initializes the FadeText with custom settings and starts the animation.
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="duration">Duration of the animation in seconds</param>
    /// <param name="upwardMovementDistance">Distance to move upwards during animation</param>
    /// <param name="onComplete">Callback when animation completes (for pooling)</param>
    public void Initialize(string text, float duration = 2f, float upwardMovementDistance = 100f, System.Action<FadeText> onComplete = null)
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
        startPositionWorld = Camera.main.ScreenToWorldPoint(rectTransform.anchoredPosition);
        startColor = textMeshPro.color;
        
        if (text == "\U0001F9E0")
        {
          textMeshPro.text = "";
          brainImage.color = Color.white;
          startColor = brainImage.color;
        }
        
        // Store callback for pooling
        onAnimationComplete = onComplete;
        isPooled = onComplete != null;
        
        // Ensure object is active
        gameObject.SetActive(true);
        
        // Start animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateCoroutine());
    }
    
    /// <summary>
    /// Resets the FadeText to its initial state for reuse in the pool.
    /// </summary>
    public void ResetForPool()
    {
        // Stop any running animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        // Reset components if they exist
        if (textMeshPro != null)
        {
            textMeshPro.color = new Color(textMeshPro.color.r, textMeshPro.color.g, textMeshPro.color.b, 1f);
        }
        
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector3.zero;
        }
        
        onAnimationComplete = null;
        isPooled = false;
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
            if (brainImage.color.a > 0f)
            {
              brainImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentColor.a);
            }
            // Update position
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, endPosition, t);

            if (true)
            {
              Vector3 worldDisplacement = Camera.main.WorldToScreenPoint(startPositionWorld) - startPosition;
              // rectTransform.anchoredPosition -= new Vector2(worldDisplacement.x/2, worldDisplacement.y/2);
            }
            
            yield return null;
        }
        
        // Ensure final state
        Color finalColor = startColor;
        finalColor.a = 0f;
        textMeshPro.color = finalColor;
        rectTransform.position = endPosition;
        
        // Return to pool or destroy
        if (isPooled && onAnimationComplete != null)
        {
            onAnimationComplete(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
    }
}
