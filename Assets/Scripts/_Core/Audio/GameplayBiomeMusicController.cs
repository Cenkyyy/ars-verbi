using System;
using UnityEngine;

/// <summary>
/// Schedules non-looping exploration music for the player's current biome. Biome changes are confirmed
/// only after a stability delay, then the next track is crossfaded in at the same normalized position.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayBiomeMusicController : MonoBehaviour
{
    [Serializable]
    private struct BiomeMusicTrack
    {
        public WorldBiomeType Biome;
        public AudioClip Clip;
    }

    [Header("References")]
    [SerializeField] private WorldGenerationController _worldGenerator;
    [SerializeField] private Player _player;
    [SerializeField] private Transform _playerTransform;

    [Header("Biome Tracks")]
    [SerializeField] private BiomeMusicTrack[] _biomeTracks;
    [SerializeField, Range(0f, 1f)] private float _biomeMusicVolume = 0.7f;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float _biomeCheckIntervalSeconds = 0.5f;
    [SerializeField, Min(0f)] private float _minSilenceDurationSeconds = 40f;
    [SerializeField, Min(0f)] private float _maxSilenceDurationSeconds = 120f;
    [SerializeField, Min(0f)] private float _biomeChangeDebounceSeconds = 1.5f;
    [SerializeField, Min(0f)] private float _crossfadeDurationSeconds = 0.5f;
    [SerializeField, Min(0f)] private float _stopFadeDurationSeconds = 1f;

    private WorldBiomeType _confirmedBiome = WorldBiomeType.None;
    private WorldBiomeType _candidateBiome = WorldBiomeType.None;
    private AudioClip _ownedClip;
    private float _candidateDetectedTime;
    private float _nextBiomeCheckTime;
    private float _scheduledStartTime = float.PositiveInfinity;
    private bool _ownsActiveMusic;
    private bool _playbackSuppressed;

    private void OnEnable()
    {
        GameStateManager.PauseChanged += HandlePauseChanged;
    }

    private void Start()
    {
        if (_player != null && _player.Data != null)
            _player.Data.OnHealthChanged += HandlePlayerHealthChanged;
    }

    private void Update()
    {
        if (GameStateManager.IsGamePaused || _playbackSuppressed || Time.time < _nextBiomeCheckTime)
            return;

        _nextBiomeCheckTime = Time.time + _biomeCheckIntervalSeconds;

        if (AudioManager.Instance == null)
            return;

        HandleOwnedMusicCompletion();

        if (!TryResolveCurrentBiome(out WorldBiomeType detectedBiome))
            return;

        UpdateConfirmedBiome(detectedBiome);

        if (_confirmedBiome == WorldBiomeType.None || _ownsActiveMusic)
            return;

        EnsureTrackScheduled();
        if (Time.time < _scheduledStartTime || !TryGetClip(_confirmedBiome, out AudioClip clip))
            return;

        PlayBiomeMusic(clip);
    }

    private void OnDisable()
    {
        GameStateManager.PauseChanged -= HandlePauseChanged;

        if (_player != null && _player.Data != null)
            _player.Data.OnHealthChanged -= HandlePlayerHealthChanged;

        StopGameplayMusic();
    }

    public void StopGameplayMusic()
    {
        _playbackSuppressed = true;
        ClearCandidateBiome();
        ClearScheduledStart();

        if (AudioManager.Instance == null || _ownedClip == null || !AudioManager.Instance.IsCurrentMusic(_ownedClip))
        {
            ClearOwnedMusic();
            return;
        }

        AudioManager.Instance.FadeOutMusic(_stopFadeDurationSeconds);
        ClearOwnedMusic();
    }

    private void UpdateConfirmedBiome(WorldBiomeType detectedBiome)
    {
        if (_confirmedBiome == WorldBiomeType.None)
        {
            ConfirmBiome(detectedBiome);
            return;
        }

        if (detectedBiome == _confirmedBiome)
        {
            ClearCandidateBiome();
            return;
        }

        if (detectedBiome != _candidateBiome)
        {
            _candidateBiome = detectedBiome;
            _candidateDetectedTime = Time.time;
            return;
        }

        if (Time.time - _candidateDetectedTime < _biomeChangeDebounceSeconds)
            return;

        ConfirmBiome(detectedBiome);
    }

    private void ConfirmBiome(WorldBiomeType biome)
    {
        _confirmedBiome = biome;
        ClearCandidateBiome();

        if (_ownsActiveMusic)
            SwitchActiveBiomeMusic(biome);
    }

    private void SwitchActiveBiomeMusic(WorldBiomeType biome)
    {
        if (AudioManager.Instance == null || _ownedClip == null || !TryGetClip(biome, out AudioClip nextClip))
            return;

        if (!AudioManager.Instance.TryGetCurrentMusicNormalizedPosition(_ownedClip, out float normalizedPlaybackPosition))
        {
            ClearOwnedMusic();
            EnsureTrackScheduled();
            return;
        }

        AudioManager.Instance.CrossfadeGameplayMusic(
            nextClip,
            normalizedPlaybackPosition,
            _crossfadeDurationSeconds,
            _biomeMusicVolume);
        _ownedClip = nextClip;
    }

    private void PlayBiomeMusic(AudioClip clip)
    {
        AudioManager.Instance.PlayGameplayMusic(
            clip,
            shouldFadeIn: true,
            fadeDuration: _crossfadeDurationSeconds,
            volumeScale: _biomeMusicVolume,
            shouldLoop: false);

        _ownsActiveMusic = true;
        _ownedClip = clip;
        ClearScheduledStart();
    }

    private void HandleOwnedMusicCompletion()
    {
        if (!_ownsActiveMusic || _ownedClip == null)
            return;

        if (!AudioManager.Instance.IsCurrentMusic(_ownedClip))
        {
            ClearOwnedMusic();
            EnsureTrackScheduled();
            return;
        }

        if (AudioManager.Instance.IsPlayingMusic(_ownedClip))
            return;

        ClearOwnedMusic();
        ScheduleNextTrack();
    }

    private void HandlePlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            StopGameplayMusic();
            return;
        }

        if (!_playbackSuppressed)
            return;

        _playbackSuppressed = false;
        EnsureTrackScheduled();
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (AudioManager.Instance == null)
            return;

        if (isPaused)
        {
            AudioManager.Instance.PauseMusic();
            return;
        }

        AudioManager.Instance.ResumeMusic();
    }

    private bool TryResolveCurrentBiome(out WorldBiomeType biome)
    {
        biome = WorldBiomeType.None;

        if (_worldGenerator == null || _playerTransform == null || !_worldGenerator.RuntimeState.IsInitialized)
            return false;

        Vector2Int tile = _worldGenerator.RuntimeState.ResolveTileFromWorld(_playerTransform.position);
        biome = _worldGenerator.CurrentWorldData.GetTile(tile.x, tile.y).Biome;
        return biome != WorldBiomeType.None;
    }

    private bool TryGetClip(WorldBiomeType biome, out AudioClip clip)
    {
        if (_biomeTracks != null)
        {
            for (int i = 0; i < _biomeTracks.Length; i++)
            {
                if (_biomeTracks[i].Biome != biome)
                    continue;

                clip = _biomeTracks[i].Clip;
                return clip != null;
            }
        }

        clip = null;
        return false;
    }

    private void EnsureTrackScheduled()
    {
        if (!float.IsPositiveInfinity(_scheduledStartTime))
            return;

        ScheduleNextTrack();
    }

    private void ScheduleNextTrack()
    {
        float minimum = Mathf.Min(_minSilenceDurationSeconds, _maxSilenceDurationSeconds);
        float maximum = Mathf.Max(_minSilenceDurationSeconds, _maxSilenceDurationSeconds);
        _scheduledStartTime = Time.time + UnityEngine.Random.Range(minimum, maximum);
    }

    private void ClearOwnedMusic()
    {
        _ownsActiveMusic = false;
        _ownedClip = null;
    }

    private void ClearCandidateBiome()
    {
        _candidateBiome = WorldBiomeType.None;
        _candidateDetectedTime = 0f;
    }

    private void ClearScheduledStart()
    {
        _scheduledStartTime = float.PositiveInfinity;
    }
}
