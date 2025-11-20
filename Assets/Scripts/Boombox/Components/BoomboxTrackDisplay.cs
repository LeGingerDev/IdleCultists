using LGD.Core;
using LGD.Core.Events;
using LGD.Extensions;
using LGD.ResourceSystem;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Isolated component for displaying a single boombox track
/// Handles both locked (purchasable) and unlocked (playable) states
/// Similar to purchasable displays but specific to music tracks
/// </summary>
public class BoomboxTrackDisplay : BaseBehaviour
{
    [FoldoutGroup("Track Reference"), Required]
    [SerializeField] private BoomboxTrackPurchasable _track;

    [FoldoutGroup("UI References - Basic"), Required]
    [SerializeField] private Image _iconImage;

    [FoldoutGroup("UI References - Basic")]
    [SerializeField] private TextMeshProUGUI _trackNameText;

    [FoldoutGroup("UI References - Basic")]
    [SerializeField] private TextMeshProUGUI _artistText;

    [FoldoutGroup("UI References - Basic")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [FoldoutGroup("UI References - Purchase")]
    [SerializeField] private GameObject _purchaseSection;

    [FoldoutGroup("UI References - Purchase")]
    [SerializeField] private Button _purchaseButton;

    [FoldoutGroup("UI References - Purchase")]
    [SerializeField] private TextMeshProUGUI _costText;

    [FoldoutGroup("UI References - Purchase")]
    [SerializeField] private TextMeshProUGUI _purchaseButtonText;

    [FoldoutGroup("UI References - Playback")]
    [SerializeField] private GameObject _playbackSection;

    [FoldoutGroup("UI References - Playback")]
    [SerializeField] private Button _playButton;

    [FoldoutGroup("UI References - Playback")]
    [SerializeField] private TextMeshProUGUI _playButtonText;

    [FoldoutGroup("UI References - Playback")]
    [SerializeField] private GameObject _nowPlayingIndicator;

    [FoldoutGroup("Settings")]
    [SerializeField] private Color _lockedColor = Color.gray;

    [FoldoutGroup("Settings")]
    [SerializeField] private Color _unlockedColor = Color.white;

    private bool _isInitialized = false;

    #region Initialization

    /// <summary>
    /// Initialize the display with a specific track
    /// </summary>
    public void Initialize(BoomboxTrackPurchasable track)
    {
        _track = track;
        Initialize();
    }

    /// <summary>
    /// Initialize the display (uses assigned track)
    /// </summary>
    public void Initialize()
    {
        if (_track == null)
        {
            DebugManager.Error("[BoomboxTrackDisplay] Cannot initialize - no track assigned!");
            return;
        }

        SetupStaticUI();
        RefreshDynamicUI();
        HookUpButtons();

        _isInitialized = true;
    }

    private void Start()
    {
        if (!_isInitialized && _track != null)
        {
            Initialize();
        }
    }

    #endregion

    #region UI Setup

    /// <summary>
    /// Setup UI elements that don't change (name, icon, artist, etc.)
    /// </summary>
    private void SetupStaticUI()
    {
        // Set icon
        if (_iconImage != null && _track.icon != null)
        {
            _iconImage.sprite = _track.icon;
        }

        // Set track name
        if (_trackNameText != null)
        {
            _trackNameText.text = _track.displayName;
        }

        // Set artist
        if (_artistText != null)
        {
            _artistText.text = _track.artist;
        }

        // Set description (use trackDescription if available, otherwise use description)
        if (_descriptionText != null)
        {
            string description = !string.IsNullOrEmpty(_track.trackDescription)
                ? _track.trackDescription
                : _track.description;
            _descriptionText.text = description;
        }
    }

    /// <summary>
    /// Refresh dynamic UI elements (cost, unlock status, now playing, etc.)
    /// </summary>
    private void RefreshDynamicUI()
    {
        bool isUnlocked = _track.IsUnlocked();

        // Toggle sections based on unlock status
        if (_purchaseSection != null)
        {
            _purchaseSection.SetActive(!isUnlocked);
        }

        if (_playbackSection != null)
        {
            _playbackSection.SetActive(isUnlocked);
        }

        // Update purchase UI if locked
        if (!isUnlocked)
        {
            UpdatePurchaseUI();
        }

        // Update playback UI if unlocked
        if (isUnlocked)
        {
            UpdatePlaybackUI();
        }

        // Update visual tint
        UpdateVisualTint(isUnlocked);
    }

    private void UpdatePurchaseUI()
    {
        // Update cost text
        if (_costText != null)
        {
            var cost = _track.GetCurrentCostSafe();
            if (cost.resource != null)
            {
                _costText.text = $"{cost.amount.FormatWithDecimals()} {cost.resource.displayName}";
            }
        }

        // Update purchase button state
        if (_purchaseButton != null)
        {
            bool canAfford = _track.CanAfford();
            _purchaseButton.interactable = canAfford;

            if (_purchaseButtonText != null)
            {
                _purchaseButtonText.text = canAfford ? "Purchase" : "Can't Afford";
            }
        }
    }

    private void UpdatePlaybackUI()
    {
        // Update now playing indicator
        if (_nowPlayingIndicator != null)
        {
            bool isCurrentTrack = BoomboxManager.Instance != null &&
                                  BoomboxManager.Instance.GetCurrentTrack() == _track;
            _nowPlayingIndicator.SetActive(isCurrentTrack);
        }

        // Update play button text
        if (_playButtonText != null)
        {
            bool isCurrentTrack = BoomboxManager.Instance != null &&
                                  BoomboxManager.Instance.GetCurrentTrack() == _track;
            _playButtonText.text = isCurrentTrack ? "Now Playing" : "Play";
        }
    }

    private void UpdateVisualTint(bool isUnlocked)
    {
        Color targetColor = isUnlocked ? _unlockedColor : _lockedColor;

        if (_iconImage != null)
        {
            _iconImage.color = targetColor;
        }
    }

    #endregion

    #region Button Handlers

    private void HookUpButtons()
    {
        if (_purchaseButton != null)
        {
            _purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        if (_playButton != null)
        {
            _playButton.onClick.AddListener(OnPlayClicked);
        }
    }

    private void OnPurchaseClicked()
    {
        if (_track == null)
            return;

        if (!_track.CanAfford())
        {
            DebugManager.Warning($"[BoomboxTrackDisplay] Cannot afford track: {_track.displayName}");
            return;
        }

        // Execute purchase through the purchasable system
        bool success = _track.ExecutePurchase();

        if (success)
        {
            DebugManager.Log($"[BoomboxTrackDisplay] <color=green>Purchased track:</color> {_track.displayName}");
            RefreshDynamicUI();
        }
    }

    private void OnPlayClicked()
    {
        if (_track == null)
            return;

        if (!_track.IsUnlocked())
        {
            DebugManager.Warning($"[BoomboxTrackDisplay] Attempted to play locked track: {_track.displayName}");
            return;
        }

        if (BoomboxManager.Instance != null)
        {
            BoomboxManager.Instance.PlayTrack(_track);
        }
    }

    #endregion

    #region Event Listeners

    [Topic(PurchasableEventIds.ON_PURCHASABLE_PURCHASED)]
    public void OnPurchasablePurchased(object sender, BasePurchasable blueprint, BasePurchasableRuntimeData runtimeData)
    {
        // Check if this is our track
        if (blueprint == _track)
        {
            RefreshDynamicUI();
        }
    }

    [Topic(BoomboxEventIds.ON_TRACK_STARTED)]
    public void OnTrackStarted(object sender, BoomboxTrackPurchasable track)
    {
        // Update now playing indicator
        RefreshDynamicUI();
    }

    [Topic(BoomboxEventIds.ON_TRACK_STOPPED)]
    public void OnTrackStopped(object sender)
    {
        // Update now playing indicator
        RefreshDynamicUI();
    }

    [Topic(ResourceEventIds.ON_RESOURCES_UPDATED)]
    public void OnResourcesUpdated(object sender, System.Collections.Generic.Dictionary<Resource, LargeNumbers.AlphabeticNotation> resources)
    {
        // Refresh purchase button state when resources change
        if (!_track.IsUnlocked())
        {
            UpdatePurchaseUI();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get the track this display is showing
    /// </summary>
    public BoomboxTrackPurchasable GetTrack()
    {
        return _track;
    }

    /// <summary>
    /// Manually refresh the display
    /// </summary>
    public void Refresh()
    {
        RefreshDynamicUI();
    }

    #endregion
}
