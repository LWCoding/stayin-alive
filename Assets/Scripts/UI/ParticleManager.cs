using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages UI particles (like FadeText) that can be spawned at any position on the screen in UI space.
/// Similar to ParticleEffectManager but for UI elements instead of world-space particles.
/// </summary>
public class ParticleManager : Singleton<ParticleManager>
{
    [Header("FadeText Prefab")]
    [Tooltip("Prefab for FadeText UI elements. If not assigned, will be created automatically.")]
    [SerializeField] private GameObject fadeTextPrefab;
    
    [Header("Default FadeText Settings")]
    [Tooltip("Default duration for FadeText animations (in seconds)")]
    [SerializeField] private float defaultFadeTextDuration = 2f;
    
    [Tooltip("Default upward movement distance for FadeText (in UI units)")]
    [SerializeField] private float defaultFadeTextMovementDistance = 100f;
    
    private Transform particleParent;
    private Canvas targetCanvas;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Find or create canvas
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("ParticleManager: No Canvas found! Please assign a Canvas in the Inspector.");
                return;
            }
        }
        
        // Create parent for UI particles
        GameObject parentObj = new GameObject("UIParticles");
        parentObj.transform.SetParent(targetCanvas.transform, false);
        RectTransform parentRect = parentObj.AddComponent<RectTransform>();
        parentRect.anchorMin = Vector2.zero;
        parentRect.anchorMax = Vector2.one;
        parentRect.sizeDelta = Vector2.zero;
        parentRect.anchoredPosition = Vector2.zero;
        particleParent = parentObj.transform;
    }
    
    /// <summary>
    /// Spawns a FadeText at the specified UI position (in screen space coordinates).
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="screenPosition">Position in screen space (0,0 = bottom-left, Screen.width/height = top-right)</param>
    /// <param name="duration">Duration of the fade animation (uses default if null)</param>
    /// <param name="upwardMovementDistance">Distance to move upwards (uses default if null)</param>
    /// <returns>The spawned FadeText component, or null if spawning failed</returns>
    public FadeText SpawnFadeText(string text, Vector2 screenPosition, float? duration = null, float? upwardMovementDistance = null)
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("ParticleManager: Cannot spawn FadeText - Canvas is not assigned!");
            return null;
        }
        
        // Create FadeText GameObject
        GameObject fadeTextObj;
        if (fadeTextPrefab != null)
        {
            fadeTextObj = Instantiate(fadeTextPrefab, particleParent);
        }
        else
        {
            fadeTextObj = new GameObject("FadeText");
            fadeTextObj.transform.SetParent(particleParent, false);
            
            // Add RectTransform
            RectTransform rectTransform = fadeTextObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200f, 50f); // Default size
            
            // Add TextMeshProUGUI component
            TextMeshProUGUI textMeshPro = fadeTextObj.AddComponent<TextMeshProUGUI>();
            textMeshPro.fontSize = 36f;
            textMeshPro.alignment = TMPro.TextAlignmentOptions.Center;
            textMeshPro.color = Color.white;
        }
        
        // Convert screen position to canvas position
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        Vector2 canvasPosition = ScreenToCanvasPosition(screenPosition, canvasRect);
        
        // Set position
        RectTransform fadeTextRect = fadeTextObj.GetComponent<RectTransform>();
        if (fadeTextRect != null)
        {
            fadeTextRect.anchoredPosition = canvasPosition;
        }
        
        // Get or add FadeText component
        FadeText fadeText = fadeTextObj.GetComponent<FadeText>();
        if (fadeText == null)
        {
            fadeText = fadeTextObj.AddComponent<FadeText>();
        }
        
        // Initialize with parameters
        float animDuration = duration ?? defaultFadeTextDuration;
        float movementDistance = upwardMovementDistance ?? defaultFadeTextMovementDistance;
        fadeText.Initialize(text, animDuration, movementDistance);
        
        return fadeText;
    }
    
    /// <summary>
    /// Spawns a FadeText at a RectTransform's position in UI space.
    /// Useful for spawning at specific UI element positions.
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="targetRectTransform">The RectTransform to spawn at</param>
    /// <param name="duration">Duration of the fade animation (uses default if null)</param>
    /// <param name="upwardMovementDistance">Distance to move upwards (uses default if null)</param>
    /// <returns>The spawned FadeText component, or null if spawning failed</returns>
    public FadeText SpawnFadeTextAtRectTransform(string text, RectTransform targetRectTransform, float? duration = null, float? upwardMovementDistance = null)
    {
        if (targetRectTransform == null)
        {
            Debug.LogWarning("ParticleManager: Cannot spawn FadeText - target RectTransform is null!");
            return null;
        }
        
        // Convert RectTransform position to screen position
        Vector3[] worldCorners = new Vector3[4];
        targetRectTransform.GetWorldCorners(worldCorners);
        Vector2 centerScreenPos = RectTransformUtility.WorldToScreenPoint(null, (worldCorners[0] + worldCorners[2]) * 0.5f);
        
        return SpawnFadeText(text, centerScreenPos, duration, upwardMovementDistance);
    }
    
    /// <summary>
    /// Converts a screen space position to canvas space position.
    /// </summary>
    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition, RectTransform canvasRect)
    {
        Vector2 localPoint;
        // For screen-space overlay canvases, worldCamera is null
        // For screen-space camera canvases, use the assigned camera
        Camera canvasCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvasCamera,
            out localPoint
        );
        return localPoint;
    }
    
    /// <summary>
    /// Sets the target canvas for spawning UI particles.
    /// </summary>
    public void SetTargetCanvas(Canvas canvas)
    {
        targetCanvas = canvas;
        if (particleParent != null && canvas != null)
        {
            particleParent.SetParent(canvas.transform, false);
        }
    }
}
