using LargeNumbers;
using System.Collections.Generic;

public class Stat
{
    private AlphabeticNotation _baseValue;
    private List<StatModifier> _modifiers = new List<StatModifier>();

    public AlphabeticNotation FinalValue { get; private set; }

    public void SetBaseValue(AlphabeticNotation value)
    {
        _baseValue = value;
    }

    public void AddModifier(StatModifier modifier)
    {
        _modifiers.Add(modifier);
    }

    public void ClearModifiers()
    {
        _modifiers.Clear();
    }

    public void Calculate()
    {
        // Start with base value
        AlphabeticNotation additive = _baseValue;

        // Add all additive modifiers
        foreach (var mod in _modifiers)
        {
            if (mod.modifierType == ModifierType.Additive)
                additive += mod.additiveValue;
        }

        // Apply all multiplicative modifiers
        float multiplicative = 1f;
        foreach (var mod in _modifiers)
        {
            if (mod.modifierType == ModifierType.Multiplicative)
                multiplicative += mod.multiplicativeValue; // +50% = 0.5
        }

        // Multiply additive by multiplicative
        FinalValue = additive * multiplicative;
    }
}