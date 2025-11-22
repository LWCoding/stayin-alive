using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    // ======================
    // 🎚 ENUM TYPES
    // ======================

    public enum SFXType
    {
        None = 0,
        Hawk = 1,
        Coyote = 2,
        Walk = 3,
        Dig = 4,
        KangDeath = 5, 
        BunnyDeath = 6,
        Eat = 7, 
        UI = 8, 
        Hunger = 9,
        Spring = 10, 
        Summer = 11, 
        Fall = 12, 
        Winter = 13,
        FullInventory = 14, 
        Planting = 15,  
        Hover = 16,
    }

    public enum MusicType
    {
        Menu = 1,
        Spring = 2,
        Summer = 3,
        Autumn = 4,
        Winter = 5,
        Danger = 6,
    }

    // ======================
    // 🎵 AUDIO ENTRY CLASSES
    // ======================

    [System.Serializable]
    public class AudioEntrySFX
    {
        public SFXType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [System.Serializable]
    public class AudioEntryMusic
    {
        public MusicType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    // ======================
    // 🧰 PUBLIC FIELDS
    // ======================

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    [Tooltip("Optional dedicated AudioSource for season SFX that need fade-out functionality. If not assigned, will use sfxSource without fade.")]
    public AudioSource seasonSFXSource;

    [Header("Audio Lists")]
    public List<AudioEntrySFX> sfxList = new();
    public List<AudioEntryMusic> musicList = new();

    // ======================
    // 🗂 DICTIONARIES
    // ======================

    private Dictionary<SFXType, AudioEntrySFX> sfxDict = new();
    private Dictionary<MusicType, AudioEntryMusic> musicDict = new();

    private Coroutine currentMusicFade;
    private MusicType currentMusicType = MusicType.Menu;
    private Coroutine currentSeasonSFXFade;
    private SFXType currentSeasonSFXType = SFXType.None;
    private bool _isHungerSoundPlaying = false;

    [Header("Startup Settings")]
    [SerializeField] private bool autoplayMusicOnStart = false;
    [SerializeField] private MusicType startupMusicType = MusicType.Menu;

    // ======================
    // 🔧 SETUP
    // ======================

    protected override void Awake()
    {
        base.Awake();
        BuildDictionaries();

        if (autoplayMusicOnStart)
        {
            PlayMusic(startupMusicType);
        }
    }

    private void BuildDictionaries()
    {
        sfxDict.Clear();
        musicDict.Clear();

        // Build SFX dictionary
        foreach (var entry in sfxList)
        {
            if (entry != null && entry.clip != null)
                sfxDict[entry.type] = entry;
        }

        // Build Music dictionary
        foreach (var entry in musicList)
        {
            if (entry != null && entry.clip != null)
                musicDict[entry.type] = entry;
        }
    }

    // ======================
    // 🎵 MUSIC COMMANDS
    // ======================

    public void PlayMusic(MusicType type, float volume = -1f, float fadeTime = 0.5f)
    {
        if (!musicDict.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"AudioManager: No music entry found for {type}");
            return;
        }

        // Don't restart if the same music is already playing
        if (musicSource.isPlaying && currentMusicType == type && musicSource.clip == entry.clip)
        {
            return;
        }

        currentMusicType = type;
        float targetVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;

        if (currentMusicFade != null)
            StopCoroutine(currentMusicFade);

        currentMusicFade = StartCoroutine(FadeToNewMusic(entry.clip, targetVolume, fadeTime));
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip, float targetVolume, float fadeTime)
    {
        // Fade out old music
        if (musicSource.isPlaying && musicSource.clip != null)
        {
            float startVol = musicSource.volume;
            float t = 0f;

            while (t < fadeTime)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }

            musicSource.Stop();
        }

        // Assign new music
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        // Fade in
        float tIn = 0f;
        while (tIn < fadeTime)
        {
            tIn += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, tIn / fadeTime);
            yield return null;
        }

        musicSource.volume = targetVolume;
        currentMusicFade = null;
    }

    public void StopMusic(float fadeTime = 1f)
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        if (currentMusicFade != null)
            StopCoroutine(currentMusicFade);

        currentMusicFade = StartCoroutine(FadeOutMusic(fadeTime));
    }

    private IEnumerator FadeOutMusic(float fadeTime)
    {
        float startVol = musicSource.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        currentMusicFade = null;
        currentMusicType = MusicType.Menu;
    }

    // ======================
    // 🔊 SFX COMMANDS
    // ======================

    public void PlaySFX(SFXType type, float volume = -1f)
    {
        if (type == SFXType.None)
        {
            return;
        }

        if (!sfxDict.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"AudioManager: No SFX entry found for {type}");
            return;
        }

        // Check if hunger sound is already playing
        if (type == SFXType.Hunger && _isHungerSoundPlaying)
        {
            return;
        }

        float finalVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;
        sfxSource.PlayOneShot(entry.clip, finalVolume);
        
        // Track hunger sound playback
        if (type == SFXType.Hunger)
        {
            _isHungerSoundPlaying = true;
            StartCoroutine(ResetHungerSoundFlag(entry.clip.length));
        }
    }
    
    /// <summary>
    /// Checks if a hunger sound is currently playing.
    /// </summary>
    public bool IsHungerSoundPlaying()
    {
        return _isHungerSoundPlaying;
    }
    
    /// <summary>
    /// Coroutine to reset the hunger sound flag after the clip duration.
    /// </summary>
    private IEnumerator ResetHungerSoundFlag(float duration)
    {
        yield return new WaitForSeconds(duration);
        _isHungerSoundPlaying = false;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void StopSFX()
    {
        if (sfxSource != null)
            sfxSource.Stop();
    }

    public void StopAllAudio()
    {
        StopMusic();
        StopSFX();
        StopSeasonSFX();
    }

    /// <summary>
    /// Plays a season SFX with fade-out support. If a season SFX is already playing, it will fade out before playing the new one.
    /// </summary>
    public void PlaySeasonSFX(SFXType type, float volume = -1f, float fadeTime = 0.5f)
    {
        // Check if this is a valid season SFX type
        if (type != SFXType.Spring && type != SFXType.Summer && type != SFXType.Fall && type != SFXType.Winter)
        {
            Debug.LogWarning($"AudioManager: {type} is not a season SFX type. Use PlaySFX instead.");
            PlaySFX(type, volume);
            return;
        }

        if (!sfxDict.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"AudioManager: No SFX entry found for {type}");
            return;
        }

        // Use dedicated season source if available, otherwise fall back to regular sfxSource
        AudioSource sourceToUse = seasonSFXSource != null ? seasonSFXSource : sfxSource;
        
        if (sourceToUse == null)
        {
            Debug.LogWarning("AudioManager: No audio source available for season SFX.");
            return;
        }

        // Don't restart if the same season SFX is already playing
        if (sourceToUse.isPlaying && currentSeasonSFXType == type && sourceToUse.clip == entry.clip)
        {
            return;
        }

        currentSeasonSFXType = type;
        float targetVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;

        // Stop any existing fade coroutine
        if (currentSeasonSFXFade != null)
        {
            StopCoroutine(currentSeasonSFXFade);
        }

        currentSeasonSFXFade = StartCoroutine(FadeToNewSeasonSFX(entry.clip, targetVolume, fadeTime, sourceToUse));
    }

    private IEnumerator FadeToNewSeasonSFX(AudioClip newClip, float targetVolume, float fadeTime, AudioSource source)
    {
        // Fade out old season SFX if playing
        if (source.isPlaying && source.clip != null)
        {
            float startVol = source.volume;
            float t = 0f;

            while (t < fadeTime)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }

            source.Stop();
        }

        // Assign new season SFX
        source.clip = newClip;
        source.volume = 0f;
        source.loop = false; // Season SFX typically don't loop
        source.Play();

        // Fade in new season SFX
        float tIn = 0f;
        while (tIn < fadeTime && source.isPlaying)
        {
            tIn += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, tIn / fadeTime);
            yield return null;
        }

        source.volume = targetVolume;
        currentSeasonSFXFade = null;
    }

    /// <summary>
    /// Stops the currently playing season SFX with a fade-out.
    /// </summary>
    public void StopSeasonSFX(float fadeTime = 0.7f)
    {
        AudioSource sourceToUse = seasonSFXSource != null ? seasonSFXSource : sfxSource;
        
        if (sourceToUse == null || !sourceToUse.isPlaying)
        {
            currentSeasonSFXType = SFXType.None;
            return;
        }

        if (currentSeasonSFXFade != null)
        {
            StopCoroutine(currentSeasonSFXFade);
        }

        currentSeasonSFXFade = StartCoroutine(FadeOutSeasonSFX(fadeTime, sourceToUse));
    }

    private IEnumerator FadeOutSeasonSFX(float fadeTime, AudioSource source)
    {
        float startVol = source.volume;
        float t = 0f;

        while (t < fadeTime && source.isPlaying)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        source.Stop();
        currentSeasonSFXFade = null;
        currentSeasonSFXType = SFXType.None;
    }
}
