using Audio.Models;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Purchasable that represents a music track for the Boombox system
/// When purchased, unlocks the track for playback
/// </summary>
[CreateAssetMenu(fileName = "BoomboxTrack_[NAME]", menuName = "LGD/Idle Cultist/Purchasable/Boombox Track")]
public class BoomboxTrackPurchasable : EventPurchasable
{
    [FoldoutGroup("Track Data"), Required]
    [Tooltip("The audio clip to play when this track is selected")]
    public AudioClipSO audioClipSO;

    [FoldoutGroup("Track Data")]
    [Tooltip("Artist or composer name")]
    public string artist = "Unknown Artist";

    [FoldoutGroup("Track Data"), TextArea(2, 5)]
    [Tooltip("Additional flavor text or description")]
    public string trackDescription;

    [FoldoutGroup("Track Data")]
    [Tooltip("If true, this track is unlocked by default (e.g., original game music)")]
    public bool isDefaultUnlocked = false;

    public override void HandlePurchase(BasePurchasableRuntimeData runtimeData)
    {
        // Publish event to notify Boombox system that this track was unlocked
        ServiceBus.Publish(BoomboxEventIds.ON_TRACK_UNLOCKED, this, this);
    }

    public override string GetContextId()
    {
        return purchasableId;
    }

    /// <summary>
    /// Checks if this track is unlocked (either purchased or default unlocked)
    /// </summary>
    public bool IsUnlocked()
    {
        if (isDefaultUnlocked)
            return true;

        return this.GetPurchaseCount() > 0;
    }

#if UNITY_EDITOR
    [Button("Rename Asset to Match Track Name"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("Display name is empty. Cannot rename asset.");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string newName = $"BoomboxTrack_{displayName.Replace(" ", "")}";
        string result = UnityEditor.AssetDatabase.RenameAsset(assetPath, newName);

        if (string.IsNullOrEmpty(result))
        {
            Debug.Log($"Successfully renamed asset to: {newName}");
        }
        else
        {
            Debug.LogError($"Failed to rename asset: {result}");
        }
    }

    [Button("Validate Track Configuration"), FoldoutGroup("Track Data")]
    private void ValidateConfiguration()
    {
        if (audioClipSO == null)
        {
            Debug.LogError($"[{displayName}] AudioClipSO is not assigned!");
            return;
        }

        if (audioClipSO.audioClips == null || audioClipSO.audioClips.Count == 0)
        {
            Debug.LogError($"[{displayName}] AudioClipSO has no audio clips assigned!");
            return;
        }

        if (string.IsNullOrEmpty(artist))
        {
            Debug.LogWarning($"[{displayName}] Artist field is empty.");
        }

        Debug.Log($"[{displayName}] Track configuration is valid! Audio: {audioClipSO.id}, Artist: {artist}");
    }
#endif
}
