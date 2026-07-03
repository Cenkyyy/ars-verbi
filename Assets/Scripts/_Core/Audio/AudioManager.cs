using System.Collections;
using UnityEngine;

/// <summary>
/// Persistent audio service for master volume, menu and gameplay music, and shared UI sound effects.
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    private const string MasterVolumePrefKey = "Audio.MasterVolume";

    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _standbyMusicSource;
    [SerializeField] private AudioClip _menuMusicClip;

    [Header("UI SFX")]
    [SerializeField] private AudioSource _uiSfxSource;
    [SerializeField] private AudioClip _defaultUiHoverClip;
    [SerializeField] private AudioClip _defaultUiClickClip;

    [field: Header("Volume")]
    [field: SerializeField, Range(0f, 1f)] public float MasterVolume { get; private set; } = 1f;

    private float _musicSourceDefaultVolume = 1f;
    private float _currentMusicTargetVolume = 1f;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSourceDefaultVolume = Mathf.Clamp01(_musicSource.volume);
        _currentMusicTargetVolume = _musicSourceDefaultVolume;
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumePrefKey, MasterVolume);
        AudioListener.volume = MasterVolume;
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;

        PlayerPrefs.SetFloat(MasterVolumePrefKey, MasterVolume);
        PlayerPrefs.Save();
    }

    public void PlayMenuMusic(bool shouldFadeIn, bool shouldFadeOut, float fadeDuration, bool shouldLoop = true)
    {
        if (shouldFadeOut)
        {
            FadeOutMusic(fadeDuration);
            return;
        }

        bool shouldStartFade = shouldFadeIn && ShouldStartNewMusic(_menuMusicClip);
        PlayMusic(_menuMusicClip, shouldLoop, shouldStartFade, _musicSourceDefaultVolume);

        if (shouldStartFade)
            FadeInMusic(fadeDuration);
    }

    public void PlayGameplayMusic(AudioClip clip, bool shouldFadeIn, float fadeDuration, float volumeScale, bool shouldLoop = false)
    {
        bool shouldStartFade = shouldFadeIn && ShouldStartNewMusic(clip);
        PlayMusic(clip, shouldLoop, shouldStartFade, ResolveMusicVolume(volumeScale));

        if (shouldStartFade)
            FadeInMusic(fadeDuration);
    }

    /// <summary>
    /// Crossfades from the active music source to the serialized standby source. Both sources play during
    /// the transition because a single AudioSource cannot overlap two clips. The incoming clip begins at
    /// the supplied normalized position, its volume rises while the outgoing volume falls, and the source
    /// references swap afterward so the stopped outgoing source can be reused for the next crossfade.
    /// </summary>
    public void CrossfadeGameplayMusic(AudioClip clip, float normalizedPlaybackPosition, float fadeDuration, float volumeScale)
    {
        if (clip == null || _musicSource == null || _standbyMusicSource == null)
            return;

        float targetVolume = ResolveMusicVolume(volumeScale);
        if (IsCurrentMusic(clip))
        {
            _currentMusicTargetVolume = targetVolume;
            _musicSource.volume = targetVolume;
            return;
        }

        StopFadeCoroutineIfRunning();

        AudioSource outgoingSource = _musicSource;
        AudioSource incomingSource = _standbyMusicSource;
        PrepareSourceForPlayback(incomingSource, clip, shouldLoop: false, volume: 0f);
        SetNormalizedPlaybackPosition(incomingSource, normalizedPlaybackPosition);
        incomingSource.Play();

        _musicSource = incomingSource;
        _standbyMusicSource = outgoingSource;
        _currentMusicTargetVolume = targetVolume;

        if (fadeDuration <= 0f || !outgoingSource.isPlaying)
        {
            StopSource(outgoingSource);
            incomingSource.volume = targetVolume;
            return;
        }

        _fadeCoroutine = StartCoroutine(CrossfadeMusicCoroutine(incomingSource, outgoingSource, targetVolume, fadeDuration));
    }

    public bool TryGetCurrentMusicNormalizedPosition(AudioClip clip, out float normalizedPlaybackPosition)
    {
        normalizedPlaybackPosition = 0f;
        if (!IsCurrentMusic(clip) || clip.samples <= 0)
            return false;

        normalizedPlaybackPosition = Mathf.Clamp01((float)_musicSource.timeSamples / clip.samples);
        return true;
    }

    public bool IsCurrentMusic(AudioClip clip)
    {
        return clip != null && _musicSource != null && _musicSource.clip == clip;
    }

    public bool IsPlayingMusic(AudioClip clip)
    {
        return IsCurrentMusic(clip) && _musicSource.isPlaying;
    }

    public void PlayUiHoverSfx()
    {
        PlayUiSfx(_defaultUiHoverClip);
    }

    public void PlayUiClickSfx()
    {
        PlayUiSfx(_defaultUiClickClip);
    }

    public void StopMusic()
    {
        StopFadeCoroutineIfRunning();
        StopSource(_standbyMusicSource);
        _musicSource.Stop();
        _currentMusicTargetVolume = _musicSourceDefaultVolume;
        _musicSource.volume = _musicSourceDefaultVolume;
    }

    public void PauseMusic()
    {
        StopFadeCoroutineIfRunning();
        StopSource(_standbyMusicSource);
        _musicSource.volume = _currentMusicTargetVolume;
        _musicSource.Pause();
    }

    public void ResumeMusic()
    {
        _musicSource.volume = _currentMusicTargetVolume;
        _musicSource.UnPause();
    }

    public void FadeOutMusic(float fadeDuration)
    {
        FadeMusic(0f, fadeDuration, true);
    }

    private void FadeInMusic(float fadeDuration)
    {
        if (_musicSource.isPlaying && fadeDuration > 0f)
            _musicSource.volume = 0f;

        FadeMusic(_currentMusicTargetVolume, fadeDuration, false);
    }

    private bool ShouldStartNewMusic(AudioClip clip)
    {
        if (clip == null)
            return false;

        return _musicSource.clip != clip || !_musicSource.isPlaying;
    }

    private void PlayMusic(AudioClip clip, bool shouldLoop, bool startSilent, float targetVolume)
    {
        if (clip == null)
        {
            StopMusic();
            return;
        }

        _currentMusicTargetVolume = targetVolume;

        if (_musicSource.clip == clip && _musicSource.isPlaying)
        {
            _musicSource.loop = shouldLoop;
            _musicSource.volume = startSilent ? 0f : targetVolume;
            return;
        }

        StopFadeCoroutineIfRunning();
        StopSource(_standbyMusicSource);
        PrepareSourceForPlayback(_musicSource, clip, shouldLoop, startSilent ? 0f : targetVolume);
        _musicSource.Play();
    }

    private void PlayUiSfx(AudioClip clip)
    {
        if (_uiSfxSource == null || clip == null)
            return;

        _uiSfxSource.Stop();
        _uiSfxSource.clip = clip;
        _uiSfxSource.Play();
    }

    private void FadeMusic(float targetVolume, float fadeDuration, bool stopMusicOnComplete)
    {
        StopFadeCoroutineIfRunning();
        StopSource(_standbyMusicSource);
        _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(targetVolume, fadeDuration, stopMusicOnComplete));
    }

    private IEnumerator FadeMusicCoroutine(float targetVolume, float fadeDuration, bool stopMusicOnComplete)
    {
        if (!TryPrepareMusicSourceForFade())
        {
            _fadeCoroutine = null;
            yield break;
        }

        if (fadeDuration <= 0f)
        {
            _musicSource.volume = targetVolume;

            if (stopMusicOnComplete)
                StopMusic();

            _fadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        float startVolume = _musicSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        _musicSource.volume = targetVolume;

        if (stopMusicOnComplete)
            StopMusic();

        _fadeCoroutine = null;
    }

    private IEnumerator CrossfadeMusicCoroutine(AudioSource incomingSource, AudioSource outgoingSource, float targetVolume, float fadeDuration)
    {
        float elapsed = 0f;
        float outgoingStartVolume = outgoingSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            incomingSource.volume = Mathf.Sin(t * Mathf.PI * 0.5f) * targetVolume;
            outgoingSource.volume = Mathf.Cos(t * Mathf.PI * 0.5f) * outgoingStartVolume;
            yield return null;
        }

        incomingSource.volume = targetVolume;
        StopSource(outgoingSource);
        _fadeCoroutine = null;
    }

    private bool TryPrepareMusicSourceForFade()
    {
        if (_musicSource == null || _musicSource.clip == null)
            return false;

        if (!_musicSource.isPlaying)
            _musicSource.UnPause();

        return _musicSource.isPlaying;
    }

    private void StopFadeCoroutineIfRunning()
    {
        if (_fadeCoroutine == null)
            return;

        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
    }

    private float ResolveMusicVolume(float volumeScale)
    {
        return _musicSourceDefaultVolume * Mathf.Clamp01(volumeScale);
    }


    private static void PrepareSourceForPlayback(AudioSource source, AudioClip clip, bool shouldLoop, float volume)
    {
        source.Stop();
        source.clip = clip;
        source.loop = shouldLoop;
        source.volume = volume;
    }

    private static void SetNormalizedPlaybackPosition(AudioSource source, float normalizedPlaybackPosition)
    {
        if (source.clip == null || source.clip.samples <= 1)
            return;

        int sample = Mathf.RoundToInt(Mathf.Clamp01(normalizedPlaybackPosition) * (source.clip.samples - 1));
        source.timeSamples = sample;
    }

    private static void StopSource(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.volume = 0f;
    }
}
