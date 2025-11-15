using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Image))]
public class ScreenWipe : Singleton<ScreenWipe>
{
    [Header("Wipe Settings")]
    [SerializeField] private float wipeDuration = 0.5f;
    [SerializeField] private AnimationCurve wipeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Wipe Directions")]
    [SerializeField] private WipeDirection wipeInDirection = WipeDirection.TopToBottom;
    [SerializeField] private WipeDirection wipeOutDirection = WipeDirection.BottomToTop;

    public enum WipeDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    private Image wipeImage;
    private RectTransform rectTransform;
    private bool isWiping = false;
    private Vector2 startPosition;
    private Vector2 endPosition;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Initialize components
        wipeImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // Start with screen covered (for scene transitions)
        SetWipePosition(1f, wipeOutDirection);
    }
    
    private void Start()
    {
        // Add a small delay to ensure everything is properly initialized
        StartCoroutine(DelayedStartWipe());
    }
    
    private IEnumerator DelayedStartWipe()
    {
        wipeImage.enabled = true;

        // Wait a while to ensure everything is set up
        yield return new WaitForSeconds(0.5f);

        // Wipe out at scene start
        yield return StartCoroutine(WipeOut());
    }
    
    /// <summary>
    /// Wipe in (cover screen) then restart current scene
    /// </summary>
    public void WipeRestart()
    {
        if (!isWiping)
        {
            StartCoroutine(WipeToSceneCoroutine(SceneManager.GetActiveScene().name));
        }
    }
    
    /// <summary>
    /// Wipe in (cover screen) then load a specific scene
    /// </summary>
    public void WipeToScene(string sceneName)
    {
        if (!isWiping)
        {
            StartCoroutine(WipeToSceneCoroutine(sceneName));
        }
    }
    
    /// <summary>
    /// Wipe in (cover screen) then load scene by build index
    /// </summary>
    public void WipeToScene(int sceneBuildIndex)
    {
        if (!isWiping)
        {
            StartCoroutine(WipeToSceneCoroutine(sceneBuildIndex));
        }
    }
    
    private IEnumerator WipeToSceneCoroutine(string sceneName)
    {
        isWiping = true;
        
        // Wipe in (cover screen)
        yield return StartCoroutine(WipeIn());
        
        // Load scene
        SceneManager.LoadScene(sceneName);
    }
    
    private IEnumerator WipeToSceneCoroutine(int sceneBuildIndex)
    {
        isWiping = true;
        
        // Wipe in (cover screen)
        yield return StartCoroutine(WipeIn());
        
        // Load scene
        SceneManager.LoadScene(sceneBuildIndex);
    }
    
    /// <summary>
    /// Wipe in animation (cover screen)
    /// </summary>
    public IEnumerator WipeIn()
    {
        wipeImage.enabled = true;
        wipeImage.color = Color.black;
        isWiping = true;
        
        // Reset to starting position for wipe in
        SetWipePosition(0f, wipeInDirection);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < wipeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / wipeDuration;
            float curveValue = wipeCurve.Evaluate(t);
            
            float progress = Mathf.Lerp(0f, 1f, curveValue);
            SetWipePosition(progress, wipeInDirection);
            
            yield return null;
        }
        
        SetWipePosition(1f, wipeInDirection);
        isWiping = false;
    }
    
    /// <summary>
    /// Wipe out animation (uncover screen)
    /// </summary>
    public IEnumerator WipeOut()
    {
        wipeImage.enabled = true;
        isWiping = true;
        
        float elapsedTime = 0f;
        float startProgress = GetWipeProgress(wipeOutDirection);
        
        while (elapsedTime < wipeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / wipeDuration;
            float curveValue = wipeCurve.Evaluate(t);
            
            float progress = Mathf.Lerp(startProgress, 0f, curveValue);
            SetWipePosition(progress, wipeOutDirection);
            
            yield return null;
        }
        
        SetWipePosition(0f, wipeOutDirection);
        isWiping = false;
        wipeImage.enabled = false;
    }

    private void SetWipePosition(float progress, WipeDirection direction)
    {
        if (rectTransform == null) return;
        
        // Calculate start and end positions based on direction
        CalculateWipePositions(direction);
        
        // Interpolate between start and end positions
        Vector2 currentPosition = Vector2.Lerp(startPosition, endPosition, progress);
        
        // Apply the position by adjusting anchors
        switch (direction)
        {
            case WipeDirection.LeftToRight:
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(progress, 1);
                break;
                
            case WipeDirection.RightToLeft:
                rectTransform.anchorMin = new Vector2(1 - progress, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                break;
                
            case WipeDirection.TopToBottom:
                rectTransform.anchorMin = new Vector2(0, 1 - progress);
                rectTransform.anchorMax = new Vector2(1, 1);
                break;
                
            case WipeDirection.BottomToTop:
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, progress);
                break;
        }
        
        // Reset offsets to ensure proper positioning
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
    
    private void CalculateWipePositions(WipeDirection direction)
    {
        // These are calculated based on the anchor positions
        // The actual movement is handled by changing the anchor values
        switch (direction)
        {
            case WipeDirection.LeftToRight:
                startPosition = Vector2.zero;
                endPosition = Vector2.one;
                break;
            case WipeDirection.RightToLeft:
                startPosition = Vector2.one;
                endPosition = Vector2.zero;
                break;
            case WipeDirection.TopToBottom:
                startPosition = new Vector2(0, 1);
                endPosition = new Vector2(1, 0);
                break;
            case WipeDirection.BottomToTop:
                startPosition = new Vector2(0, 0);
                endPosition = new Vector2(1, 1);
                break;
        }
    }
    
    private float GetWipeProgress(WipeDirection direction)
    {
        if (rectTransform == null) return 0f;
        
        switch (direction)
        {
            case WipeDirection.LeftToRight:
                return rectTransform.anchorMax.x;
            case WipeDirection.RightToLeft:
                return 1f - rectTransform.anchorMin.x;
            case WipeDirection.TopToBottom:
                return 1f - rectTransform.anchorMin.y;
            case WipeDirection.BottomToTop:
                return rectTransform.anchorMax.y;
            default:
                return 0f;
        }
    }
} 