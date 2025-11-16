using LargeNumbers;
using System;

[Serializable]
public class StatModifier
{
    public StatType statType;
    public ModifierType modifierType;

    // Additive uses AlphabeticNotation (grows infinitely)
    public AlphabeticNotation additiveValue;

    // Multiplicative uses float (percentages stay small)
    public float multiplicativeValue;

    public string sourceId;

    // Constructor for Additive modifiers
    public StatModifier(StatType statType, AlphabeticNotation value, string sourceId = "")
    {
        this.statType = statType;
        this.modifierType = ModifierType.Additive;
        this.additiveValue = value;
        this.multiplicativeValue = 0f;
        this.sourceId = sourceId;
    }

    // Constructor for Multiplicative modifiers (0.5 = +50%)
    public StatModifier(StatType statType, float value, string sourceId = "")
    {
        this.statType = statType;
        this.modifierType = ModifierType.Multiplicative;
        this.additiveValue = AlphabeticNotation.zero;
        this.multiplicativeValue = value;
        this.sourceId = sourceId;
    }
}