using UnityEngine;

/// <summary>
/// Simple sound effect player for hawk-related audio. Plays a sound effect once at a specified volume.
/// </summary>
public class HawkSFXPlayer : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("The sound effect clip to play.")]
    [SerializeField] private AudioClip _soundEffect;
    
    [Tooltip("The volume to play the sound effect at (0.0 to 1.0).")]
    [SerializeField] private float _volume = 1.0f;
    
    private AudioSource _audioSource;
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    /// <summary>
    /// Plays the sound effect once at the specified volume.
    /// </summary>
    public void PlaySound()
    {
        if (_soundEffect == null)
        {
            Debug.LogWarning("HawkSFXPlayer: Sound effect is not assigned!");
            return;
        }
        
        if (_audioSource == null)
        {
            Debug.LogError("HawkSFXPlayer: AudioSource component not found!");
            return;
        }
        
        _audioSource.PlayOneShot(_soundEffect, _volume);
    }
}

