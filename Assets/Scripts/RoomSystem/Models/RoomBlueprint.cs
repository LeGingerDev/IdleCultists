using LargeNumbers;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomBlueprint_[NAME]", menuName = "LGD/Idle Cultist/Room/Create Blueprint")]
public class RoomBlueprint : ScriptableObject
{
    [FoldoutGroup("Identity")]
    public string roomId;

    [FoldoutGroup("Identity")]
    public string displayName;

    [FoldoutGroup("Identity"), TextArea(2, 4)]
    public string description;

    [FoldoutGroup("Identity")]
    public Sprite icon;

    [FoldoutGroup("Unlock Requirements")]
    public CostScaling unlockCost;

    [FoldoutGroup("Unlock Requirements")]
    [Tooltip("Other rooms that must be unlocked before this one")]
    public List<RoomBlueprint> prerequisiteRooms = new List<RoomBlueprint>();

    [FoldoutGroup("Unlock Requirements")]
    [Tooltip("Purchasables that must be bought before unlocking (prestige, skills, etc.)")]
    public List<BasePurchasable> purchaseRequirements = new List<BasePurchasable>();

    [FoldoutGroup("Unlock Requirements")]
    [Tooltip("Sequential unlock order (0 = starting room, 1 = first unlock, etc.)")]
    public int unlockOrder = 0;
    public AlphabeticNotation largeNumber;
    public ResourceAmountPair GetUnlockCost()
    {
        return unlockCost.CalculateCostWithResource(1);
    }

    public bool CheckPrerequisites()
    {
        // Check room prerequisites
        foreach (var prereqRoom in prerequisiteRooms)
        {
            if (!RoomManager.Instance.IsRoomUnlocked(prereqRoom.roomId))
            {
                return false;
            }
        }

        // Check purchase requirements
        foreach (var purchaseReq in purchaseRequirements)
        {
            if (!HasPurchasedRequirement(purchaseReq))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasPurchasedRequirement(BasePurchasable purchasable)
    {
        return purchasable != null && purchasable.GetPurchaseCount() > 0;
    }

    public bool CanAfford() =>
        ResourceManager.Instance.CanSpend(GetUnlockCost());

#if UNITY_EDITOR
    [Button("Rename Asset to Match Display Name"), FoldoutGroup("Identity")]
    private void RenameAsset()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("Display name is empty. Cannot rename asset.");
            return;
        }

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        string newName = $"RoomBlueprint_{displayName}";
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
#endif
}
