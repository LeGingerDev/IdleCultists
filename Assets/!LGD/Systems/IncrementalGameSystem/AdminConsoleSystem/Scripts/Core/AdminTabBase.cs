using UnityEngine;

/// <summary>
/// Base class for all admin console tabs
/// </summary>
public abstract class AdminTabBase
{
    protected Vector2 ScrollPosition;
    protected AdminConsoleManager Console;

    // Convenient style accessors
    protected GUIStyle HeaderStyle => AdminGUIStyles.HeaderStyle;
    protected GUIStyle SubHeaderStyle => AdminGUIStyles.SubHeaderStyle;
    protected GUIStyle ButtonStyle => AdminGUIStyles.ButtonStyle;
    protected GUIStyle SmallButtonStyle => AdminGUIStyles.SmallButtonStyle;
    protected GUIStyle LabelStyle => AdminGUIStyles.LabelStyle;
    protected GUIStyle BoxStyle => AdminGUIStyles.BoxStyle;

    public virtual void Initialize(AdminConsoleManager console)
    {
        Console = console;
    }

    /// <summary>
    /// Called when tab is opened
    /// </summary>
    public virtual void OnTabOpened()
    {
        RefreshData();
    }

    /// <summary>
    /// Called when tab is closed
    /// </summary>
    public virtual void OnTabClosed()
    {
    }

    /// <summary>
    /// Refresh tab data (override in derived classes)
    /// </summary>
    public virtual void RefreshData()
    {
    }

    /// <summary>
    /// Draw the tab content (must be implemented by derived classes)
    /// </summary>
    public abstract void DrawTab();

    /// <summary>
    /// Helper method to draw a section with a header
    /// </summary>
    protected void DrawSection(string title, System.Action drawContent)
    {
        GUILayout.BeginVertical(BoxStyle);
        GUILayout.Label(title, HeaderStyle);
        GUILayout.Space(5);
        drawContent?.Invoke();
        GUILayout.EndVertical();
        GUILayout.Space(10);
    }

    /// <summary>
    /// Helper method to draw a horizontal button row
    /// </summary>
    protected void DrawButtonRow(params System.Action[] buttons)
    {
        GUILayout.BeginHorizontal();
        foreach (var button in buttons)
        {
            button?.Invoke();
        }
        GUILayout.EndHorizontal();
    }
}
