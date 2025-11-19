using System;
using UnityEngine;

/// <summary>
/// Admin tab for viewing stat breakdowns
/// </summary>
public class AdminStatsTab : AdminTabBase
{
    public override void DrawTab()
    {
        GUILayout.Label("Stats Overview", HeaderStyle);
        GUILayout.Space(5);

        if (StatManager.Instance == null)
        {
            GUILayout.Label("StatManager not found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Recalculate Stats", ButtonStyle))
        {
            StatManager.Instance.RecalculateAllStats();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(450));

        // Display all stat types and their current values
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            GUILayout.BeginVertical(BoxStyle);

            GUILayout.Label($"{statType}", HeaderStyle);

            // Get breakdown
            StatBreakdown breakdown = StatManager.Instance.GetStatBreakdown(statType);

            GUILayout.Label($"Final Value: {breakdown.FinalValue}");
            GUILayout.Label($"Base: {breakdown.BaseValue}");
            GUILayout.Label($"Additive: +{breakdown.AdditiveBonus}");
            GUILayout.Label($"Multiplicative: x{(1f + breakdown.MultiplicativeBonus):F2} ({(breakdown.MultiplicativeBonus * 100f):F1}%)");

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }
}
