using System.Collections.Generic;
using UnityEngine;

public interface IDropZone
{
    string GetZoneId();
    DropZoneType GetZoneType();
    bool CanAcceptEntity(EntityPickup entity);
    Vector3 GetDropPosition(Vector3 worldPos);
    void OnEntityAssigned(EntityPickup entity);
    void OnEntityRemoved(EntityPickup entity);
    void OnEntityReconnected(EntityPickup entity); // For save/load restoration
    void OnEntityReturned(EntityPickup entity); // For invalid drop return
    void ShowVisualFeedback();
    void HideVisualFeedback();
    int GetCurrentCapacity();
    int GetMaxCapacity();
    int GetAvailableCapacity();
    List<string> GetAssignedEntityIds();
}