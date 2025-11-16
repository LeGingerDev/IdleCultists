#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using LargeNumbers;

public class AlphabeticNotationDrawer : OdinValueDrawer<AlphabeticNotation>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var rect = EditorGUILayout.GetControlRect();

        if (label != null)
        {
            rect = EditorGUI.PrefixLabel(rect, label);
        }

        var value = this.ValueEntry.SmartValue;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Show formatted preview at the top
        var previewText = FormatPreview(value);
        EditorGUILayout.LabelField("Preview", previewText, EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // Editable coefficient
        var newCoefficient = EditorGUILayout.DoubleField("Coefficient", value.coefficient);

        // Editable magnitude
        var newMagnitude = EditorGUILayout.IntField("Magnitude", value.magnitude);

        EditorGUILayout.EndVertical();

        // Update the value if it changed
        if (newCoefficient != value.coefficient || newMagnitude != value.magnitude)
        {
            this.ValueEntry.SmartValue = new AlphabeticNotation(newCoefficient, newMagnitude);
        }
    }

    private string FormatPreview(AlphabeticNotation value)
    {
        if (value.isZero) return "0";

        var coefficientStr = value.coefficient.ToString("F2");
        var magnitudeName = AlphabeticNotation.GetAlphabeticMagnitudeName(value.magnitude);

        return string.IsNullOrEmpty(magnitudeName)
            ? coefficientStr
            : $"{coefficientStr}{magnitudeName}";
    }
}
#endif