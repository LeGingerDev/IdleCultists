#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LGD.Utilities.Attributes.Editor
{
    [CustomPropertyDrawer(typeof(EventIdDropdownAttribute))]
    public class EventIdDropdownDrawer : PropertyDrawer
    {
        private const float DROPDOWN_SPACING = 2f;
        private Dictionary<string, List<string>> _eventIdsByClass;
        private string[] _classNames;

        private string GetEditorPrefKey(SerializedProperty property)
        {
            return $"EventIdDropdown_{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";
        }

        private void InitializeEventIds()
        {
            if (_eventIdsByClass != null)
                return;

            _eventIdsByClass = new Dictionary<string, List<string>>();

            // Find all static classes ending with "EventIds"
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Name.EndsWith("EventIds"));

                    foreach (var type in types)
                    {
                        // Get all const string fields
                        var constFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                            .ToList();

                        if (constFields.Count > 0)
                        {
                            var eventIds = constFields
                                .Select(f => f.GetValue(null) as string)
                                .Where(v => !string.IsNullOrEmpty(v))
                                .OrderBy(v => v)
                                .ToList();

                            if (eventIds.Count > 0)
                            {
                                _eventIdsByClass[type.Name] = eventIds;
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }

            _classNames = _eventIdsByClass.Keys.OrderBy(k => k).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [EventIdDropdown] with string fields only.");
                return;
            }

            InitializeEventIds();

            if (_eventIdsByClass.Count == 0)
            {
                EditorGUI.LabelField(position, label.text, "No EventIds classes found");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // Calculate positions for two dropdowns
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            var totalDropdownWidth = position.width - EditorGUIUtility.labelWidth;
            var classDropdownRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                totalDropdownWidth * 0.4f - DROPDOWN_SPACING,
                EditorGUIUtility.singleLineHeight
            );
            var eventDropdownRect = new Rect(
                classDropdownRect.xMax + DROPDOWN_SPACING * 2,
                position.y,
                totalDropdownWidth * 0.6f - DROPDOWN_SPACING,
                EditorGUIUtility.singleLineHeight
            );

            EditorGUI.LabelField(labelRect, label);

            // Get or set the selected class from EditorPrefs
            string prefKey = GetEditorPrefKey(property);
            string selectedClass = EditorPrefs.GetString(prefKey, _classNames.Length > 0 ? _classNames[0] : "");

            // Try to determine class from current value if not set
            if (string.IsNullOrEmpty(selectedClass) && !string.IsNullOrEmpty(property.stringValue))
            {
                foreach (var kvp in _eventIdsByClass)
                {
                    if (kvp.Value.Contains(property.stringValue))
                    {
                        selectedClass = kvp.Key;
                        EditorPrefs.SetString(prefKey, selectedClass);
                        break;
                    }
                }
            }

            // Class dropdown
            int selectedClassIndex = Array.IndexOf(_classNames, selectedClass);
            if (selectedClassIndex < 0) selectedClassIndex = 0;

            int newClassIndex = EditorGUI.Popup(classDropdownRect, selectedClassIndex, _classNames);

            if (newClassIndex != selectedClassIndex)
            {
                selectedClass = _classNames[newClassIndex];
                EditorPrefs.SetString(prefKey, selectedClass);
                property.stringValue = ""; // Reset event selection when class changes
            }

            // Event ID dropdown
            if (!string.IsNullOrEmpty(selectedClass) && _eventIdsByClass.ContainsKey(selectedClass))
            {
                var eventIds = _eventIdsByClass[selectedClass];
                string[] eventIdArray = eventIds.ToArray();

                int selectedEventIndex = Array.IndexOf(eventIdArray, property.stringValue);

                // Show current value even if not found in list
                string displayValue = selectedEventIndex >= 0 ? eventIdArray[selectedEventIndex] : property.stringValue;

                int newEventIndex = EditorGUI.Popup(eventDropdownRect, selectedEventIndex, eventIdArray);

                if (newEventIndex >= 0 && newEventIndex < eventIdArray.Length)
                {
                    property.stringValue = eventIdArray[newEventIndex];
                }
            }
            else
            {
                EditorGUI.LabelField(eventDropdownRect, "No events available");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif