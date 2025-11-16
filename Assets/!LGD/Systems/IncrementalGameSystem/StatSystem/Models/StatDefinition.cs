using LargeNumbers;
using Sirenix.OdinInspector;
using System;

[Serializable]
public class StatDefinition
{
    [HorizontalGroup("Stat")]
    public StatType statType;

    [HorizontalGroup("Stat")]
    public double baseValue; // Keep as double for Inspector ease

    public RuntimeStat ToRuntimeStat()
    {
        return new RuntimeStat
        {
            statType = this.statType,
            currentValue = new AlphabeticNotation(baseValue)
        };
    }
}