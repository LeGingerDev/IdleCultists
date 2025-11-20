using UnityEngine;

/// <summary>
/// Shared GUI styles for the admin console
/// </summary>
public static class AdminGUIStyles
{
    private static bool _initialized = false;

    public static GUIStyle WindowStyle { get; private set; }
    public static GUIStyle TabButtonStyle { get; private set; }
    public static GUIStyle ActiveTabButtonStyle { get; private set; }
    public static GUIStyle HeaderStyle { get; private set; }
    public static GUIStyle SubHeaderStyle { get; private set; }
    public static GUIStyle ButtonStyle { get; private set; }
    public static GUIStyle SmallButtonStyle { get; private set; }
    public static GUIStyle LabelStyle { get; private set; }
    public static GUIStyle BoxStyle { get; private set; }

    public static void Initialize()
    {
        if (_initialized) return;

        WindowStyle = new GUIStyle(GUI.skin.window)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        TabButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        ActiveTabButtonStyle = new GUIStyle(TabButtonStyle)
        {
            normal = { textColor = Color.cyan }
        };

        HeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        SubHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        ButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12
        };

        SmallButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10
        };

        LabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };

        BoxStyle = new GUIStyle(GUI.skin.box);

        _initialized = true;
    }

    public static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }
}
