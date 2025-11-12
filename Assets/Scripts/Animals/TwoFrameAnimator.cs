using UnityEngine;
using System.Collections;

/// <summary>
/// Animates a SpriteRenderer between two sprites over a specified interval.
/// </summary>
public class TwoFrameAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("SpriteRenderer to animate. Assign this in the inspector.")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    
    [Tooltip("First sprite frame")]
    [SerializeField] private Sprite _frame1;
    
    [Tooltip("Second sprite frame")]
    [SerializeField] private Sprite _frame2;
    
    [Tooltip("Time interval in seconds between frame switches")]
    [Min(0.01f)]
    [SerializeField] private float _interval = 0.5f;
    private Coroutine _animationCoroutine;
    private bool _isAnimating = false;

    /// <summary>
    /// Sets the SpriteRenderer to animate. Call this if the SpriteRenderer is not assigned in the inspector.
    /// </summary>
    /// <param name="spriteRenderer">The SpriteRenderer to animate</param>
    public void SetSpriteRenderer(SpriteRenderer spriteRenderer)
    {
        _spriteRenderer = spriteRenderer;
    }

    /// <summary>
    /// Initializes the animator with two sprites and an interval.
    /// </summary>
    /// <param name="frame1">First sprite frame</param>
    /// <param name="frame2">Second sprite frame</param>
    /// <param name="interval">Time interval in seconds between frame switches</param>
    public void Initialize(Sprite frame1, Sprite frame2, float interval)
    {
        _frame1 = frame1;
        _frame2 = frame2;
        _interval = Mathf.Max(0.01f, interval);
        
        // Stop any existing animation
        StopAnimation();
        
        // Start the animation if both sprites are valid
        if (_frame1 != null && _frame2 != null)
        {
            StartAnimation();
        }
        else if (_frame1 != null)
        {
            // If only frame1 is available, just set it
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = _frame1;
            }
        }
    }

    private void OnEnable()
    {
        // Restart animation when enabled if we have valid sprites
        if (_frame1 != null && _frame2 != null && !_isAnimating)
        {
            StartAnimation();
        }
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    /// <summary>
    /// Starts the animation coroutine.
    /// </summary>
    private void StartAnimation()
    {
        if (_isAnimating || _frame1 == null || _frame2 == null || _spriteRenderer == null)
        {
            return;
        }

        _isAnimating = true;
        _animationCoroutine = StartCoroutine(AnimateCoroutine());
    }

    /// <summary>
    /// Stops the animation coroutine.
    /// </summary>
    private void StopAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        _isAnimating = false;
    }

    /// <summary>
    /// Coroutine that alternates between the two sprite frames.
    /// </summary>
    private IEnumerator AnimateCoroutine()
    {
        bool useFrame1 = true;
        
        // Set initial sprite
        if (_spriteRenderer != null && _frame1 != null)
        {
            _spriteRenderer.sprite = _frame1;
        }

        while (_isAnimating)
        {
            yield return new WaitForSeconds(_interval);
            
            if (_spriteRenderer == null || !_isAnimating)
            {
                break;
            }

            // Switch to the other frame
            useFrame1 = !useFrame1;
            _spriteRenderer.sprite = useFrame1 ? _frame1 : _frame2;
        }
    }
}

