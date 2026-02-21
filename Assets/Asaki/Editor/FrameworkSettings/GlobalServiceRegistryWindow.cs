using System.Collections.Generic;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asaki.Editor.FrameworkSettings
{
    public class GlobalServiceRegistryWindow : EditorWindow
    {
        private const string REGISTRY_ASSET_PATH = "Assets/Resources/GlobalServiceRegistry.asset";

        private GlobalServiceRegistry _registry;
        private AsakiFrameworkSetting _frameworkSetting;

        private Vector2 _scrollPosition;
        private int _selectedIndex = -1;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _entryStyle;
        private GUIStyle _validStyle;
        private GUIStyle _invalidStyle;
        private GUIStyle _disabledStyle;
        private GUIStyle _toolbarButtonStyle;

        private GUIContent _addIcon;
        private GUIContent _removeIcon;
        private GUIContent _upIcon;
        private GUIContent _downIcon;
        private GUIContent _sortIcon;

        private bool _showSettings = true;
        private bool _showStatistics = true;
        private bool _showHelp = false;

        private string _searchFilter = "";
        private List<GlobalServiceEntry> _filteredEntries = new List<GlobalServiceEntry>();

        [MenuItem("Asaki/Global Services/Service Registry Window", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<GlobalServiceRegistryWindow>("Global Service Registry");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        [MenuItem("Asaki/Global Services/Create Registry Asset", false, 1)]
        public static GlobalServiceRegistry CreateRegistryAsset()
        {
            var registry = GetOrCreateRegistryAsset();
            Selection.activeObject = registry;
            EditorGUIUtility.PingObject(registry);
            return registry;
        }

        private void OnEnable()
        {
            RefreshState();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                RefreshState();
                Repaint();
            }
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            RefreshState();
            Repaint();
        }

        private void RefreshState()
        {
            _registry = GetOrCreateRegistryAsset();
            _frameworkSetting = GetFrameworkSetting();
            UpdateFilteredEntries();
        }

        private void InitStyles()
        {
            if (_headerStyle != null)
                return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 10),
                alignment = TextAnchor.MiddleCenter,
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5),
            };

            _entryStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 2, 2),
            };

            _validStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                fontStyle = FontStyle.Bold,
            };

            _invalidStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.3f, 0.3f) },
                fontStyle = FontStyle.Bold,
            };

            _disabledStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
            };

            _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedWidth = 30,
                fixedHeight = 22,
            };

            _addIcon = EditorGUIUtility.IconContent("Toolbar Plus", "|Add new service entry");
            _removeIcon = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove selected entry");
            _upIcon = EditorGUIUtility.IconContent("upArrow", "|Move up");
            _downIcon = EditorGUIUtility.IconContent("downArrow", "|Move down");
            _sortIcon = EditorGUIUtility.IconContent("AlphabeticalSorting", "|Sort by priority");
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginVertical();

            DrawHeader();

            EditorGUILayout.Space(5);

            DrawToolbar();

            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawSearchBar();

            EditorGUILayout.Space(5);

            DrawServiceList();

            EditorGUILayout.Space(10);

            if (_showStatistics)
            {
                DrawStatistics();
            }

            EditorGUILayout.Space(10);

            if (_showSettings)
            {
                DrawSettings();
            }

            EditorGUILayout.Space(10);

            if (_showHelp)
            {
                DrawHelp();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("🌐 Global Service Registry", _headerStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (_registry != null)
            {
                GUILayout.Label($"Version: {_registry.Version}", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(_addIcon, _toolbarButtonStyle))
            {
                AddNewEntry();
            }

            GUI.enabled = _selectedIndex >= 0 && _registry != null && _registry.Count > 0;
            if (GUILayout.Button(_removeIcon, _toolbarButtonStyle))
            {
                RemoveSelectedEntry();
            }
            GUI.enabled = true;

            GUILayout.Space(5);

            GUI.enabled = _selectedIndex > 0;
            if (GUILayout.Button(_upIcon, _toolbarButtonStyle))
            {
                MoveEntryUp();
            }
            GUI.enabled = true;

            GUI.enabled =
                _selectedIndex >= 0 && _registry != null && _selectedIndex < _registry.Count - 1;
            if (GUILayout.Button(_downIcon, _toolbarButtonStyle))
            {
                MoveEntryDown();
            }
            GUI.enabled = true;

            GUILayout.Space(5);

            GUI.enabled = _registry != null && _registry.Count > 0;
            if (GUILayout.Button(_sortIcon, _toolbarButtonStyle))
            {
                SortByPriority();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            _showSettings = GUILayout.Toggle(
                _showSettings,
                "⚙",
                EditorStyles.toolbarButton,
                GUILayout.Width(30)
            );
            _showStatistics = GUILayout.Toggle(
                _showStatistics,
                "📊",
                EditorStyles.toolbarButton,
                GUILayout.Width(30)
            );
            _showHelp = GUILayout.Toggle(
                _showHelp,
                "?",
                EditorStyles.toolbarButton,
                GUILayout.Width(30)
            );

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("🔍", GUILayout.Width(25));

            string newFilter = GUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (newFilter != _searchFilter)
            {
                _searchFilter = newFilter;
                UpdateFilteredEntries();
            }

            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                _searchFilter = "";
                UpdateFilteredEntries();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void UpdateFilteredEntries()
        {
            _filteredEntries.Clear();

            if (_registry == null)
                return;

            foreach (var entry in _registry.ServiceEntries)
            {
                if (string.IsNullOrEmpty(_searchFilter))
                {
                    _filteredEntries.Add(entry);
                    continue;
                }

                if (
                    entry.Prefab != null
                    && entry.Prefab.name.ToLower().Contains(_searchFilter.ToLower())
                )
                {
                    _filteredEntries.Add(entry);
                    continue;
                }

                if (
                    !string.IsNullOrEmpty(entry.Description)
                    && entry.Description.ToLower().Contains(_searchFilter.ToLower())
                )
                {
                    _filteredEntries.Add(entry);
                }
            }
        }

        private void DrawServiceList()
        {
            if (_registry == null)
            {
                EditorGUILayout.HelpBox(
                    "No GlobalServiceRegistry found. Click 'Create' to create one.",
                    MessageType.Warning
                );
                if (GUILayout.Button("Create Registry Asset", GUILayout.Height(30)))
                {
                    CreateRegistryAsset();
                    RefreshState();
                }
                return;
            }

            if (_filteredEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No service entries. Click '+' to add a new service.",
                    MessageType.Info
                );
                return;
            }

            for (int i = 0; i < _filteredEntries.Count; i++)
            {
                var entry = _filteredEntries[i];
                int actualIndex = _registry.IndexOf(entry);

                DrawServiceEntry(entry, actualIndex, i);
            }
        }

        private void DrawServiceEntry(GlobalServiceEntry entry, int actualIndex, int displayIndex)
        {
            bool isSelected = _selectedIndex == actualIndex;
            bool isValid = entry.Prefab != null && HasGlobalServiceComponent(entry.Prefab);

            Color bgColor = GUI.backgroundColor;

            if (!entry.Enabled)
            {
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            }
            else if (!isValid && entry.Prefab != null)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f, 0.3f);
            }
            else if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
            }

            EditorGUILayout.BeginVertical(_entryStyle);

            EditorGUILayout.BeginHorizontal();

            Rect entryRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));

            if (
                Event.current.type == EventType.MouseDown
                && entryRect.Contains(Event.current.mousePosition)
            )
            {
                _selectedIndex = actualIndex;
                Repaint();
            }

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"#{actualIndex + 1}", EditorStyles.boldLabel, GUILayout.Width(30));

            EditorGUI.BeginChangeCheck();
            GameObject newPrefab = (GameObject)
                EditorGUILayout.ObjectField(
                    entry.Prefab,
                    typeof(GameObject),
                    false,
                    GUILayout.MinWidth(150)
                );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_registry, "Change Service Prefab");
                entry.Prefab = newPrefab;
                _registry.SetModified();
                EditorUtility.SetDirty(_registry);
            }

            string statusIcon = entry.Enabled
                ? (isValid ? "✅" : (entry.Prefab == null ? "⚪" : "❌"))
                : "⏸️";

            GUIStyle statusStyle = entry.Enabled
                ? (isValid ? _validStyle : (entry.Prefab == null ? _disabledStyle : _invalidStyle))
                : _disabledStyle;

            GUILayout.Label(statusIcon, statusStyle, GUILayout.Width(25));

            EditorGUI.BeginChangeCheck();
            bool newEnabled = EditorGUILayout.Toggle(entry.Enabled, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_registry, "Toggle Service Enabled");
                entry.Enabled = newEnabled;
                _registry.SetModified();
                EditorUtility.SetDirty(_registry);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Priority:", GUILayout.Width(55));

            EditorGUI.BeginChangeCheck();
            int newPriority = EditorGUILayout.IntSlider(
                entry.Priority,
                0,
                100,
                GUILayout.Width(150)
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_registry, "Change Service Priority");
                entry.Priority = newPriority;
                _registry.SetModified();
                EditorUtility.SetDirty(_registry);
            }

            GUILayout.Label("Desc:", GUILayout.Width(35));

            EditorGUI.BeginChangeCheck();
            string newDesc = EditorGUILayout.TextField(entry.Description);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_registry, "Change Service Description");
                entry.Description = newDesc;
                _registry.SetModified();
                EditorUtility.SetDirty(_registry);
            }

            EditorGUILayout.EndHorizontal();

            if (entry.Prefab != null && !isValid)
            {
                EditorGUILayout.HelpBox(
                    "Prefab does not contain any IAsakiGlobalService component!",
                    MessageType.Warning
                );
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = bgColor;
        }

        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("📊 Statistics", EditorStyles.boldLabel);

            if (_registry == null)
            {
                GUILayout.Label("No registry loaded.");
                EditorGUILayout.EndVertical();
                return;
            }

            int total = _registry.Count;
            int enabled = 0;
            int valid = 0;
            int invalid = 0;

            foreach (var entry in _registry.ServiceEntries)
            {
                if (entry.Enabled)
                    enabled++;

                if (entry.Prefab != null)
                {
                    if (HasGlobalServiceComponent(entry.Prefab))
                        valid++;
                    else
                        invalid++;
                }
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUILayout.Label($"Total: {total}", EditorStyles.boldLabel);
            GUILayout.Label($"Enabled: {enabled}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label($"Valid: {valid}", _validStyle);
            GUILayout.Label(
                $"Invalid: {invalid}",
                invalid > 0 ? _invalidStyle : EditorStyles.label
            );
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("⚙️ Settings", EditorStyles.boldLabel);

            if (_registry == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();
            bool validateOnStart = EditorGUILayout.Toggle(
                "Validate On Start",
                _registry.ValidateOnStart
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_registry, "Change Validate On Start");
                var prop = new SerializedObject(_registry).FindProperty("_validateOnStart");
                prop.boolValue = validateOnStart;
                prop.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_registry);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Validate All", GUILayout.Height(25)))
            {
                ValidateAllEntries();
            }

            if (GUILayout.Button("Remove Invalid", GUILayout.Height(25)))
            {
                RemoveInvalidEntries();
            }

            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Clear All",
                        "Are you sure you want to remove all service entries?",
                        "Yes",
                        "No"
                    )
                )
                {
                    Undo.RecordObject(_registry, "Clear All Services");
                    _registry.ClearAll();
                    EditorUtility.SetDirty(_registry);
                    _selectedIndex = -1;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawHelp()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("❓ Help", EditorStyles.boldLabel);

            GUILayout.Label("Usage:", EditorStyles.boldLabel);
            GUILayout.Label("1. Add service prefabs by clicking '+' button or drag & drop");
            GUILayout.Label("2. Toggle services on/off with the checkbox");
            GUILayout.Label("3. Set priority to control initialization order (lower = earlier)");
            GUILayout.Label("4. Click 'Sort' to reorder by priority");

            EditorGUILayout.Space(5);

            GUILayout.Label("Requirements:", EditorStyles.boldLabel);
            GUILayout.Label("• Prefabs must contain IAsakiGlobalService components");
            GUILayout.Label("• Services are instantiated in framework bootstrap phase");
            GUILayout.Label("• Configuration is shared across all scenes");

            EditorGUILayout.Space(5);

            GUILayout.Label("Integration:", EditorStyles.boldLabel);

            if (_frameworkSetting == null)
            {
                GUILayout.Label("⚠️ No AsakiFrameworkSetting found!", _invalidStyle);
            }
            else if (_frameworkSetting.GlobalServiceRegistry != _registry)
            {
                GUILayout.Label(
                    "⚠️ This registry is not linked to AsakiFrameworkSetting!",
                    _invalidStyle
                );
                if (GUILayout.Button("Link to Framework Setting", GUILayout.Height(25)))
                {
                    LinkToFrameworkSetting();
                }
            }
            else
            {
                GUILayout.Label("✅ Linked to AsakiFrameworkSetting", _validStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void AddNewEntry()
        {
            if (_registry == null)
                return;

            Undo.RecordObject(_registry, "Add Service Entry");

            var newEntry = new GlobalServiceEntry { Enabled = true, Priority = 50 };

            _registry.AddServiceEntry(newEntry);
            _selectedIndex = _registry.Count - 1;
            UpdateFilteredEntries();
            EditorUtility.SetDirty(_registry);
        }

        private void RemoveSelectedEntry()
        {
            if (_registry == null || _selectedIndex < 0 || _selectedIndex >= _registry.Count)
                return;

            Undo.RecordObject(_registry, "Remove Service Entry");
            _registry.RemoveServiceEntry(_selectedIndex);
            _selectedIndex = -1;
            UpdateFilteredEntries();
            EditorUtility.SetDirty(_registry);
        }

        private void MoveEntryUp()
        {
            if (_registry == null || _selectedIndex <= 0)
                return;

            Undo.RecordObject(_registry, "Move Service Entry Up");
            _registry.MoveEntry(_selectedIndex, _selectedIndex - 1);
            _selectedIndex--;
            UpdateFilteredEntries();
            EditorUtility.SetDirty(_registry);
        }

        private void MoveEntryDown()
        {
            if (_registry == null || _selectedIndex < 0 || _selectedIndex >= _registry.Count - 1)
                return;

            Undo.RecordObject(_registry, "Move Service Entry Down");
            _registry.MoveEntry(_selectedIndex, _selectedIndex + 1);
            _selectedIndex++;
            UpdateFilteredEntries();
            EditorUtility.SetDirty(_registry);
        }

        private void SortByPriority()
        {
            if (_registry == null)
                return;

            Undo.RecordObject(_registry, "Sort Services by Priority");
            _registry.SortByPriority();
            UpdateFilteredEntries();
            EditorUtility.SetDirty(_registry);
        }

        private void ValidateAllEntries()
        {
            if (_registry == null)
                return;

            int invalidCount = 0;
            foreach (var entry in _registry.ServiceEntries)
            {
                if (entry.Prefab != null && !HasGlobalServiceComponent(entry.Prefab))
                {
                    invalidCount++;
                    Debug.LogWarning(
                        $"[GlobalServiceRegistry] Invalid prefab: {entry.Prefab.name}"
                    );
                }
            }

            if (invalidCount == 0)
            {
                EditorUtility.DisplayDialog(
                    "Validation Result",
                    "All service entries are valid!",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Validation Result",
                    $"Found {invalidCount} invalid entries. Check console for details.",
                    "OK"
                );
            }
        }

        private void RemoveInvalidEntries()
        {
            if (_registry == null)
                return;

            var toRemove = new List<int>();
            for (int i = _registry.Count - 1; i >= 0; i--)
            {
                var entry = _registry[i];
                if (entry.Prefab != null && !HasGlobalServiceComponent(entry.Prefab))
                {
                    toRemove.Add(i);
                }
            }

            if (toRemove.Count == 0)
            {
                EditorUtility.DisplayDialog("Remove Invalid", "No invalid entries found.", "OK");
                return;
            }

            if (
                EditorUtility.DisplayDialog(
                    "Remove Invalid",
                    $"Remove {toRemove.Count} invalid entries?",
                    "Yes",
                    "No"
                )
            )
            {
                Undo.RecordObject(_registry, "Remove Invalid Entries");
                foreach (int index in toRemove)
                {
                    _registry.RemoveServiceEntry(index);
                }
                _selectedIndex = -1;
                UpdateFilteredEntries();
                EditorUtility.SetDirty(_registry);
            }
        }

        private void LinkToFrameworkSetting()
        {
            if (_frameworkSetting == null || _registry == null)
                return;

            Undo.RecordObject(_frameworkSetting, "Link GlobalServiceRegistry");
            var prop = new SerializedObject(_frameworkSetting).FindProperty(
                "_globalServiceRegistry"
            );
            prop.objectReferenceValue = _registry;
            prop.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_frameworkSetting);
        }

        private bool HasGlobalServiceComponent(GameObject prefab)
        {
            if (prefab == null)
                return false;
            var components = prefab.GetComponentsInChildren<IAsakiGlobalService>(true);
            return components != null && components.Length > 0;
        }

        private static GlobalServiceRegistry GetOrCreateRegistryAsset()
        {
            var registry = Resources.Load<GlobalServiceRegistry>("GlobalServiceRegistry");
            if (registry != null)
                return registry;

            string[] guids = AssetDatabase.FindAssets("t:GlobalServiceRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GlobalServiceRegistry>(path);
            }

            if (!System.IO.Directory.Exists("Assets/Resources"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources");
            }

            registry = ScriptableObject.CreateInstance<GlobalServiceRegistry>();
            AssetDatabase.CreateAsset(registry, REGISTRY_ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GlobalServiceRegistry] Created new registry at: {REGISTRY_ASSET_PATH}");
            return registry;
        }

        private static AsakiFrameworkSetting GetFrameworkSetting()
        {
            var config = Resources.Load<AsakiFrameworkSetting>("AsakiFrameworkSetting");
            if (config != null)
                return config;

            string[] guids = AssetDatabase.FindAssets("t:AsakiFrameworkSetting");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AsakiFrameworkSetting>(path);
            }

            return null;
        }
    }
}
