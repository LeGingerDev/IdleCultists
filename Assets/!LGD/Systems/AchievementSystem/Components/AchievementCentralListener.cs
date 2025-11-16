using LargeNumbers;
using LGD.Core;
using LGD.Core.Events;
using LGD.ResourceSystem;
using LGD.ResourceSystem.Models;
using System.Collections.Generic;

public class AchievementCentralListener : BaseBehaviour
{
    // Resources - Current/Max tracking
    [Topic(ResourceEventIds.ON_RESOURCES_UPDATED)]
    public void OnResourcesUpdated(object sender, Dictionary<Resource, AlphabeticNotation> resources)
    {
        foreach (var kvp in resources)
        {
            AchievementManager.Instance.UpdateResourceAchievements(kvp.Key, kvp.Value);
        }
    }

    [Topic(OrbOfDevotionEventIds.ON_ORB_CLICK)]
    public void OnOrbClicked(object sender)
    {
        AchievementManager.Instance.UpdateAchievementsByType(
            AchievementTrackingType.devotionClickCount,
            new AlphabeticNotation(1)
        );
    }

    // Resources - Cumulative tracking
    [Topic(ResourceEventIds.ON_RESOURCE_ADDED)]
    public void OnResourceAdded(object sender, Resource resource, AlphabeticNotation amountAdded)
    {
        AchievementManager.Instance.AddToResourceAchievements(resource, amountAdded);
    }

    // Devotee count
    [Topic(EntityEventIds.ON_ENTITY_SPAWNED)]
    public void OnDevoteeCountChanged(object sender, EntityRuntimeData runtimeData, bool fromLoading)
    {
        if(fromLoading == true)
            return;
        AchievementManager.Instance.UpdateAchievementsByType(
            AchievementTrackingType.devoteeCount,
            new AlphabeticNotation(1)
        );
    }

    // Devotion clicks
    [Topic("on-devotion-clicked")]
    public void OnDevotionClicked(object sender)
    {
        AchievementManager.Instance.UpdateAchievementsByType(
            AchievementTrackingType.devotionClickCount,
            new AlphabeticNotation(1)
        );
    }

    // Rooms unlocked
    [Topic(RoomEventIds.ON_ROOM_UNLOCKED)]
    public void OnRoomUnlocked(object sender)
    {
        AchievementManager.Instance.UpdateAchievementsByType(
            AchievementTrackingType.roomsUnlocked,
            new AlphabeticNotation(1)
        );
    }

}
