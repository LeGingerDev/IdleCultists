using System;
using UnityEngine;

/// <summary>
/// Automatically populates a dropdown with event IDs from all static classes ending with "EventIds"
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EventIdDropdownAttribute : PropertyAttribute
{
    // Empty - all logic is in the drawer
}