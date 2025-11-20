/// <summary>
/// Event IDs for the Boombox system
/// Used with ServiceBus for event publishing/subscribing
/// </summary>
public static class BoomboxEventIds
{
    /// <summary>
    /// Published when a track is unlocked (purchased or otherwise made available)
    /// Parameters: (object sender, BoomboxTrackPurchasable track)
    /// </summary>
    public const string ON_TRACK_UNLOCKED = "on-track-unlocked";

    /// <summary>
    /// Published when a track starts playing
    /// Parameters: (object sender, BoomboxTrackPurchasable track)
    /// </summary>
    public const string ON_TRACK_STARTED = "on-track-started";

    /// <summary>
    /// Published when the current track is stopped
    /// Parameters: (object sender)
    /// </summary>
    public const string ON_TRACK_STOPPED = "on-track-stopped";

    /// <summary>
    /// Published when playback changes to a different track
    /// Parameters: (object sender, BoomboxTrackPurchasable previousTrack, BoomboxTrackPurchasable newTrack)
    /// </summary>
    public const string ON_TRACK_CHANGED = "on-track-changed";

    /// <summary>
    /// Published when the Boombox manager initializes
    /// Parameters: (object sender)
    /// </summary>
    public const string ON_BOOMBOX_INITIALIZED = "on-boombox-initialized";

    /// <summary>
    /// Published on every beat of the currently playing track
    /// Parameters: (object sender, float bpm)
    /// </summary>
    public const string ON_BEAT = "on-beat";

    /// <summary>
    /// Published periodically to allow beat effects to resync with audio
    /// Helps prevent drift over long playback sessions
    /// Parameters: (object sender)
    /// </summary>
    public const string ON_BEAT_RESYNC = "on-beat-resync";
}
