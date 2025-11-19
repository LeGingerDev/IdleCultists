using LGD.Core.Singleton;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main coordinator for the admin console system
/// Manages window, tabs, and input handling
/// Toggle with Shift+C
/// </summary>
public class AdminConsoleManager : MonoSingleton<AdminConsoleManager>
{
    [SerializeField, FoldoutGroup("Settings")]
    private KeyCode _modifierKey = KeyCode.LeftShift;

    [SerializeField, FoldoutGroup("Settings")]
    private KeyCode _toggleKey = KeyCode.C;

    [SerializeField, FoldoutGroup("Settings")]
    private Vector2 _defaultWindowSize = new Vector2(900, 650);

    [SerializeField, FoldoutGroup("Settings")]
    private Vector2 _defaultWindowPosition = new Vector2(100, 100);

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private bool _isVisible = false;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private int _currentTab = 0;

    private Rect _windowRect;
    private List<AdminTabBase> _tabs = new List<AdminTabBase>();
    private List<string> _tabNames = new List<string>();

    protected override void Awake()
    {
        base.Awake();
        _windowRect = new Rect(_defaultWindowPosition.x, _defaultWindowPosition.y,
                               _defaultWindowSize.x, _defaultWindowSize.y);
        InitializeTabs();
    }

    private void Start()
    {
        // Give managers time to initialize
        Invoke(nameof(RefreshCurrentTab), 1f);
    }

    private void Update()
    {
        // Toggle visibility with Shift+C
        if (Input.GetKey(_modifierKey) && Input.GetKeyDown(_toggleKey))
        {
            _isVisible = !_isVisible;

            if (_isVisible)
            {
                RefreshCurrentTab();
            }
        }
    }

    #region Initialization

    private void InitializeTabs()
    {
        AdminGUIStyles.Initialize();

        // Initialize all tabs
        AddTab(new AdminRoomsTab(), "Rooms");
        AddTab(new AdminEntitiesTab(), "Entities");
        AddTab(new AdminPurchasablesTab(), "Purchasables");
        AddTab(new AdminResourcesTab(), "Resources");
        AddTab(new AdminStatsTab(), "Stats");
        AddTab(new AdminAchievementsTab(), "Achievements");
        AddTab(new AdminTimersTab(), "Timers");
        AddTab(new AdminZonesTab(), "Zones");
        AddTab(new AdminInspectorTab(), "Inspector");
        AddTab(new AdminToolsTab(), "Tools");
    }

    private void AddTab(AdminTabBase tab, string tabName)
    {
        tab.Initialize(this);
        _tabs.Add(tab);
        _tabNames.Add(tabName);
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        if (!_isVisible) return;

        AdminGUIStyles.EnsureInitialized();

        _windowRect = GUI.Window(1000, _windowRect, DrawAdminWindow, "Admin Console", AdminGUIStyles.WindowStyle);
    }

    private void DrawAdminWindow(int windowID)
    {
        GUILayout.BeginVertical();

        // Draw tabs
        DrawTabs();

        GUILayout.Space(10);

        // Draw current tab content
        if (_currentTab >= 0 && _currentTab < _tabs.Count)
        {
            _tabs[_currentTab].DrawTab();
        }

        GUILayout.EndVertical();

        // Make window draggable
        GUI.DragWindow();
    }

    private void DrawTabs()
    {
        GUILayout.BeginHorizontal();

        for (int i = 0; i < _tabs.Count; i++)
        {
            bool isActive = _currentTab == i;
            GUIStyle style = isActive ? AdminGUIStyles.ActiveTabButtonStyle : AdminGUIStyles.TabButtonStyle;

            if (GUILayout.Button(_tabNames[i], style, GUILayout.Height(30)))
            {
                SwitchToTab(i);
            }
        }

        GUILayout.EndHorizontal();
    }

    #endregion

    #region Tab Management

    private void SwitchToTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= _tabs.Count) return;

        // Notify old tab it's closing
        if (_currentTab >= 0 && _currentTab < _tabs.Count)
        {
            _tabs[_currentTab].OnTabClosed();
        }

        _currentTab = tabIndex;

        // Notify new tab it's opening
        _tabs[_currentTab].OnTabOpened();
    }

    private void RefreshCurrentTab()
    {
        if (_currentTab >= 0 && _currentTab < _tabs.Count)
        {
            _tabs[_currentTab].RefreshData();
        }
    }

    #endregion
}
