using LGD.Core.Events;
using System.Linq;

public static class InputExtensions
{
    /// <summary>
    /// Checks if world clicks can be processed by querying all systems via RequestBus.
    /// Returns true only if ALL providers approve.
    /// </summary>
    public static bool CanProcessWorldClick(this object requester)
    {
        var responses = RequestBus.RequestAll<bool>(
            InputRequestIds.CAN_PROCESS_WORLD_CLICK,
            requester
        );

        // No providers = allow by default
        if (responses.Count == 0)
            return true;

        // All must approve
        return responses.All(r => r);
    }

    /// <summary>
    /// Checks if pickup actions can be processed by querying all systems via RequestBus.
    /// Returns true only if ALL providers approve.
    /// </summary>
    public static bool CanProcessPickup(this object requester)
    {
        var responses = RequestBus.RequestAll<bool>(
            InputRequestIds.CAN_PROCESS_PICKUP,
            requester
        );

        // No providers = allow by default
        if (responses.Count == 0)
            return true;

        // All must approve
        return responses.All(r => r);
    }
}
