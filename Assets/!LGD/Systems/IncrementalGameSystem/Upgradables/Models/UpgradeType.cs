using UnityEngine;

public enum UpgradeType
{
    OneTime,    // Can only buy once
    Capped,     // Can buy up to maxTier
    Infinite    // Can buy forever
}
