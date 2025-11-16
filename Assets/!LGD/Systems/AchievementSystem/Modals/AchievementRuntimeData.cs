using LargeNumbers;
using System;

[Serializable]
public class AchievementRuntimeData
{
    public string id;
    public bool isUnlocked;
    public AchievementTrackingType trackingType;
    public AlphabeticNotation progress;
    public AlphabeticNotation goal;
}
