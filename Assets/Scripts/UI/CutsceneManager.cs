using System.Collections;
using System.Collections.Generic;
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
    Fall = 4,
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
    [Tooltip("Root GameObject that contains the dialogue UI (will be hidden when dialogue finishes).")]
    [SerializeField] private GameObject _dialogueContainer;
    [Tooltip("Script used to switch scenes once dialogue ends.")]
    [SerializeField] private SwitchSceneTo _sceneSwitcher;
    [Tooltip("Scene name to load once the dialogue sequence finishes.")]
    [SerializeField] private string _sceneToLoadOnComplete = "03_Tutorial";
    
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

    [Header("Typing Audio")]
    [Tooltip("AudioSource used to play the typing blip sounds.")]
    [SerializeField] private AudioSource _typingAudioSource;
    [Tooltip("AudioClip that will be played as a blip while text is typing.")]
    [SerializeField] private AudioClip _typingBlipClip;
    [Tooltip("Delay between typing blip sounds while characters are revealed.")]
    [SerializeField] private float _typingBlipDelay = 0.08f;
    [Tooltip("Random pitch range applied to each typing blip (min <= max).")]
    [SerializeField] private float _typingPitchMin = 0.95f;
    [SerializeField] private float _typingPitchMax = 1.05f;

    [Header("Cutscene Objects")]
    [Tooltip("UI GameObject that represents the stick.")]
    [SerializeField] private GameObject _stickObject;
    [Tooltip("UI GameObject that represents the den.")]
    [SerializeField] private GameObject _denObject;
    [Tooltip("UI GameObject that represents the worm.")]
    [SerializeField] private GameObject _wormObject;
    [Tooltip("UI GameObject that represents the grass.")]
    [SerializeField] private GameObject _grassObject;
    [Tooltip("UI GameObject that represents the hawk.")]
    [SerializeField] private GameObject _hawkObject;
    [Tooltip("UI GameObject that represents the coyote.")]
    [SerializeField] private GameObject _coyoteObject;
    [Tooltip("UI GameObject that represents the bones.")]
    [SerializeField] private GameObject _bonesObject;
    [Tooltip("UI GameObject that represents the crowd.")]
    [SerializeField] private GameObject _crowdObject;

    [Header("Object Fade Settings")]
    [Tooltip("Duration (in seconds) for UI objects to fade in.")]
    [SerializeField] private float _objectFadeDuration = 0.5f;

    [Header("Object Reveal Dialogue Indices")]
    [Tooltip("Dialogue index at which the stick UI object should fade in.")]
    [SerializeField] private int _stickDialogueIndex = -1;
    [Tooltip("Dialogue index at which the den UI object should fade in.")]
    [SerializeField] private int _denDialogueIndex = -1;
    [Tooltip("Dialogue index at which the worm UI object should fade in.")]
    [SerializeField] private int _wormDialogueIndex = -1;
    [Tooltip("Dialogue index at which the grass UI object should fade in.")]
    [SerializeField] private int _grassDialogueIndex = -1;
    [Tooltip("Dialogue index at which the hawk UI object should fade in.")]
    [SerializeField] private int _hawkDialogueIndex = -1;
    [Tooltip("Dialogue index at which the coyote UI object should fade in.")]
    [SerializeField] private int _coyoteDialogueIndex = -1;
    [Tooltip("Dialogue index at which the bones UI object should fade in.")]
    [SerializeField] private int _bonesDialogueIndex = -1;
    [Tooltip("Dialogue index at which the crowd UI object should fade in.")]
    [SerializeField] private int _crowdDialogueIndex = -1;
    
    [Header("Cutscene Visibility Controls")]
    [Tooltip("If checked, objects for cutscene 1 (stick, den, worm, grass, hawk, coyote) will not appear. Uncheck to show them.")]
    [SerializeField] private bool _hideCutscene1Objects = false;
    [Tooltip("If checked, the crowd object will not appear. Uncheck to show it.")]
    [SerializeField] private bool _hideCutscene2Objects = false;
    [Tooltip("If checked, the bones object will not appear. Uncheck to show it.")]
    [SerializeField] private bool _hideCutscene3Objects = false;
    
    private int _currentDialogueIndex = 0;
    private Coroutine _typingCoroutine = null;
    private Coroutine _typingAudioCoroutine = null;
    private Coroutine _animationWaitCoroutine = null;
    private bool _isTyping = false;
    private string _currentFullText = "";
    private DialogueAnimation _currentAnimation = DialogueAnimation.None;
    private readonly Dictionary<GameObject, Coroutine> _activeFadeCoroutines = new Dictionary<GameObject, Coroutine>();

    private void Start()
    {
        // Ensure animators are in a neutral state at start
        if (_animator != null)
        {
            _animator.enabled = false;
            _ratAnimator.enabled = false;
        }
        
        // Ensure dialogue text starts with a space (to take up vertical space)
        if (_dialogueText != null)
        {
            _dialogueText.text = " ";
        }
        
        // Start the coroutine to wait before starting dialogue
        if (_dialogueEntries.Length > 0)
        {
            StartCoroutine(DelayedStartDialogue());
        }
    }
    
    /// <summary>
    /// Coroutine that waits 1 second before starting dialogue.
    /// </summary>
    private IEnumerator DelayedStartDialogue()
    {
        // Wait 1 second
        yield return new WaitForSeconds(1f);
        
        // Now start the dialogue
        ShowDialogueUI();
        StartDialogue();
    }

    private void Update()
    {
        // Check for E key press or mouse click to advance dialogue
        bool shouldAdvance =
            Input.GetKeyDown(KeyCode.E) ||
            Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return);
        
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
        ShowDialogueUI();
        DisplayDialogue(_currentDialogueIndex, triggerAnimation: false);
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
            HideDialogueUI();
            SwitchToNextScene();
            return;
        }
        
        DisplayDialogue(_currentDialogueIndex);
    }

    /// <summary>
    /// Displays the dialogue at the specified index with typing effect.
    /// </summary>
    /// <param name="index">The dialogue index to display.</param>
    /// <param name="triggerAnimation">Whether to trigger animations associated with this dialogue entry.</param>
    private void DisplayDialogue(int index, bool triggerAnimation = true)
    {
        if (index < 0 || index >= _dialogueEntries.Length || _dialogueText == null)
        {
            return;
        }

        DialogueEntry entry = _dialogueEntries[index];
        _currentFullText = entry.text;
        _currentAnimation = entry.animation;

        HandleObjectReveals(index);
        
        // Trigger animation (always plays talking animation while typing)
        if (triggerAnimation)
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
    /// Triggers animations: always plays talking animation while typing, JumpCloser on rat animator if specified,
    /// or the specified animation otherwise.
    /// </summary>
    private void TriggerAnimation(DialogueAnimation animation)
    {
        // Enable main animator if needed
        if (_animator != null && !_animator.enabled)
        {
            _animator.enabled = true;
        }

        if (animation == DialogueAnimation.JumpCloser)
        {
            // Play JumpCloser on rat animator
            if (_ratAnimator != null)
            {
                if (!_ratAnimator.enabled)
                {
                    _ratAnimator.enabled = true;
                }
                _ratAnimator.Play("JumpCloser");
            }
            
            // Also play talking animation on main animator
            if (_animator != null)
            {
                _animator.Play("GodTalk");
            }
        }
        else if (animation != DialogueAnimation.None)
        {
            // Play the specified animation on main animator
            if (_animator != null)
            {
                _animator.Play(animation.ToString());
            }
        }
        else
        {
            // Play talking animation
            if (_animator != null)
            {
                _animator.Play("GodTalk");
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
        StartTypingAudioLoop();
        
        foreach (char character in text)
        {
            _dialogueText.text += character;
            yield return new WaitForSeconds(_typingSpeed);
        }
        
        _isTyping = false;
        _typingCoroutine = null;
        StopTypingAudioLoop();
        
        // Stop talking animation when typing is complete
        StopAnimation();
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
        StopTypingAudioLoop();
        
        // Stop talking animation when typing is skipped
        StopAnimation();
    }

    /// <summary>
    /// Starts the looping coroutine that plays typing blip sounds.
    /// </summary>
    private void StartTypingAudioLoop()
    {
        if (_typingAudioSource == null || _typingBlipClip == null || _typingAudioCoroutine != null)
        {
            return;
        }

        _typingAudioCoroutine = StartCoroutine(TypingAudioLoop());
    }

    /// <summary>
    /// Stops the typing audio coroutine and any currently playing audio.
    /// </summary>
    private void StopTypingAudioLoop()
    {
        if (_typingAudioCoroutine != null)
        {
            StopCoroutine(_typingAudioCoroutine);
            _typingAudioCoroutine = null;
        }

        if (_typingAudioSource != null)
        {
            _typingAudioSource.Stop();
        }
    }

    /// <summary>
    /// Coroutine that repeatedly plays the typing blip audio with a delay and random pitch.
    /// </summary>
    private IEnumerator TypingAudioLoop()
    {
        float delay = Mathf.Max(0f, _typingBlipDelay);

        while (_isTyping)
        {
            PlayTypingBlip();

            if (delay <= 0f)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(delay);
            }
        }

        _typingAudioCoroutine = null;
    }

    /// <summary>
    /// Plays a single typing blip with randomized pitch.
    /// </summary>
    private void PlayTypingBlip()
    {
        if (_typingAudioSource == null || _typingBlipClip == null)
        {
            return;
        }

        float minPitch = Mathf.Min(_typingPitchMin, _typingPitchMax);
        float maxPitch = Mathf.Max(_typingPitchMin, _typingPitchMax);
        _typingAudioSource.pitch = Random.Range(minPitch, maxPitch);
        _typingAudioSource.PlayOneShot(_typingBlipClip);
    }

    /// <summary>
    /// Shows the dialogue UI container, if assigned.
    /// </summary>
    private void ShowDialogueUI()
    {
        if (_dialogueContainer != null && !_dialogueContainer.activeSelf)
        {
            _dialogueContainer.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the dialogue UI container, if assigned.
    /// </summary>
    private void HideDialogueUI()
    {
        if (_dialogueContainer != null && _dialogueContainer.activeSelf)
        {
            _dialogueContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Switches to the configured scene after the dialogue finishes.
    /// </summary>
    private void SwitchToNextScene()
    {
        if (_sceneSwitcher != null && !string.IsNullOrEmpty(_sceneToLoadOnComplete))
        {
            _sceneSwitcher.GoToScene(_sceneToLoadOnComplete);
        }
        else if (_sceneSwitcher == null)
        {
            Debug.LogWarning("CutsceneManager: Scene switcher reference not assigned.");
        }
    }

    /// <summary>
    /// Checks whether any UI objects should be revealed for the given dialogue index.
    /// </summary>
    private void HandleObjectReveals(int dialogueIndex)
    {
        // Cutscene 1 objects (stick, den, worm, grass, hawk, coyote) - only appear if cutscene1 is unchecked (false)
        if (!_hideCutscene1Objects)
        {
            if (dialogueIndex == _stickDialogueIndex)
            {
                FadeInGameObject(_stickObject);
            }

            if (dialogueIndex == _denDialogueIndex)
            {
                FadeInGameObject(_denObject);
            }

            if (dialogueIndex == _wormDialogueIndex)
            {
                FadeInGameObject(_wormObject);
            }

            if (dialogueIndex == _grassDialogueIndex)
            {
                FadeInGameObject(_grassObject);
            }

            if (dialogueIndex == _hawkDialogueIndex)
            {
                FadeInGameObject(_hawkObject);
            }

            if (dialogueIndex == _coyoteDialogueIndex)
            {
                FadeInGameObject(_coyoteObject);
            }
        }

        // Cutscene 2 object (crowd) - only appears if cutscene2 is unchecked (false)
        if (!_hideCutscene2Objects)
        {
            if (dialogueIndex == _crowdDialogueIndex)
            {
                FadeInGameObject(_crowdObject);
            }
        }

        // Cutscene 3 object (bones) - only appears if cutscene3 is unchecked (false)
        if (!_hideCutscene3Objects)
        {
            if (dialogueIndex == _bonesDialogueIndex)
            {
                FadeInGameObject(_bonesObject);
            }
        }
    }

    /// <summary>
    /// Fades in the specified UI GameObject using a CanvasGroup.
    /// </summary>
    private void FadeInGameObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        target.SetActive(true);

        // Stop any existing fade coroutine on this object.
        if (_activeFadeCoroutines.TryGetValue(target, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        float duration = Mathf.Max(0.001f, _objectFadeDuration);
        Coroutine fadeCoroutine = StartCoroutine(FadeCanvasGroupIn(canvasGroup, duration, target));
        _activeFadeCoroutines[target] = fadeCoroutine;
    }

    /// <summary>
    /// Coroutine that fades in a CanvasGroup over the specified duration.
    /// </summary>
    private IEnumerator FadeCanvasGroupIn(CanvasGroup canvasGroup, float duration, GameObject key)
    {
        float elapsed = 0f;
        float startAlpha = 0f;
        float endAlpha = 1f;

        canvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        if (_activeFadeCoroutines.ContainsKey(key))
        {
            _activeFadeCoroutines[key] = null;
        }
    }

    /// <summary>
    /// Stops the talking animation (GodTalk) and transitions to Idle animation.
    /// </summary>
    private void StopAnimation()
    {
        // Stop any animation-related coroutines
        if (_animationWaitCoroutine != null)
        {
            StopCoroutine(_animationWaitCoroutine);
            _animationWaitCoroutine = null;
        }
        
        // Transition from GodTalk to Idle animation
        if (_animator != null && _animator.enabled)
        {
            // Check if GodTalk is currently playing on layer 0
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("GodTalk"))
            {
                // Transition to Idle animation
                _animator.Play("Idle");
            }
        }
        
        // Don't disable rat animator - let JumpCloser and other animations finish
    }
}

