using LGD.Tasks;
using System.Collections;
using UnityEngine;

/// <summary>
/// Task for restoring the last played music track during game load
/// Waits for BoomboxManager to initialize, then restores saved playback state
/// Call this after other loading tasks (rooms, entities) are complete
/// </summary>
public class RestoreMusicTask : TaskBase
{
    public override IEnumerator ExecuteInternal()
    {
        Debug.Log("<color=yellow>[RestoreMusicTask] Starting music restoration...</color>");

        // Wait for BoomboxManager to initialize
        yield return new WaitUntil(() => BoomboxManager.Instance != null && BoomboxManager.Instance.IsInitialized());

        // Restore the saved music state
        yield return BoomboxManager.Instance.RestoreMusic();

        Debug.Log("<color=green>[RestoreMusicTask] Music restoration complete!</color>");
    }
}
