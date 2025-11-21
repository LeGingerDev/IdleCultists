using System.Collections;
using UnityEngine;

/// <summary>
/// Save/Load provider for Boombox state
/// Stores the last played track and playback state
/// </summary>
public class BoomboxSaveProvider : SaveLoadProviderBase<BoomboxRuntimeData>
{
    [SerializeField]
    private BoomboxTrackPurchasable _defaultTrack;

    protected override string GetSaveFileName()
    {
        return "boombox.json";
    }

    protected override IEnumerator CreateDefault()
    {
        _data.Clear();

        BoomboxRuntimeData defaultData = new BoomboxRuntimeData();
        defaultData.lastPlayedTrackId = _defaultTrack.purchasableId;
        defaultData.wasPlaying = true;
        _data.Add(defaultData);

        DebugManager.Log("[Boombox] <color=green>Created default boombox save data</color>");

        yield return null;
    }

    /// <summary>
    /// Get the boombox state (there's only one item in the list)
    /// </summary>
    public BoomboxRuntimeData GetBoomboxState()
    {
        if (_data.Count > 0)
            return _data[0];

        // Return default if no data exists
        return new BoomboxRuntimeData();
    }

    /// <summary>
    /// Set the boombox state (updates the single item in the list)
    /// </summary>
    public void SetBoomboxState(BoomboxRuntimeData state)
    {
        if (_data.Count == 0)
        {
            _data.Add(state);
        }
        else
        {
            _data[0] = state;
        }

        MarkDirty();
    }
}
