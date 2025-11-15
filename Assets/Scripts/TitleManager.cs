using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the title screen. Handles playing the start animation and transitioning to the next scene.
/// </summary>
public class TitleManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The Animator component that will play the 'Start' animation.")]
    [SerializeField] private Animator _animator;
    
    [Header("Scene Settings")]
    [Tooltip("The name of the scene to load after the animation completes.")]
    [SerializeField] private string _nextSceneName;
    
    [Header("UI Blocking")]
    [Tooltip("The GameObject to show during the animation to block UI interactions. Should have a GraphicsRaycaster component.")]
    [SerializeField] private GameObject _uiBlocker;
    
    /// <summary>
    /// Plays the "Start" animation and then switches to the next scene when the animation is complete.
    /// </summary>
    public void Play()
    {
        if (_animator == null)
        {
            Debug.LogError("TitleManager: Animator is not assigned!");
            return;
        }
        
        if (string.IsNullOrEmpty(_nextSceneName))
        {
            Debug.LogError("TitleManager: Next scene name is not assigned!");
            return;
        }
        
        StartCoroutine(PlayAnimationAndSwitchScene());
    }
    
    private IEnumerator PlayAnimationAndSwitchScene()
    {
        // Show the UI blocker to prevent interactions during animation
        if (_uiBlocker != null)
        {
            _uiBlocker.SetActive(true);
        }
        
        // Play the start animation
        _animator.Play("Play");
        
        // Wait for the animation to complete
        // Get the current state info to determine animation length
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        
        yield return new WaitForEndOfFrame();
        while (_animator.GetCurrentAnimatorStateInfo(0).IsName("Play"))
        {
            yield return null;
        }
        
        // Wipe in and switch to the next scene
        if (ScreenWipe.Instance != null)
        {
            ScreenWipe.Instance.WipeToScene(_nextSceneName);
        }
        else
        {
            Debug.LogError("TitleManager: ScreenWipe instance not found!");
            SceneManager.LoadScene(_nextSceneName);
        }
    }
}

