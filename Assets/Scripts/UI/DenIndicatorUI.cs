using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI indicator that shows the direction of the nearest off-screen den.
/// Attach this script to the indicator GameObject (must have an Image component).
/// The indicator will position itself on the screen edge and rotate to point toward the nearest den.
/// </summary>
[RequireComponent(typeof(Image))]
public class DenIndicatorUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Distance from the screen edge for the indicator (in pixels).")]
    [SerializeField] private float _edgeDistance = 50f;
    
    [Tooltip("Padding from screen edges to keep indicators visible.")]
    [SerializeField] private float _screenPadding = 20f;
    
    private Camera _mainCamera;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private RectTransform _canvasRectTransform;
    private Image _image;
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        
        if (_image == null)
        {
            Debug.LogError("DenIndicatorUI: GameObject must have an Image component!");
            enabled = false;
            return;
        }
        
        // Find canvas
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError("DenIndicatorUI: No Canvas found in the scene!");
                enabled = false;
                return;
            }
        }
        
        _canvasRectTransform = _canvas.GetComponent<RectTransform>();
        
        // Find main camera
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("DenIndicatorUI: Main Camera not found! Please ensure there's a camera tagged as 'MainCamera'.");
            enabled = false;
            return;
        }
        
        // Ensure anchor is set to center for proper positioning
        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // Hide initially
        _image.enabled = false;
    }
    
    private void Update()
    {
        if (_canvasRectTransform == null || _mainCamera == null)
        {
            return;
        }
        
        // Get all dens in the scene
        List<Den> dens = InteractableManager.Instance != null ? InteractableManager.Instance.GetAllDens() : new List<Den>();
        
        // Get player position (first controllable animal)
        Vector3 playerWorldPos = GetPlayerWorldPosition();
        
        // Find the nearest off-screen den
        Den nearestOffScreenDen = null;
        if (dens.Count > 0)
        {
            nearestOffScreenDen = FindNearestOffScreenDen(dens, playerWorldPos);
        }
        
        if (nearestOffScreenDen == null)
        {
            // All dens are on-screen or no dens exist, hide indicator
            _image.enabled = false;
            return;
        }
        
        // Show indicator and update position
        _image.enabled = true;
        UpdateIndicatorPosition(nearestOffScreenDen.transform.position, playerWorldPos);
    }
    
    /// <summary>
    /// Gets the player's world position from the first controllable animal.
    /// </summary>
    private Vector3 GetPlayerWorldPosition()
    {
        if (AnimalManager.Instance == null)
        {
            return Vector3.zero;
        }
        
        List<Animal> animals = AnimalManager.Instance.GetAllAnimals();
        foreach (Animal animal in animals)
        {
            if (animal != null && animal.IsControllable)
            {
                return animal.transform.position;
            }
        }
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// Finds the nearest off-screen den to the player.
    /// </summary>
    private Den FindNearestOffScreenDen(List<Den> dens, Vector3 playerWorldPos)
    {
        Den nearestDen = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Den den in dens)
        {
            if (den == null)
            {
                continue;
            }
            
            // Check if den is visible on screen
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(den.transform.position);
            bool isOnScreen = viewportPos.x >= 0f && viewportPos.x <= 1f && 
                             viewportPos.y >= 0f && viewportPos.y <= 1f && 
                             viewportPos.z > 0f;
            
            if (isOnScreen)
            {
                continue; // Skip on-screen dens
            }
            
            // Calculate distance from player
            float distance = Vector3.Distance(playerWorldPos, den.transform.position);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestDen = den;
            }
        }
        
        return nearestDen;
    }
    
    /// <summary>
    /// Updates the indicator position and rotation to point toward the den.
    /// </summary>
    private void UpdateIndicatorPosition(Vector3 denWorldPos, Vector3 playerWorldPos)
    {
        // Calculate screen edge position
        Vector2 screenEdgePos = CalculateScreenEdgePosition(denWorldPos, playerWorldPos);
        _rectTransform.anchoredPosition = screenEdgePos;
        
        // Rotate indicator to point toward den
        Vector3 directionToDen = (denWorldPos - playerWorldPos).normalized;
        float angle = Mathf.Atan2(directionToDen.y, directionToDen.x) * Mathf.Rad2Deg;
        _rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    
    /// <summary>
    /// Calculates the position on the screen edge where the indicator should be placed.
    /// Finds where the line from screen center to den intersects the screen edge.
    /// </summary>
    private Vector2 CalculateScreenEdgePosition(Vector3 denWorldPos, Vector3 playerWorldPos)
    {
        // Get den position in viewport coordinates (0-1 range)
        Vector3 viewportPos = _mainCamera.WorldToViewportPoint(denWorldPos);
        
        // If den is behind camera, flip the direction
        if (viewportPos.z < 0)
        {
            viewportPos.x = 1f - viewportPos.x;
            viewportPos.y = 1f - viewportPos.y;
        }
        
        // Get screen dimensions
        Vector2 screenSize = _canvasRectTransform.sizeDelta;
        if (screenSize.x == 0 || screenSize.y == 0)
        {
            // If canvas size is 0, use actual screen size
            screenSize = new Vector2(Screen.width, Screen.height);
        }
        
        float halfWidth = screenSize.x * 0.5f;
        float halfHeight = screenSize.y * 0.5f;
        
        // Calculate direction from center (0.5, 0.5) to den in viewport space
        Vector2 direction = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
        
        // If direction is too small, return center (shouldn't happen for off-screen dens)
        if (direction.magnitude < 0.001f)
        {
            return Vector2.zero;
        }
        
        direction.Normalize();
        
        // Calculate edge boundaries (in viewport space, accounting for padding)
        float edgeLeft = 0f;
        float edgeRight = 1f;
        float edgeBottom = 0f;
        float edgeTop = 1f;
        
        // Convert padding to viewport space
        float paddingX = _screenPadding / screenSize.x;
        float paddingY = _screenPadding / screenSize.y;
        float edgePaddingX = (_edgeDistance + _screenPadding) / screenSize.x;
        float edgePaddingY = (_edgeDistance + _screenPadding) / screenSize.y;
        
        // Find intersection with screen edges using parametric line equation
        // Line: start = (0.5, 0.5), direction = (direction.x, direction.y)
        // Parametric: point = start + t * direction
        // Find t where line intersects edge boundaries
        
        float t = float.MaxValue;
        Vector2 edgePos = Vector2.zero;
        
        // Check intersection with right edge (x = edgeRight - edgePaddingX)
        if (direction.x > 0.001f)
        {
            float tRight = (edgeRight - edgePaddingX - 0.5f) / direction.x;
            float yAtRight = 0.5f + direction.y * tRight;
            if (yAtRight >= edgeBottom + paddingY && yAtRight <= edgeTop - paddingY && tRight < t && tRight > 0)
            {
                t = tRight;
                edgePos = new Vector2(
                    (edgeRight - edgePaddingX) * screenSize.x - halfWidth,
                    yAtRight * screenSize.y - halfHeight
                );
            }
        }
        
        // Check intersection with left edge (x = edgeLeft + edgePaddingX)
        if (direction.x < -0.001f)
        {
            float tLeft = (edgeLeft + edgePaddingX - 0.5f) / direction.x;
            float yAtLeft = 0.5f + direction.y * tLeft;
            if (yAtLeft >= edgeBottom + paddingY && yAtLeft <= edgeTop - paddingY && tLeft < t && tLeft > 0)
            {
                t = tLeft;
                edgePos = new Vector2(
                    (edgeLeft + edgePaddingX) * screenSize.x - halfWidth,
                    yAtLeft * screenSize.y - halfHeight
                );
            }
        }
        
        // Check intersection with top edge (y = edgeTop - edgePaddingY)
        if (direction.y > 0.001f)
        {
            float tTop = (edgeTop - edgePaddingY - 0.5f) / direction.y;
            float xAtTop = 0.5f + direction.x * tTop;
            if (xAtTop >= edgeLeft + paddingX && xAtTop <= edgeRight - paddingX && tTop < t && tTop > 0)
            {
                t = tTop;
                edgePos = new Vector2(
                    xAtTop * screenSize.x - halfWidth,
                    (edgeTop - edgePaddingY) * screenSize.y - halfHeight
                );
            }
        }
        
        // Check intersection with bottom edge (y = edgeBottom + edgePaddingY)
        if (direction.y < -0.001f)
        {
            float tBottom = (edgeBottom + edgePaddingY - 0.5f) / direction.y;
            float xAtBottom = 0.5f + direction.x * tBottom;
            if (xAtBottom >= edgeLeft + paddingX && xAtBottom <= edgeRight - paddingX && tBottom < t && tBottom > 0)
            {
                t = tBottom;
                edgePos = new Vector2(
                    xAtBottom * screenSize.x - halfWidth,
                    (edgeBottom + edgePaddingY) * screenSize.y - halfHeight
                );
            }
        }
        
        // Final safety clamp
        float maxX = halfWidth - _screenPadding;
        float maxY = halfHeight - _screenPadding;
        edgePos.x = Mathf.Clamp(edgePos.x, -maxX, maxX);
        edgePos.y = Mathf.Clamp(edgePos.y, -maxY, maxY);
        
        return edgePos;
    }
}
