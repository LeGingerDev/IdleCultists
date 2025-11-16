using LargeNumbers;
using System;

[Serializable]
public class RuntimeStat
{
    public StatType statType;
    public AlphabeticNotation currentValue;

    public RuntimeStat()
    {
        currentValue = AlphabeticNotation.zero;
    }

    public RuntimeStat(StatType type, AlphabeticNotation value)
    {
        statType = type;
        currentValue = value;
    }
}