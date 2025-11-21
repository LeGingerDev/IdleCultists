using System;

/// <summary>
/// Runtime data for the Boombox system
/// Stores the last played track and playback state for save/load
/// </summary>
[Serializable]
public class BoomboxRuntimeData
{
    /// <summary>
    /// The ID of the last track that was playing (null if nothing was playing)
    /// </summary>
    public string lastPlayedTrackId;

    /// <summary>
    /// Whether the track was playing when the game was saved
    /// </summary>
    public bool wasPlaying;

    /// <summary>
    /// Default constructor
    /// </summary>
    public BoomboxRuntimeData()
    {
        lastPlayedTrackId = null;
        wasPlaying = false;
    }

    /// <summary>
    /// Constructor with values
    /// </summary>
    public BoomboxRuntimeData(string trackId, bool isPlaying)
    {
        lastPlayedTrackId = trackId;
        wasPlaying = isPlaying;
    }
}
