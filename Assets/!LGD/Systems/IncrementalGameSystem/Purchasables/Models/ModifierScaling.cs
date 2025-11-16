using LargeNumbers;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class ModifierScaling
{
    [HorizontalGroup("Modifier")]
    public StatType statType;

    [HorizontalGroup("Modifier")]
    public ModifierType modifierType;

    [HorizontalGroup("Values")]
    public double baseValue; // Keep as double for Inspector

    [HorizontalGroup("Values")]
    public double growthPerLevel;

    [FoldoutGroup("Preview"), ShowInInspector, HideLabel, TextArea(10, 20), ReadOnly]
    private string _modifierPreview = "Click 'Preview Modifier Scaling' to generate preview";

    // Raw calculation
    public double CalculateValue(int tier)
    {
        return baseValue + (growthPerLevel * (tier - 1));
    }

    // Returns AlphabeticNotation for additive modifiers
    public AlphabeticNotation CalculateAdditiveValue(int tier)
    {
        double value = CalculateValue(tier);
        return new AlphabeticNotation(value);
    }

    // Returns float for multiplicative modifiers (percentages)
    public float CalculateMultiplicativeValue(int tier)
    {
        return (float)CalculateValue(tier);
    }

#if UNITY_EDITOR
    [Button("Preview Modifier Scaling"), FoldoutGroup("Preview")]
    private void PreviewModifierScaling()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== MODIFIER SCALING PREVIEW ({statType}) ===\n");

        for (int tier = 1; tier <= 20; tier++)
        {
            double value = CalculateValue(tier);

            string displayValue;
            if (modifierType == ModifierType.Multiplicative)
            {
                displayValue = $"+{value * 100:F1}%";
            }
            else
            {
                AlphabeticNotation alphaValue = new AlphabeticNotation(value);
                displayValue = alphaValue.ToString();
            }

            sb.AppendLine($"Tier {tier,2}: {displayValue,15} {modifierType}");
        }

        _modifierPreview = sb.ToString();
        Debug.Log(_modifierPreview);
    }
#endif
}