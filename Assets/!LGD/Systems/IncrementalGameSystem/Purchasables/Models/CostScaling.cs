using LargeNumbers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class CostScaling
{
    [HorizontalGroup("Cost")]
    public Resource costType;

    // Keep as double for Inspector editing
    [HorizontalGroup("Cost")]
    public double baseCost;

    [HorizontalGroup("Cost")]
    public GrowthType growthType;

    [HorizontalGroup("Cost")]
    public double growthRate;

    [FoldoutGroup("Preview"), ShowInInspector, HideLabel, TextArea(10, 20), ReadOnly]
    private string _costPreview = "Click 'Preview Cost Scaling' to generate preview";

    // Returns raw double value for preview/debugging
    public double CalculateCostDouble(int tier)
    {
        return growthType switch
        {
            GrowthType.Linear => baseCost + (growthRate * (tier - 1)),
            GrowthType.Exponential => baseCost * Math.Pow(growthRate, tier - 1),
            _ => baseCost
        };
    }

    // Returns AlphabeticNotation for actual game use
    public AlphabeticNotation CalculateCost(int tier)
    {
        double cost = CalculateCostDouble(tier);
        return new AlphabeticNotation(cost);
    }

    public ResourceAmountPair CalculateCostWithResource(int tier)
    {
        AlphabeticNotation cost = CalculateCost(tier);
        return new ResourceAmountPair(costType, cost);
    }

#if UNITY_EDITOR
    [Button("Preview Cost Scaling"), FoldoutGroup("Preview")]
    private void PreviewCostScaling()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== COST SCALING PREVIEW ===\n");

        AlphabeticNotation totalCost = AlphabeticNotation.zero;

        for (int tier = 1; tier <= 20; tier++)
        {
            AlphabeticNotation cost = CalculateCost(tier);
            totalCost += cost;

            sb.AppendLine($"Tier {tier,2}: {cost.FormatWithDecimals(),15}  (Total: {totalCost.FormatWithDecimals()})");
        }

        _costPreview = sb.ToString();
        Debug.Log(_costPreview);
    }
#endif
}