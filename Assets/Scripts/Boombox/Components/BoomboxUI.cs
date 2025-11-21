using LGD.Core.Events;
using LGD.UIelements.Panels;
using LargeNumbers;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LGD.ResourceSystem.Managers;
using LGD.Extensions;
using LGD.ResourceSystem;
using LGD.ResourceSystem.Models;

/// <summary>
/// Main UI panel for the Boombox music player system
/// Displays available tracks, handles purchasing and playback
/// </summary>
public class BoomboxUI : SlidePanel
{
    [FoldoutGroup("Boombox References")]
    [SerializeField, Required] private BoomboxTrackDisplay _trackDisplayPrefab;

    [FoldoutGroup("Boombox References")]
    [SerializeField, Required] private Transform _trackDisplayContainer;

    [FoldoutGroup("Boombox References")]
    [SerializeField] private TextMeshProUGUI _achievementPointsText;

    [FoldoutGroup("Boombox References")]
    [SerializeField] private Resource _achievementPointsResource;

    [FoldoutGroup("Now Playing Section")]
    [SerializeField] private GameObject _nowPlayingSection;

    [FoldoutGroup("Now Playing Section")]
    [SerializeField] private TextMeshProUGUI _nowPlayingTrackText;

    [FoldoutGroup("Now Playing Section")]
    [SerializeField] private TextMeshProUGUI _nowPlayingArtistText;

    [FoldoutGroup("Now Playing Section")]
    [SerializeField] private Image _nowPlayingIcon;

    [FoldoutGroup("Now Playing Section")]
    [SerializeField] private Button _stopButton;

    [FoldoutGroup("Now Playing Section")]
    [SerializeField] private Button _previousButton;

    [FoldoutGroup("Now Playing Section")]
    [SerializeField] private Button _nextButton;

    private List<BoomboxTrackDisplay> _trackDisplays = new List<BoomboxTrackDisplay>();
    private bool _isInitialized = false;

    protected override void OnOpen()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        else
        {
            RefreshUI();
        }
    }

    protected override void OnClose()
    {
        // Optional: Any cleanup when panel closes
    }

    #region Initialization

    private void Initialize()
    {
        if (BoomboxManager.Instance == null || !BoomboxManager.Instance.IsInitialized())
        {
            DebugManager.Warning("[BoomboxUI] BoomboxManager not initialized yet");
            Invoke(nameof(Initialize), 0.1f);
            return;
        }

        // Get all tracks
        List<BoomboxTrackPurchasable> allTracks = BoomboxManager.Instance.GetAllTracks();

        // Create displays for each track
        foreach (var track in allTracks)
        {
            BoomboxTrackDisplay display = Instantiate(_trackDisplayPrefab, _trackDisplayContainer);
            display.Initialize(track);
            _trackDisplays.Add(display);
        }

        // Hook up now playing buttons
        HookUpButtons();

        // Initial refresh
        RefreshUI();

        _isInitialized = true;
        DebugManager.Log("[BoomboxUI] <color=cyan>Boombox UI initialized</color> with " + allTracks.Count + " tracks");
    }

    private void HookUpButtons()
    {
        if (_stopButton != null)
        {
            _stopButton.onClick.AddListener(OnStopClicked);
        }

        if (_previousButton != null)
        {
            _previousButton.onClick.AddListener(OnPreviousClicked);
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    #endregion

    #region UI Refresh

    private void RefreshUI()
    {
        RefreshAchievementPoints();
        RefreshNowPlaying();
    }

    private void RefreshAchievementPoints()
    {
        if (_achievementPointsText == null || _achievementPointsResource == null)
            return;

        if (ResourceManager.Instance == null)
            return;

        AlphabeticNotation points = ResourceManager.Instance.GetResourceAmount(_achievementPointsResource);
        _achievementPointsText.text = $"Achievement Points: {points.FormatWithDecimals()}";
    }

    private void RefreshNowPlaying()
    {
        if (_nowPlayingSection == null)
            return;

        if (BoomboxManager.Instance == null)
            return;

        BoomboxTrackPurchasable currentTrack = BoomboxManager.Instance.GetCurrentTrack();
        bool isPlaying = currentTrack != null;

        _nowPlayingSection.SetActive(isPlaying);

        if (isPlaying && currentTrack != null)
        {
            // Update now playing info
            if (_nowPlayingTrackText != null)
            {
                _nowPlayingTrackText.text = $"Currently Playing\n{currentTrack.displayName}";
            }

            if (_nowPlayingArtistText != null)
            {
                _nowPlayingArtistText.text = currentTrack.artist;
            }

            if (_nowPlayingIcon != null && currentTrack.icon != null)
            {
                _nowPlayingIcon.sprite = currentTrack.icon;
            }
        }
    }

    #endregion

    #region Button Handlers

    private void OnStopClicked()
    {
        if (BoomboxManager.Instance != null)
        {
            BoomboxManager.Instance.StopTrack();
        }
    }

    private void OnPreviousClicked()
    {
        if (BoomboxManager.Instance != null)
        {
            BoomboxManager.Instance.PreviousTrack();
        }
    }

    private void OnNextClicked()
    {
        if (BoomboxManager.Instance != null)
        {
            BoomboxManager.Instance.NextTrack();
        }
    }

    #endregion

    #region Event Listeners

    [Topic(BoomboxEventIds.ON_TRACK_STARTED)]
    public void OnTrackStarted(object sender, BoomboxTrackPurchasable track)
    {
        RefreshNowPlaying();
    }

    [Topic(BoomboxEventIds.ON_TRACK_STOPPED)]
    public void OnTrackStopped(object sender)
    {
        RefreshNowPlaying();
    }

    [Topic(BoomboxEventIds.ON_TRACK_UNLOCKED)]
    public void OnTrackUnlocked(object sender, BoomboxTrackPurchasable track)
    {
        // Track displays will handle their own refresh
        RefreshAchievementPoints();
    }

    [Topic(ResourceEventIds.ON_RESOURCES_UPDATED)]
    public void OnResourcesUpdated(object sender, System.Collections.Generic.Dictionary<Resource, AlphabeticNotation> resources)
    {
        RefreshAchievementPoints();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Manually refresh all track displays
    /// </summary>
    public void RefreshAllTracks()
    {
        foreach (var display in _trackDisplays)
        {
            display.Refresh();
        }
    }

    #endregion
}
