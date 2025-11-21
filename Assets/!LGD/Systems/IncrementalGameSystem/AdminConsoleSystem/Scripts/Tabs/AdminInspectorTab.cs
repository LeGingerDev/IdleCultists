using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Admin tab for inspecting object properties and fields at runtime
/// </summary>
public class AdminInspectorTab : AdminTabBase
{
    private object _inspectedObject;
    private string _objectTypeName = "None";
    private List<FieldInfo> _fields = new List<FieldInfo>();
    private List<PropertyInfo> _properties = new List<PropertyInfo>();

    private Vector2 _fieldsScroll;
    private Vector2 _propertiesScroll;

    // Quick selection options
    private string[] _quickSelectOptions = new string[]
    {
        "ResourceManager",
        "PurchasableManager",
        "EntityManager",
        "RoomManager",
        "StatManager",
        "AchievementManager",
        "TimerManager"
    };

    private int _selectedQuickOption = 0;

    public override void DrawTab()
    {
        GUILayout.Label("Object Inspector", HeaderStyle);
        GUILayout.Space(5);

        // Quick selection
        DrawSection("Quick Select Manager", () =>
        {
            GUILayout.BeginHorizontal();
            _selectedQuickOption = GUILayout.SelectionGrid(_selectedQuickOption, _quickSelectOptions, 4);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button("Inspect Selected Manager", ButtonStyle))
            {
                InspectManagerByName(_quickSelectOptions[_selectedQuickOption]);
            }
        });

        // Currently inspected object
        DrawSection($"Inspecting: {_objectTypeName}", () =>
        {
            if (_inspectedObject == null)
            {
                GUILayout.Label("No object selected. Use Quick Select above or click objects in-game (future feature).");
                return;
            }

            GUILayout.Label($"Type: {_inspectedObject.GetType().FullName}", SubHeaderStyle);
            GUILayout.Label($"Summary: {AdminInspectorHelper.GetObjectSummary(_inspectedObject)}");

            GUILayout.Space(10);

            // Tabs for Fields and Properties
            GUILayout.BeginHorizontal();

            if (GUILayout.Button($"Fields ({_fields.Count})", ButtonStyle))
            {
                // Already showing fields by default
            }

            if (GUILayout.Button($"Properties ({_properties.Count})", ButtonStyle))
            {
                // Future: toggle to properties view
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Draw fields
            GUILayout.Label("Fields:", SubHeaderStyle);
            _fieldsScroll = GUILayout.BeginScrollView(_fieldsScroll, GUILayout.Height(200));

            if (_fields.Count == 0)
            {
                GUILayout.Label("No inspectable fields found.");
            }
            else
            {
                foreach (var field in _fields)
                {
                    AdminInspectorHelper.DrawField(_inspectedObject, field, editable: false);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Draw properties
            GUILayout.Label("Properties:", SubHeaderStyle);
            _propertiesScroll = GUILayout.BeginScrollView(_propertiesScroll, GUILayout.Height(200));

            if (_properties.Count == 0)
            {
                GUILayout.Label("No inspectable properties found.");
            }
            else
            {
                foreach (var prop in _properties)
                {
                    AdminInspectorHelper.DrawProperty(_inspectedObject, prop);
                }
            }

            GUILayout.EndScrollView();
        });
    }

    private void InspectManagerByName(string managerName)
    {
        object manager = null;

        // Use reflection to get the Instance property of the manager
        Type managerType = Type.GetType(managerName);

        if (managerType == null)
        {
            // Try common namespaces
            managerType = Type.GetType($"LGD.ResourceSystem.Managers.{managerName}");
        }

        if (managerType != null)
        {
            var instanceProp = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp != null)
            {
                manager = instanceProp.GetValue(null);
            }
        }

        if (manager != null)
        {
            InspectObject(manager);
            DebugManager.Log($"[Admin] Inspecting {managerName}");
        }
        else
        {
            DebugManager.Warning($"[Admin] Could not find manager: {managerName}");
        }
    }

    public void InspectObject(object obj)
    {
        _inspectedObject = obj;

        if (obj == null)
        {
            _objectTypeName = "None";
            _fields.Clear();
            _properties.Clear();
            return;
        }

        Type type = obj.GetType();
        _objectTypeName = type.Name;

        // Get inspectable members
        _fields = AdminInspectorHelper.GetInspectableFields(type);
        _properties = AdminInspectorHelper.GetInspectableProperties(type);
    }
}
