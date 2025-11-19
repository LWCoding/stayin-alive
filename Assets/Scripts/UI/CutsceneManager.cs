using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Types of animations that can be triggered during dialogue.
/// </summary>
public enum DialogueAnimation
{
    None = 0,
    FadeRatGod = 1,
    JumpCloser = 2,
    GodTalk = 3,
}

/// <summary>
/// Represents a single dialogue entry with text and optional animation.
/// </summary>
[System.Serializable]
public struct DialogueEntry
{
    [Tooltip("Animation to trigger when this dialogue is displayed. Use None for no animation.")]
    public DialogueAnimation animation;
    
    [Tooltip("The dialogue text to display.")]
    [TextArea(2, 6)]
    public string text;
}

/// <summary>
/// Manages cutscene dialogue with typing effect. Press E or click to advance to the next dialogue.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [Tooltip("List of dialogue entries. Each entry contains text and an optional animation.")]
    [SerializeField] private DialogueEntry[] _dialogueEntries = new DialogueEntry[0];
    
    [Header("UI References")]
    [Tooltip("TextMeshProUGUI component that displays the dialogue text.")]
    [SerializeField] private TextMeshProUGUI _dialogueText;
    
    [Header("Animation")]
    [Tooltip("Animator component that contains the animation clips. Animation clips should be named to match the DialogueAnimation enum values (e.g., 'FadeRatGod', 'JumpCloser', etc.).")]
    [SerializeField] private Animator _animator;
    
    [Tooltip("Animator component for the rat. JumpCloser animation will be played on this animator.")]
    [SerializeField] private Animator _ratAnimator;
    
    [Tooltip("UI Image component whose sprite will be changed when GodTalk animation ends.")]
    [SerializeField] private Image _ratGodImage;
    
    [Tooltip("Default sprite to set on the UI Image when GodTalk animation ends.")]
    [SerializeField] private Sprite _defaultRatGodSprite;
    
    [Header("Typing Settings")]
    [Tooltip("Time delay between each character when typing (in seconds).")]
    [SerializeField] private float _typingSpeed = 0.005f;
    
    private int _currentDialogueIndex = 0;
    private Coroutine _typingCoroutine = null;
    private Coroutine _animationWaitCoroutine = null;
    private bool _isTyping = false;
    private string _currentFullText = "";
    private DialogueAnimation _currentAnimation = DialogueAnimation.None;

    private void Start()
    {
        // Ensure animators are in a neutral state at start
        if (_animator != null)
        {
            _animator.enabled = false;
        }
        if (_ratAnimator != null)
        {
            _ratAnimator.enabled = false;
        }
        
        // Start displaying the first dialogue if available
        if (_dialogueEntries.Length > 0)
        {
            StartDialogue();
        }
        else if (_dialogueText != null)
        {
            _dialogueText.text = "";
        }
    }

    private void Update()
    {
        // Check for E key press or mouse click to advance dialogue
        bool shouldAdvance = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
        
        if (shouldAdvance)
        {
            if (_isTyping)
            {
                // If currently typing, skip to the end
                SkipTyping();
            }
            else
            {
                // Advance to next dialogue
                NextDialogue();
            }
        }
    }

    /// <summary>
    /// Starts the dialogue system from the beginning.
    /// </summary>
    public void StartDialogue()
    {
        _currentDialogueIndex = 0;
        DisplayDialogue(_currentDialogueIndex);
    }

    /// <summary>
    /// Advances to the next dialogue in the array.
    /// </summary>
    private void NextDialogue()
    {
        _currentDialogueIndex++;
        
        if (_currentDialogueIndex >= _dialogueEntries.Length)
        {
            // All dialogues have been shown
            if (_dialogueText != null)
            {
                _dialogueText.text = "";
            }
            return;
        }
        
        DisplayDialogue(_currentDialogueIndex);
    }

    /// <summary>
    /// Displays the dialogue at the specified index with typing effect.
    /// </summary>
    private void DisplayDialogue(int index)
    {
        if (index < 0 || index >= _dialogueEntries.Length || _dialogueText == null)
        {
            return;
        }

        DialogueEntry entry = _dialogueEntries[index];
        _currentFullText = entry.text;
        _currentAnimation = entry.animation;
        
        // Trigger animation if one is specified
        if (entry.animation != DialogueAnimation.None)
        {
            TriggerAnimation(entry.animation);
        }
        
        // Stop any existing typing coroutine
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }
        
        // Start typing effect
        _typingCoroutine = StartCoroutine(TypeText(_currentFullText));
    }

    /// <summary>
    /// Triggers the specified animation by playing the corresponding animation clip on the Animator.
    /// The animation clip name must match the DialogueAnimation enum value.
    /// JumpCloser animation is played on the rat animator instead of the main animator.
    /// </summary>
    private void TriggerAnimation(DialogueAnimation animation)
    {
        if (animation == DialogueAnimation.None)
        {
            return;
        }

        // Stop any existing animation wait coroutine
        if (_animationWaitCoroutine != null)
        {
            StopCoroutine(_animationWaitCoroutine);
            _animationWaitCoroutine = null;
        }

        // Convert enum to string to match animation clip name
        string animationName = animation.ToString();
        
        // JumpCloser is played on the rat animator, and GodTalk is played on the main animator
        if (animation == DialogueAnimation.JumpCloser)
        {
            // Play JumpCloser on rat animator
            if (_ratAnimator != null)
            {
                // Enable rat animator if it was disabled
                if (!_ratAnimator.enabled)
                {
                    _ratAnimator.enabled = true;
                }
                
                // Play the JumpCloser animation on the rat animator
                _ratAnimator.Play(animationName);
            }
            
            // Also play GodTalk on the main animator
            if (_animator != null)
            {
                // Enable animator if it was disabled
                if (!_animator.enabled)
                {
                    _animator.enabled = true;
                }
                
                // Play GodTalk animation on the main animator
                _animator.Play("GodTalk");
            }
        }
        else
        {
            // All other animations are played on the main animator
            if (_animator != null)
            {
                // Enable animator if it was disabled
                if (!_animator.enabled)
                {
                    _animator.enabled = true;
                }
                
                // Play the animation clip by name
                _animator.Play(animationName);
                
                // Note: GodTalk animation should be set to loop in Unity's Animator Controller
                // It will loop until typing finishes, at which point StopAnimation() is called
            }
        }
    }


    /// <summary>
    /// Coroutine that types out the text character by character.
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        _isTyping = true;
        _dialogueText.text = "";
        
        foreach (char character in text)
        {
            _dialogueText.text += character;
            yield return new WaitForSeconds(_typingSpeed);
        }
        
        _isTyping = false;
        _typingCoroutine = null;
        
        // Stop GodTalk animation when typing is complete (for both GodTalk and JumpCloser)
        if (_currentAnimation == DialogueAnimation.GodTalk || _currentAnimation == DialogueAnimation.JumpCloser)
        {
            StopAnimation();
        }
    }

    /// <summary>
    /// Skips the typing animation and immediately shows the full text.
    /// </summary>
    private void SkipTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        
        if (_dialogueText != null)
        {
            _dialogueText.text = _currentFullText;
        }
        
        _isTyping = false;
        
        // Stop GodTalk animation when typing is skipped (for both GodTalk and JumpCloser)
        if (_currentAnimation == DialogueAnimation.GodTalk || _currentAnimation == DialogueAnimation.JumpCloser)
        {
            StopAnimation();
        }
    }

    /// <summary>
    /// Stops the current animation by disabling the animator.
    /// </summary>
    private void StopAnimation()
    {
        // Stop the animation wait coroutine if it's running
        if (_animationWaitCoroutine != null)
        {
            StopCoroutine(_animationWaitCoroutine);
            _animationWaitCoroutine = null;
        }
        
        // If GodTalk or JumpCloser animation was playing, change sprite immediately
        // (JumpCloser also plays GodTalk on the main animator)
        if (_currentAnimation == DialogueAnimation.GodTalk || _currentAnimation == DialogueAnimation.JumpCloser)
        {
            if (_ratGodImage != null && _defaultRatGodSprite != null)
            {
                _ratGodImage.sprite = _defaultRatGodSprite;
            }
        }
        
        if (_animator != null && _animator.enabled)
        {
            _animator.enabled = false;
        }
        
        // Also stop rat animator if JumpCloser was playing
        if (_currentAnimation == DialogueAnimation.JumpCloser)
        {
            if (_ratAnimator != null && _ratAnimator.enabled)
            {
                _ratAnimator.enabled = false;
            }
        }
    }
}

