using LargeNumbers;
using LGD.Core;
using LGD.Core.Singleton;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// UGUI-based admin console for in-game debugging and testing
/// Toggle with Shift+C
/// </summary>
public class AdminConsoleManager : MonoSingleton<AdminConsoleManager>
{
    [SerializeField, FoldoutGroup("Settings")]
    private KeyCode _modifierKey = KeyCode.LeftShift;

    [SerializeField, FoldoutGroup("Settings")]
    private KeyCode _toggleKey = KeyCode.C;

    [SerializeField, FoldoutGroup("Settings")]
    private Vector2 _defaultWindowSize = new Vector2(800, 600);

    [SerializeField, FoldoutGroup("Settings")]
    private Vector2 _defaultWindowPosition = new Vector2(100, 100);

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private bool _isVisible = false;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private int _currentTab = 0;

    private Rect _windowRect;
    private Vector2[] _scrollPositions = new Vector2[10]; // Scroll positions for each tab

    // Tab indices
    private const int TAB_ROOMS = 0;
    private const int TAB_ENTITIES = 1;
    private const int TAB_PURCHASABLES = 2;
    private const int TAB_RESOURCES = 3;
    private const int TAB_TOOLS = 4;

    // Rooms tab state
    private int _selectedRoomIndex = 0;
    private List<RoomRuntimeData> _allRooms = new List<RoomRuntimeData>();
    private string[] _roomNames = new string[0];

    // Entities tab state
    private int _selectedEntityIndex = 0;
    private List<EntityBlueprint> _allEntityBlueprints = new List<EntityBlueprint>();
    private string[] _entityNames = new string[0];
    private Vector3 _spawnPosition = Vector3.zero;

    // Purchasables tab state
    private Vector2 _purchasablesScroll = Vector2.zero;
    private List<BasePurchasableRuntimeData> _allPurchasables = new List<BasePurchasableRuntimeData>();
    private Dictionary<string, BasePurchasable> _purchasableBlueprintCache = new Dictionary<string, BasePurchasable>();

    // Resources tab state
    private Vector2 _resourcesScroll = Vector2.zero;
    private List<Resource> _allResources = new List<Resource>();
    private Dictionary<Resource, string> _resourceInputs = new Dictionary<Resource, string>();

    // Tools tab state
    private float _timeScale = 1f;

    // GUI Styles
    private GUIStyle _windowStyle;
    private GUIStyle _tabButtonStyle;
    private GUIStyle _activeTabButtonStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _buttonStyle;
    private bool _stylesInitialized = false;

    protected override void Awake()
    {
        base.Awake();
        _windowRect = new Rect(_defaultWindowPosition.x, _defaultWindowPosition.y,
                               _defaultWindowSize.x, _defaultWindowSize.y);
    }

    private void Start()
    {
        // Give managers time to initialize
        Invoke(nameof(InitializeAdminData), 1f);
    }

    private void Update()
    {
        // Toggle visibility with Shift+C
        if (Input.GetKey(_modifierKey) && Input.GetKeyDown(_toggleKey))
        {
            _isVisible = !_isVisible;

            if (_isVisible)
            {
                RefreshData();
            }
        }
    }

    private void InitializeAdminData()
    {
        RefreshData();
    }

    private void RefreshData()
    {
        RefreshRoomsData();
        RefreshEntitiesData();
        RefreshPurchasablesData();
        RefreshResourcesData();
    }

    #region Data Refresh Methods

    private void RefreshRoomsData()
    {
        if (RoomManager.Instance != null)
        {
            _allRooms = RoomManager.Instance.GetAllRooms();
            _roomNames = _allRooms.Select(r =>
            {
                string status = r.isUnlocked ? "[UNLOCKED]" : "[LOCKED]";
                return $"{status} {r.roomId}";
            }).ToArray();

            if (_selectedRoomIndex >= _roomNames.Length)
                _selectedRoomIndex = 0;
        }
    }

    private void RefreshEntitiesData()
    {
        var entityRegistry = RegistryManager.Instance?.GetRegistry<EntityBlueprint>() as EntityRegistry;
        if (entityRegistry != null)
        {
            _allEntityBlueprints = entityRegistry.GetAllItems();
            _entityNames = _allEntityBlueprints.Select(e => e.displayName).ToArray();

            if (_selectedEntityIndex >= _entityNames.Length)
                _selectedEntityIndex = 0;
        }
    }

    private void RefreshPurchasablesData()
    {
        if (PurchasableManager.Instance != null && PurchasableManager.Instance.IsInitialized())
        {
            _allPurchasables = PurchasableManager.Instance.GetAllPurchasables();

            // Cache blueprints
            var purchasableRegistry = RegistryManager.Instance?.GetRegistry<BasePurchasable>() as PurchasableRegistry;
            if (purchasableRegistry != null)
            {
                _purchasableBlueprintCache.Clear();
                foreach (var runtime in _allPurchasables)
                {
                    var blueprint = purchasableRegistry.GetItemById(runtime.purchasableId);
                    if (blueprint != null)
                    {
                        _purchasableBlueprintCache[runtime.purchasableId] = blueprint;
                    }
                }
            }
        }
    }

    private void RefreshResourcesData()
    {
        var resourceRegistry = RegistryManager.Instance?.GetRegistry<Resource>() as ResourceRegistry;
        if (resourceRegistry != null && ResourceManager.Instance != null)
        {
            _allResources = resourceRegistry.GetAllItems();

            // Initialize input fields for resources
            foreach (var resource in _allResources)
            {
                if (!_resourceInputs.ContainsKey(resource))
                {
                    _resourceInputs[resource] = "1000";
                }
            }
        }
    }

    #endregion

    #region GUI Drawing

    private void OnGUI()
    {
        if (!_isVisible) return;

        InitializeStyles();

        _windowRect = GUI.Window(1000, _windowRect, DrawAdminWindow, "Admin Console", _windowStyle);
    }

    private void InitializeStyles()
    {
        if (_stylesInitialized) return;

        _windowStyle = new GUIStyle(GUI.skin.window)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _tabButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        _activeTabButtonStyle = new GUIStyle(_tabButtonStyle)
        {
            normal = { textColor = Color.cyan }
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12
        };

        _stylesInitialized = true;
    }

    private void DrawAdminWindow(int windowID)
    {
        GUILayout.BeginVertical();

        // Draw tabs
        DrawTabs();

        GUILayout.Space(10);

        // Draw current tab content
        switch (_currentTab)
        {
            case TAB_ROOMS:
                DrawRoomsTab();
                break;
            case TAB_ENTITIES:
                DrawEntitiesTab();
                break;
            case TAB_PURCHASABLES:
                DrawPurchasablesTab();
                break;
            case TAB_RESOURCES:
                DrawResourcesTab();
                break;
            case TAB_TOOLS:
                DrawToolsTab();
                break;
        }

        GUILayout.EndVertical();

        // Make window draggable
        GUI.DragWindow();
    }

    private void DrawTabs()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Rooms", _currentTab == TAB_ROOMS ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
        {
            _currentTab = TAB_ROOMS;
            RefreshRoomsData();
        }

        if (GUILayout.Button("Entities", _currentTab == TAB_ENTITIES ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
        {
            _currentTab = TAB_ENTITIES;
            RefreshEntitiesData();
        }

        if (GUILayout.Button("Purchasables", _currentTab == TAB_PURCHASABLES ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
        {
            _currentTab = TAB_PURCHASABLES;
            RefreshPurchasablesData();
        }

        if (GUILayout.Button("Resources", _currentTab == TAB_RESOURCES ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
        {
            _currentTab = TAB_RESOURCES;
            RefreshResourcesData();
        }

        if (GUILayout.Button("Tools", _currentTab == TAB_TOOLS ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
        {
            _currentTab = TAB_TOOLS;
        }

        GUILayout.EndHorizontal();
    }

    #endregion

    #region Tab Drawing

    private void DrawRoomsTab()
    {
        GUILayout.Label("Room Management", _headerStyle);
        GUILayout.Space(5);

        if (_roomNames.Length == 0)
        {
            GUILayout.Label("No rooms found. Make sure RoomManager is initialized.");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Select Room:", GUILayout.Width(100));
        _selectedRoomIndex = GUILayout.SelectionGrid(_selectedRoomIndex, _roomNames, 1, GUILayout.Height(200));
        GUILayout.EndHorizontal();

        if (_selectedRoomIndex < _allRooms.Count)
        {
            RoomRuntimeData selectedRoom = _allRooms[_selectedRoomIndex];

            GUILayout.Space(10);
            GUILayout.Label($"Room ID: {selectedRoom.roomId}");
            GUILayout.Label($"Status: {(selectedRoom.isUnlocked ? "UNLOCKED" : "LOCKED")}");

            if (selectedRoom.isUnlocked)
            {
                GUILayout.Label($"Unlocked At: {selectedRoom.timeUnlocked}");
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (selectedRoom.isUnlocked)
            {
                if (GUILayout.Button("Lock Room", _buttonStyle, GUILayout.Height(40)))
                {
                    LockRoom(selectedRoom);
                }
            }
            else
            {
                if (GUILayout.Button("Unlock Room", _buttonStyle, GUILayout.Height(40)))
                {
                    UnlockRoom(selectedRoom);
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawEntitiesTab()
    {
        GUILayout.Label("Entity Spawning", _headerStyle);
        GUILayout.Space(5);

        if (_entityNames.Length == 0)
        {
            GUILayout.Label("No entity blueprints found.");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Select Entity:", GUILayout.Width(120));
        _selectedEntityIndex = GUILayout.SelectionGrid(_selectedEntityIndex, _entityNames, 1, GUILayout.Height(200));
        GUILayout.EndHorizontal();

        if (_selectedEntityIndex < _allEntityBlueprints.Count)
        {
            EntityBlueprint selectedEntity = _allEntityBlueprints[_selectedEntityIndex];

            GUILayout.Space(10);
            GUILayout.Label($"Entity: {selectedEntity.displayName}");
            GUILayout.Label($"ID: {selectedEntity.id}");

            GUILayout.Space(10);
            GUILayout.Label("Spawn Position:");

            GUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(20));
            string xStr = GUILayout.TextField(_spawnPosition.x.ToString("F2"), GUILayout.Width(80));
            GUILayout.Label("Y:", GUILayout.Width(20));
            string yStr = GUILayout.TextField(_spawnPosition.y.ToString("F2"), GUILayout.Width(80));
            GUILayout.Label("Z:", GUILayout.Width(20));
            string zStr = GUILayout.TextField(_spawnPosition.z.ToString("F2"), GUILayout.Width(80));
            GUILayout.EndHorizontal();

            // Parse position inputs
            if (float.TryParse(xStr, out float x)) _spawnPosition.x = x;
            if (float.TryParse(yStr, out float y)) _spawnPosition.y = y;
            if (float.TryParse(zStr, out float z)) _spawnPosition.z = z;

            GUILayout.Space(5);

            if (GUILayout.Button("Set to Camera Position", _buttonStyle))
            {
                if (Camera.main != null)
                {
                    _spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 5f;
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button($"Spawn {selectedEntity.displayName}", _buttonStyle, GUILayout.Height(40)))
            {
                SpawnEntity(selectedEntity);
            }

            GUILayout.Space(10);
            GUILayout.Label($"Total Entities: {EntityManager.Instance?.GetEntityCount() ?? 0}");
        }
    }

    private void DrawPurchasablesTab()
    {
        GUILayout.Label("Purchasable Management", _headerStyle);
        GUILayout.Space(5);

        if (_allPurchasables.Count == 0)
        {
            GUILayout.Label("No purchasables found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", _buttonStyle))
        {
            RefreshPurchasablesData();
        }
        if (GUILayout.Button("Reset All to 0", _buttonStyle))
        {
            ResetAllPurchases();
        }
        if (GUILayout.Button("Max All Purchases", _buttonStyle))
        {
            MaxAllPurchases();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        _scrollPositions[TAB_PURCHASABLES] = GUILayout.BeginScrollView(_scrollPositions[TAB_PURCHASABLES], GUILayout.Height(450));

        foreach (var runtimeData in _allPurchasables)
        {
            if (!_purchasableBlueprintCache.TryGetValue(runtimeData.purchasableId, out BasePurchasable blueprint))
                continue;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label($"{blueprint.displayName}", _headerStyle);
            GUILayout.Label($"ID: {blueprint.purchasableId}");
            GUILayout.Label($"Type: {blueprint.purchaseType}");
            GUILayout.Label($"Purchase Count: {runtimeData.purchaseCount}");
            GUILayout.Label($"Active: {runtimeData.isActive}");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("-1", GUILayout.Width(50)))
            {
                ModifyPurchaseCount(runtimeData, -1);
            }

            if (GUILayout.Button("+1", GUILayout.Width(50)))
            {
                ModifyPurchaseCount(runtimeData, 1);
            }

            if (GUILayout.Button("+10", GUILayout.Width(50)))
            {
                ModifyPurchaseCount(runtimeData, 10);
            }

            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                runtimeData.purchaseCount = 0;
                runtimeData.isActive = false;
                SavePurchasables();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }

    private void DrawResourcesTab()
    {
        GUILayout.Label("Resource Management", _headerStyle);
        GUILayout.Space(5);

        if (_allResources.Count == 0)
        {
            GUILayout.Label("No resources found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", _buttonStyle))
        {
            RefreshResourcesData();
        }
        if (GUILayout.Button("Clear All Resources", _buttonStyle))
        {
            ClearAllResources();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        _scrollPositions[TAB_RESOURCES] = GUILayout.BeginScrollView(_scrollPositions[TAB_RESOURCES], GUILayout.Height(450));

        foreach (var resource in _allResources)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label($"{resource.displayName}", _headerStyle);

            AlphabeticNotation currentAmount = ResourceManager.Instance.GetResourceAmount(resource);
            GUILayout.Label($"Current: {currentAmount.ToString()}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount:", GUILayout.Width(80));

            if (_resourceInputs.TryGetValue(resource, out string currentInput))
            {
                _resourceInputs[resource] = GUILayout.TextField(currentInput, GUILayout.Width(150));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add", GUILayout.Width(80)))
            {
                if (double.TryParse(_resourceInputs[resource], out double amount))
                {
                    ResourceManager.Instance.AddResource(resource, new AlphabeticNotation(amount));
                }
            }

            if (GUILayout.Button("Set", GUILayout.Width(80)))
            {
                if (double.TryParse(_resourceInputs[resource], out double amount))
                {
                    ResourceManager.Instance.SetResource(resource, new AlphabeticNotation(amount));
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                ResourceManager.Instance.SetResource(resource, AlphabeticNotation.zero);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }

    private void DrawToolsTab()
    {
        GUILayout.Label("Development Tools", _headerStyle);
        GUILayout.Space(10);

        // Time Scale
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Time Scale", _headerStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Current: {Time.timeScale:F2}x", GUILayout.Width(150));
        _timeScale = GUILayout.HorizontalSlider(_timeScale, 0.1f, 10f, GUILayout.Width(300));
        if (GUILayout.Button("Apply", GUILayout.Width(80)))
        {
            Time.timeScale = _timeScale;
        }
        if (GUILayout.Button("Reset", GUILayout.Width(80)))
        {
            Time.timeScale = 1f;
            _timeScale = 1f;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Save/Load Tools
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Save/Load", _headerStyle);
        if (GUILayout.Button("Force Save All", _buttonStyle, GUILayout.Height(40)))
        {
            ForceSaveAll();
        }
        if (GUILayout.Button("Delete All Saves (Dangerous!)", _buttonStyle, GUILayout.Height(40)))
        {
            DeleteAllSaves();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Room Tools
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Room Tools", _headerStyle);
        if (GUILayout.Button("Unlock All Rooms", _buttonStyle, GUILayout.Height(40)))
        {
            UnlockAllRooms();
        }
        if (GUILayout.Button("Lock All Rooms", _buttonStyle, GUILayout.Height(40)))
        {
            LockAllRooms();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // General Info
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Game Info", _headerStyle);
        GUILayout.Label($"FPS: {(1f / Time.smoothDeltaTime):F0}");
        GUILayout.Label($"Time Scale: {Time.timeScale:F2}x");
        GUILayout.Label($"Total Entities: {EntityManager.Instance?.GetEntityCount() ?? 0}");
        GUILayout.Label($"Unlocked Rooms: {RoomManager.Instance?.GetUnlockedRoomCount() ?? 0}");
        GUILayout.EndVertical();
    }

    #endregion

    #region Admin Actions

    private void UnlockRoom(RoomRuntimeData room)
    {
        room.Unlock();
        StartCoroutine(RoomManager.Instance.ManualSave());
        StartCoroutine(RoomManager.Instance.RestoreUnlockedRooms());
        RefreshRoomsData();
        DebugManager.Log($"[Admin] Unlocked room: {room.roomId}");
    }

    private void LockRoom(RoomRuntimeData room)
    {
        room.isUnlocked = false;
        StartCoroutine(RoomManager.Instance.ManualSave());
        RefreshRoomsData();
        DebugManager.Log($"[Admin] Locked room: {room.roomId}");
    }

    private void SpawnEntity(EntityBlueprint blueprint)
    {
        if (blueprint.prefab == null)
        {
            DebugManager.Error($"[Admin] Entity blueprint {blueprint.id} has no prefab!");
            return;
        }

        // Create new runtime data
        EntityRuntimeData runtimeData = new EntityRuntimeData();
        runtimeData.Initialise(blueprint);
        runtimeData.worldPosition = new SerializableVector3(_spawnPosition);

        // Spawn the GameObject
        EntityController controller = Instantiate(blueprint.prefab, _spawnPosition, Quaternion.identity);
        controller.Initialise(runtimeData, blueprint);

        DebugManager.Log($"[Admin] Spawned entity: {blueprint.displayName} at {_spawnPosition}");
    }

    private void ModifyPurchaseCount(BasePurchasableRuntimeData runtimeData, int delta)
    {
        runtimeData.purchaseCount = Mathf.Max(0, runtimeData.purchaseCount + delta);

        if (runtimeData.purchaseCount > 0)
        {
            runtimeData.isActive = true;
        }

        SavePurchasables();
        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
    }

    private void ResetAllPurchases()
    {
        foreach (var purchasable in _allPurchasables)
        {
            purchasable.purchaseCount = 0;
            purchasable.isActive = false;
        }
        SavePurchasables();
        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[Admin] Reset all purchases");
    }

    private void MaxAllPurchases()
    {
        foreach (var runtimeData in _allPurchasables)
        {
            if (_purchasableBlueprintCache.TryGetValue(runtimeData.purchasableId, out BasePurchasable blueprint))
            {
                if (blueprint.purchaseType == PurchaseType.Infinite)
                {
                    runtimeData.purchaseCount = 100;
                }
                else if (blueprint.purchaseType == PurchaseType.Capped && blueprint.maxPurchases > 0)
                {
                    runtimeData.purchaseCount = blueprint.maxPurchases;
                }
                else
                {
                    runtimeData.purchaseCount = 1;
                }
                runtimeData.isActive = true;
            }
        }
        SavePurchasables();
        ServiceBus.Publish(EntityEventIds.ON_STATS_RECALCULATION_REQUESTED, this);
        DebugManager.Log("[Admin] Maxed all purchases");
    }

    private void SavePurchasables()
    {
        StartCoroutine(PurchasableManager.Instance.ManualSave());
    }

    private void ClearAllResources()
    {
        foreach (var resource in _allResources)
        {
            ResourceManager.Instance.SetResource(resource, AlphabeticNotation.zero);
        }
        DebugManager.Log("[Admin] Cleared all resources");
    }

    private void ForceSaveAll()
    {
        StartCoroutine(SaveLoadProviderManager.Instance.SaveAll());
        DebugManager.Log("[Admin] Forced save all");
    }

    private void DeleteAllSaves()
    {
        SaveLoadProviderManager.Instance.DeleteAllSaves();
        DebugManager.Warning("[Admin] Deleted all saves! Restart the game for changes to take effect.");
    }

    private void UnlockAllRooms()
    {
        foreach (var room in _allRooms)
        {
            if (!room.isUnlocked)
            {
                room.Unlock();
            }
        }
        StartCoroutine(RoomManager.Instance.ManualSave());
        StartCoroutine(RoomManager.Instance.RestoreUnlockedRooms());
        RefreshRoomsData();
        DebugManager.Log("[Admin] Unlocked all rooms");
    }

    private void LockAllRooms()
    {
        foreach (var room in _allRooms)
        {
            room.isUnlocked = false;
        }
        StartCoroutine(RoomManager.Instance.ManualSave());
        RefreshRoomsData();
        DebugManager.Log("[Admin] Locked all rooms");
    }

    #endregion
}
