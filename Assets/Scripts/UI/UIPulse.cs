using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utility script that gradually pulses a UI element to be bigger and smaller.
/// Uses linear interpolation for smooth scaling animation.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIPulse : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField]
    [Tooltip("Minimum scale value (1.0 = normal size)")]
    private float minScale = 0.9f;
    
    [SerializeField]
    [Tooltip("Maximum scale value (1.0 = normal size)")]
    private float maxScale = 1.1f;

    [Header("Timing Settings")]
    [SerializeField]
    [Tooltip("Duration of one complete pulse cycle (in seconds)")]
    private float pulseDuration = 1.0f;

    [SerializeField]
    [Tooltip("Whether to start pulsing automatically on Start")]
    private bool playOnStart = true;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private float timer = 0f;
    private bool isPulsing = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartPulse();
        }
    }

    private void Update()
    {
        if (isPulsing)
        {
            timer += Time.deltaTime;
            
            // Calculate normalized time (0 to 1) for one complete cycle
            float normalizedTime = (timer % pulseDuration) / pulseDuration;
            
            // Use ping-pong effect: 0 -> 1 -> 0
            float pingPong = Mathf.PingPong(normalizedTime * 2f, 1f);
            
            // Linear interpolation between min and max scale
            float currentScale = Mathf.Lerp(minScale, maxScale, pingPong);
            
            rectTransform.localScale = originalScale * currentScale;
        }
    }

    /// <summary>
    /// Starts the pulsing animation.
    /// </summary>
    public void StartPulse()
    {
        isPulsing = true;
        timer = 0f;
    }

    /// <summary>
    /// Stops the pulsing animation and resets to original scale.
    /// </summary>
    public void StopPulse()
    {
        isPulsing = false;
        rectTransform.localScale = originalScale;
    }

    /// <summary>
    /// Resets the scale to original without stopping the pulse.
    /// </summary>
    public void ResetScale()
    {
        originalScale = rectTransform.localScale;
    }
}
