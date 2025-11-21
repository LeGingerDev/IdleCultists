using Audio.Managers;
using LGD.Core;
using LGD.Core.Events;
using LGD.Core.Singleton;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the Boombox music player system
/// Tracks which tracks are unlocked and handles playback delegation to AudioManager
/// </summary>
public class BoomboxManager : MonoSingleton<BoomboxManager>
{
    private BoomboxTrackPurchasable _currentTrack = null;
    private PurchasableRegistry _purchasableRegistry;
    private BoomboxSaveProvider _saveProvider;
    private bool _isInitialized = false;

    // BPM Tracking
    private Coroutine _bpmTrackerCoroutine = null;
    private const float RESYNC_INTERVAL = 30f; // Resync every 30 seconds

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        StartCoroutine(InitializeAsync());
    }

    private IEnumerator InitializeAsync()
    {
        // Wait for PurchasableManager to initialize
        yield return new WaitUntil(() => PurchasableManager.Instance != null && PurchasableManager.Instance.IsInitialized());

        // Get the purchasable registry
        _purchasableRegistry = RegistryManager.Instance.GetRegistry<BasePurchasable>() as PurchasableRegistry;

        if (_purchasableRegistry == null)
        {
            DebugManager.Error("[Boombox] Failed to get PurchasableRegistry!");
            yield break;
        }

        // Get save provider
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<BoomboxRuntimeData>() as BoomboxSaveProvider;

        if (_saveProvider != null)
        {
            // Load saved boombox state
            yield return _saveProvider.Load();
            DebugManager.Log("[Boombox] <color=cyan>Boombox save data loaded</color>");
        }
        else
        {
            DebugManager.Warning("[Boombox] BoomboxSaveProvider not found! Boombox state will not persist.");
        }

        _isInitialized = true;
        ServiceBus.Publish(BoomboxEventIds.ON_BOOMBOX_INITIALIZED, this);
        DebugManager.Log("[Boombox] <color=cyan>Boombox Manager initialized</color>");
    }

    #region Track Management

    /// <summary>
    /// Gets all available boombox tracks (both locked and unlocked)
    /// </summary>
    public List<BoomboxTrackPurchasable> GetAllTracks()
    {
        if (_purchasableRegistry == null)
            return new List<BoomboxTrackPurchasable>();

        return _purchasableRegistry.GetAllItems()
            .OfType<BoomboxTrackPurchasable>()
            .ToList();
    }

    /// <summary>
    /// Gets only the unlocked tracks (purchased or default unlocked)
    /// </summary>
    public List<BoomboxTrackPurchasable> GetUnlockedTracks()
    {
        return GetAllTracks()
            .Where(track => track.IsUnlocked())
            .ToList();
    }

    /// <summary>
    /// Checks if a specific track is unlocked
    /// </summary>
    public bool IsTrackUnlocked(BoomboxTrackPurchasable track)
    {
        if (track == null)
            return false;

        return track.IsUnlocked();
    }

    /// <summary>
    /// Gets the currently playing track (null if nothing playing)
    /// </summary>
    public BoomboxTrackPurchasable GetCurrentTrack()
    {
        return _currentTrack;
    }

    /// <summary>
    /// Checks if any track is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return _currentTrack != null;
    }

    #endregion

    #region Playback Control

    /// <summary>
    /// Plays the specified track (delegates to AudioManager)
    /// </summary>
    public void PlayTrack(BoomboxTrackPurchasable track)
    {
        if (track == null)
        {
            DebugManager.Warning("[Boombox] Attempted to play null track");
            return;
        }

        if (!IsTrackUnlocked(track))
        {
            DebugManager.Warning($"[Boombox] Attempted to play locked track: {track.displayName}");
            return;
        }

        if (track.audioClipSO == null)
        {
            DebugManager.Error($"[Boombox] Track {track.displayName} has no AudioClipSO assigned!");
            return;
        }

        // Store previous track for event
        BoomboxTrackPurchasable previousTrack = _currentTrack;

        // Update current track
        _currentTrack = track;

        // Delegate playback to AudioManager with crossfade
        AudioManager.Instance.PlayBGMTrack(track.audioClipSO, crossfade: true);

        // Start BPM tracker for beat-synced effects
        StartBPMTracker(track.audioClipSO.bpm);

        // Save state
        SaveCurrentState();

        // Publish events
        ServiceBus.Publish(BoomboxEventIds.ON_TRACK_STARTED, this, track);

        if (previousTrack != null && previousTrack != track)
        {
            ServiceBus.Publish(BoomboxEventIds.ON_TRACK_CHANGED, this, previousTrack, track);
        }

        DebugManager.Log($"[Boombox] <color=yellow>Now playing:</color> {track.displayName} by {track.artist}");
    }

    /// <summary>
    /// Stops the currently playing track
    /// </summary>
    public void StopTrack()
    {
        if (_currentTrack == null)
            return;

        // Stop BGM playback
        AudioManager.Instance.StopAllBGMTracks();

        // Stop BPM tracker
        StopBPMTracker();

        // Clear current track
        _currentTrack = null;

        // Save state
        SaveCurrentState();

        // Publish event
        ServiceBus.Publish(BoomboxEventIds.ON_TRACK_STOPPED, this);

        DebugManager.Log("[Boombox] <color=yellow>Playback stopped</color>");
    }

    /// <summary>
    /// Plays the next track in the unlocked tracks list
    /// </summary>
    public void NextTrack()
    {
        List<BoomboxTrackPurchasable> unlockedTracks = GetUnlockedTracks();

        if (unlockedTracks.Count == 0)
        {
            DebugManager.Warning("[Boombox] No unlocked tracks available");
            return;
        }

        // If nothing is playing, play the first track
        if (_currentTrack == null)
        {
            PlayTrack(unlockedTracks[0]);
            return;
        }

        // Find current track index
        int currentIndex = unlockedTracks.IndexOf(_currentTrack);

        if (currentIndex == -1)
        {
            // Current track not found in unlocked list, play first
            PlayTrack(unlockedTracks[0]);
            return;
        }

        // Play next track (loop back to start if at end)
        int nextIndex = (currentIndex + 1) % unlockedTracks.Count;
        PlayTrack(unlockedTracks[nextIndex]);
    }

    /// <summary>
    /// Plays the previous track in the unlocked tracks list
    /// </summary>
    public void PreviousTrack()
    {
        List<BoomboxTrackPurchasable> unlockedTracks = GetUnlockedTracks();

        if (unlockedTracks.Count == 0)
        {
            DebugManager.Warning("[Boombox] No unlocked tracks available");
            return;
        }

        // If nothing is playing, play the last track
        if (_currentTrack == null)
        {
            PlayTrack(unlockedTracks[unlockedTracks.Count - 1]);
            return;
        }

        // Find current track index
        int currentIndex = unlockedTracks.IndexOf(_currentTrack);

        if (currentIndex == -1)
        {
            // Current track not found in unlocked list, play last
            PlayTrack(unlockedTracks[unlockedTracks.Count - 1]);
            return;
        }

        // Play previous track (loop to end if at start)
        int previousIndex = currentIndex - 1;
        if (previousIndex < 0)
            previousIndex = unlockedTracks.Count - 1;

        PlayTrack(unlockedTracks[previousIndex]);
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Checks if the manager is fully initialized
    /// </summary>
    public bool IsInitialized()
    {
        return _isInitialized;
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// Saves the current playback state
    /// </summary>
    private void SaveCurrentState()
    {
        if (_saveProvider == null)
            return;

        BoomboxRuntimeData state = new BoomboxRuntimeData(
            _currentTrack?.purchasableId,
            _currentTrack != null
        );

        _saveProvider.SetBoomboxState(state);
    }

    /// <summary>
    /// Restores the saved playback state (called by RestoreMusicTask)
    /// </summary>
    public IEnumerator RestoreMusic()
    {
        if (!_isInitialized)
        {
            DebugManager.Warning("[Boombox] Cannot restore music - manager not initialized");
            yield break;
        }

        if (_saveProvider == null)
        {
            DebugManager.Warning("[Boombox] Cannot restore music - no save provider");
            yield break;
        }

        BoomboxRuntimeData savedState = _saveProvider.GetBoomboxState();

        // If there was no track playing, nothing to restore
        if (string.IsNullOrEmpty(savedState.lastPlayedTrackId) || !savedState.wasPlaying)
        {
            DebugManager.Log("[Boombox] <color=cyan>No music to restore</color>");
            yield break;
        }

        // Find the track by ID
        BoomboxTrackPurchasable trackToRestore = GetAllTracks()
            .FirstOrDefault(t => t.purchasableId == savedState.lastPlayedTrackId);

        if (trackToRestore == null)
        {
            DebugManager.Warning($"[Boombox] Could not find saved track: {savedState.lastPlayedTrackId}");
            yield break;
        }

        // Check if track is still unlocked
        if (!trackToRestore.IsUnlocked())
        {
            DebugManager.Warning($"[Boombox] Saved track is locked: {trackToRestore.displayName}");
            yield break;
        }

        // Restore playback
        PlayTrack(trackToRestore);
        DebugManager.Log($"[Boombox] <color=green>Restored music:</color> {trackToRestore.displayName}");

        yield return null;
    }

    #endregion

    #region BPM Tracking

    /// <summary>
    /// Starts the BPM tracker that publishes beat events
    /// </summary>
    private void StartBPMTracker(float bpm)
    {
        // Stop any existing tracker
        StopBPMTracker();

        if (bpm <= 0)
        {
            DebugManager.Warning("[Boombox] Invalid BPM value, BPM tracker not started");
            return;
        }

        // Start the tracker coroutine
        _bpmTrackerCoroutine = StartCoroutine(BPMTrackerCoroutine(bpm));
        DebugManager.Log($"[Boombox] <color=cyan>BPM tracker started:</color> {bpm} BPM ({60f / bpm:F3}s per beat)");
    }

    /// <summary>
    /// Stops the BPM tracker
    /// </summary>
    private void StopBPMTracker()
    {
        if (_bpmTrackerCoroutine != null)
        {
            StopCoroutine(_bpmTrackerCoroutine);
            _bpmTrackerCoroutine = null;
            DebugManager.Log("[Boombox] <color=cyan>BPM tracker stopped</color>");
        }
    }

    /// <summary>
    /// Coroutine that publishes beat events at regular intervals based on BPM
    /// Also publishes periodic resync events to prevent drift
    /// </summary>
    private IEnumerator BPMTrackerCoroutine(float bpm)
    {
        float beatInterval = 60f / bpm; // Seconds per beat
        float nextBeatTime = Time.time + beatInterval;
        float nextResyncTime = Time.time + RESYNC_INTERVAL;

        while (true)
        {
            // Check if it's time for a beat
            if (Time.time >= nextBeatTime)
            {
                // Publish beat event
                ServiceBus.Publish(BoomboxEventIds.ON_BEAT, this, bpm);

                // Calculate next beat time
                nextBeatTime += beatInterval;

                // Prevent drift accumulation - if we're way behind, reset
                if (nextBeatTime < Time.time - beatInterval)
                {
                    nextBeatTime = Time.time + beatInterval;
                }
            }

            // Check if it's time for a resync
            if (Time.time >= nextResyncTime)
            {
                // Publish resync event for effects to reset
                ServiceBus.Publish(BoomboxEventIds.ON_BEAT_RESYNC, this);
                nextResyncTime = Time.time + RESYNC_INTERVAL;
            }

            yield return null;
        }
    }

    #endregion
}
