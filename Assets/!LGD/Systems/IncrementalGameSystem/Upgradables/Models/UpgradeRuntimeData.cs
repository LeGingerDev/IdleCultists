using System;
[Serializable]
public class UpgradeRuntimeData
{
    public string upgradeId;
    public int currentTier;
    public bool isActive;

    // Parameterless constructor for JSON deserialization
    public UpgradeRuntimeData() { }

    public UpgradeRuntimeData(string id)
    {
        upgradeId = id;
        currentTier = 0;
        isActive = false;
    }

    public void PurchaseTier()
    {
        currentTier++;
        isActive = true;
    }
}