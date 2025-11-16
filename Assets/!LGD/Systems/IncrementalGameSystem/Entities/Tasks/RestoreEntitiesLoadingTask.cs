using LGD.Tasks;
using System.Collections;
using UnityEngine;

public class RestoreEntitiesLoadingTask : TaskBase
{
    public override IEnumerator ExecuteInternal()
    {
        // 1. Wait for EntityManager to finish silent load (automatic)
        yield return new WaitUntil(() => EntityManager.Instance.IsInitialized());

        // 2. Restore entities - spawns GameObjects at saved positions
        yield return EntityManager.Instance.RestoreEntities();

        // 3. Reconnect entities to zones - rebuilds zone tracking
        yield return EntityZoneReconnectionHelper.ReconnectEntitiesToZones();

        // 4. Continue with rest of initialization...
        Debug.Log("Game fully restored!");
    }
}
