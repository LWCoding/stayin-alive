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
        Damage, 
        Hawk, 
        Coyote,
        Walk, 
        Dig,
    }

    public enum MusicType
    {
        Menu,
        Spring, 
        Summer, 
        Autumn, 
        Winter,
        Danger,
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

    [Header("Audio Lists")]
    public List<AudioEntrySFX> sfxList = new();
    public List<AudioEntryMusic> musicList = new();

    // ======================
    // 🗂 DICTIONARIES
    // ======================

    private Dictionary<SFXType, AudioEntrySFX> sfxDict = new();
    private Dictionary<MusicType, AudioEntryMusic> musicDict = new();

    private Coroutine currentMusicFade;

    // ======================
    // 🔧 SETUP
    // ======================

    protected override void Awake()
    {
        base.Awake();
        BuildDictionaries();
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

    public void PlayMusic(MusicType type, float volume = -1f, float fadeTime = 1f)
    {
        if (!musicDict.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"AudioManager: No music entry found for {type}");
            return;
        }

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
    }

    // ======================
    // 🔊 SFX COMMANDS
    // ======================

    public void PlaySFX(SFXType type, float volume = -1f)
    {
        if (!sfxDict.TryGetValue(type, out var entry))
        {
            Debug.LogWarning($"AudioManager: No SFX entry found for {type}");
            return;
        }

        float finalVolume = (volume >= 0f) ? Mathf.Clamp01(volume) : entry.volume;
        sfxSource.PlayOneShot(entry.clip, finalVolume);
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
    }
}
