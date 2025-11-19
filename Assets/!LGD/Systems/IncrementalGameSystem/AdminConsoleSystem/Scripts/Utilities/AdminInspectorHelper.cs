using LargeNumbers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Helper class for inspecting and drawing object fields/properties in the admin console
/// </summary>
public static class AdminInspectorHelper
{
    private const BindingFlags INSPECTOR_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    #region Type Checking

    public static bool CanInspect(Type type)
    {
        if (type == null) return false;

        // Primitives and common types
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return true;

        // Unity types
        if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color))
            return true;

        // Custom types
        if (type == typeof(AlphabeticNotation) || type == typeof(SerializableVector3))
            return true;

        // Enums
        if (type.IsEnum)
            return true;

        // Class/Struct types (inspectable but read-only by default)
        if (type.IsClass || type.IsValueType)
            return true;

        return false;
    }

    public static bool IsEditableType(Type type)
    {
        if (type == null) return false;

        // Primitives
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return true;

        // Enums
        if (type.IsEnum)
            return true;

        // Custom editable types
        if (type == typeof(AlphabeticNotation))
            return true;

        return false;
    }

    #endregion

    #region Member Discovery

    public static List<FieldInfo> GetInspectableFields(Type type)
    {
        if (type == null) return new List<FieldInfo>();

        return type.GetFields(INSPECTOR_FLAGS)
            .Where(f => !f.IsLiteral && !f.IsInitOnly && CanInspect(f.FieldType))
            .OrderBy(f => f.Name)
            .ToList();
    }

    public static List<PropertyInfo> GetInspectableProperties(Type type)
    {
        if (type == null) return new List<PropertyInfo>();

        return type.GetProperties(INSPECTOR_FLAGS)
            .Where(p => p.CanRead && CanInspect(p.PropertyType))
            .OrderBy(p => p.Name)
            .ToList();
    }

    #endregion

    #region Drawing

    /// <summary>
    /// Draw a field with automatic type detection
    /// </summary>
    public static void DrawField(object obj, FieldInfo field, bool editable = false)
    {
        if (obj == null || field == null) return;

        GUILayout.BeginHorizontal();

        GUILayout.Label($"{field.Name}:", GUILayout.Width(150));

        object value = field.GetValue(obj);
        Type fieldType = field.FieldType;

        if (editable && IsEditableType(fieldType))
        {
            object newValue = DrawEditableValue(value, fieldType);
            if (newValue != null && !newValue.Equals(value))
            {
                field.SetValue(obj, newValue);
            }
        }
        else
        {
            DrawReadOnlyValue(value, fieldType);
        }

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draw a property with automatic type detection
    /// </summary>
    public static void DrawProperty(object obj, PropertyInfo prop)
    {
        if (obj == null || prop == null) return;

        GUILayout.BeginHorizontal();

        GUILayout.Label($"{prop.Name}:", GUILayout.Width(150));

        try
        {
            object value = prop.GetValue(obj);
            DrawReadOnlyValue(value, prop.PropertyType);
        }
        catch (Exception e)
        {
            GUILayout.Label($"<Error: {e.Message}>", GUILayout.Width(200));
        }

        GUILayout.EndHorizontal();
    }

    #endregion

    #region Value Drawing

    private static void DrawReadOnlyValue(object value, Type type)
    {
        if (value == null)
        {
            GUILayout.Label("null", GUILayout.Width(200));
            return;
        }

        // AlphabeticNotation
        if (type == typeof(AlphabeticNotation))
        {
            GUILayout.Label(value.ToString(), GUILayout.Width(200));
        }
        // Vector3
        else if (type == typeof(Vector3))
        {
            Vector3 v = (Vector3)value;
            GUILayout.Label($"({v.x:F2}, {v.y:F2}, {v.z:F2})", GUILayout.Width(200));
        }
        // SerializableVector3
        else if (type == typeof(SerializableVector3))
        {
            SerializableVector3 v = (SerializableVector3)value;
            GUILayout.Label($"({v.x:F2}, {v.y:F2}, {v.z:F2})", GUILayout.Width(200));
        }
        // Color
        else if (type == typeof(Color))
        {
            Color c = (Color)value;
            GUILayout.Label($"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})", GUILayout.Width(200));
        }
        // Bool
        else if (type == typeof(bool))
        {
            GUILayout.Label(value.ToString(), GUILayout.Width(200));
        }
        // Enum
        else if (type.IsEnum)
        {
            GUILayout.Label(value.ToString(), GUILayout.Width(200));
        }
        // Collections
        else if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            IEnumerable enumerable = (IEnumerable)value;
            int count = 0;
            foreach (var _ in enumerable) count++;
            GUILayout.Label($"Collection [{count} items]", GUILayout.Width(200));
        }
        // Default (ToString)
        else
        {
            string displayValue = value.ToString();
            if (displayValue.Length > 50)
                displayValue = displayValue.Substring(0, 47) + "...";
            GUILayout.Label(displayValue, GUILayout.Width(200));
        }
    }

    private static object DrawEditableValue(object value, Type type)
    {
        // String
        if (type == typeof(string))
        {
            return GUILayout.TextField(value?.ToString() ?? "", GUILayout.Width(200));
        }
        // Int
        else if (type == typeof(int))
        {
            string input = GUILayout.TextField(value?.ToString() ?? "0", GUILayout.Width(200));
            return int.TryParse(input, out int result) ? result : value;
        }
        // Float
        else if (type == typeof(float))
        {
            string input = GUILayout.TextField(value?.ToString() ?? "0", GUILayout.Width(200));
            return float.TryParse(input, out float result) ? result : value;
        }
        // Double
        else if (type == typeof(double))
        {
            string input = GUILayout.TextField(value?.ToString() ?? "0", GUILayout.Width(200));
            return double.TryParse(input, out double result) ? result : value;
        }
        // Bool
        else if (type == typeof(bool))
        {
            return GUILayout.Toggle((bool)value, "", GUILayout.Width(200));
        }
        // Enum
        else if (type.IsEnum)
        {
            // Simple enum selector (could be enhanced with dropdown)
            GUILayout.Label(value.ToString(), GUILayout.Width(200));
            return value; // Read-only for now
        }
        // AlphabeticNotation
        else if (type == typeof(AlphabeticNotation))
        {
            string input = GUILayout.TextField(value?.ToString() ?? "0", GUILayout.Width(200));
            if (double.TryParse(input, out double result))
            {
                return new AlphabeticNotation(result);
            }
            return value;
        }

        // Fallback: read-only
        DrawReadOnlyValue(value, type);
        return value;
    }

    #endregion

    #region Object Info

    public static string GetObjectSummary(object obj)
    {
        if (obj == null) return "null";

        Type type = obj.GetType();

        // Try to find identifying properties
        var idProp = type.GetProperty("id") ?? type.GetProperty("Id") ?? type.GetProperty("ID");
        var nameProp = type.GetProperty("name") ?? type.GetProperty("Name") ?? type.GetProperty("displayName");

        string summary = type.Name;

        if (idProp != null)
        {
            try { summary += $" (ID: {idProp.GetValue(obj)})"; } catch { }
        }

        if (nameProp != null)
        {
            try { summary += $" [{nameProp.GetValue(obj)}]"; } catch { }
        }

        return summary;
    }

    #endregion
}
