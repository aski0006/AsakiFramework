using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core.Architecture.Entities;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Entities
{
    /// <summary>
    /// 实体世界查看器 - 查看和管理所有实体
    /// </summary>
    public class EntityWorldWindow : EditorWindow
    {
        [MenuItem("Asaki/Entities/Entity World", false, 10)]
        public static void ShowWindow()
        {
            GetWindow<EntityWorldWindow>("Entity World");
        }

        // 反射获取EntityWorld
        private static FieldInfo _entitiesField;
        private static FieldInfo _generationsField;
        private static bool _reflectionInitialized;

        // 运行时数据
        private List<EntityInfo> _entityInfos = new();
        private IEntityWorld _cachedWorld;
        private double _lastUpdateTime;
        private const double RefreshInterval = 0.3f;

        // UI状态
        private Vector2 _entityScrollPos;
        private Vector2 _detailScrollPos;
        private string _searchFilter = "";
        private EntityInfo _selectedEntity;
        private bool _showActiveOnly = false;
        private bool _autoRefresh = true;
        private bool _showComponents = true;
        private bool _showStats = true;

        // 组件类型筛选
        private List<Type> _availableComponentTypes = new();
        private Type _selectedComponentType;

        // 统计
        private int _totalEntities;
        private int _activeEntities;
        private int _totalComponents;

        private class EntityInfo
        {
            public EntityId Id;
            public IEntity Entity;
            public bool IsValid;
            public int ComponentCount;
            public List<Type> ComponentTypes = new();
            public bool IsActive;
        }

        private void OnEnable()
        {
            InitializeReflection();
            RefreshAvailableComponentTypes();
        }

        private void InitializeReflection()
        {
            if (_reflectionInitialized)
                return;

            try
            {
                _entitiesField = typeof(EntityWorld).GetField(
                    "_entities",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                _generationsField = typeof(EntityWorld).GetField(
                    "_generations",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                _reflectionInitialized = _entitiesField != null && _generationsField != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EntityWorldWindow] Reflection init failed: {ex}");
            }
        }

        private void RefreshAvailableComponentTypes()
        {
            _availableComponentTypes = TypeCache
                .GetTypesDerivedFrom<IEntityComponent>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToList();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;
            if (!_autoRefresh)
                return;

            if (EditorApplication.timeSinceStartup - _lastUpdateTime > RefreshInterval)
            {
                RefreshData();
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void RefreshData()
        {
            _entityInfos.Clear();
            _totalEntities = 0;
            _activeEntities = 0;
            _totalComponents = 0;

            var worlds = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IEntityWorld>()
                .ToList();

            if (worlds.Count == 0)
                return;

            _cachedWorld = worlds[0];
            var allEntities = _cachedWorld.GetAllEntities().ToList();

            foreach (var entity in allEntities)
            {
                if (entity == null)
                    continue;

                _totalEntities++;
                if (entity.IsActive)
                    _activeEntities++;
                _totalComponents += entity.ComponentCount;

                var info = new EntityInfo
                {
                    Id = entity.Id,
                    Entity = entity,
                    IsValid = entity.Id.IsValid,
                    ComponentCount = entity.ComponentCount,
                    IsActive = entity.IsActive,
                };

                // 收集组件类型
                foreach (var comp in entity.GetAllComponents())
                {
                    if (comp != null)
                        info.ComponentTypes.Add(comp.GetType());
                }

                _entityInfos.Add(info);
            }

            // 应用筛选
            FilterEntities();
        }

        private List<EntityInfo> _filteredEntities = new();

        private void FilterEntities()
        {
            _filteredEntities = _entityInfos
                .Where(e =>
                {
                    // 激活状态筛选
                    if (_showActiveOnly && !e.IsActive)
                        return false;

                    // 搜索文本筛选
                    if (!string.IsNullOrEmpty(_searchFilter))
                    {
                        var searchLower = _searchFilter.ToLower();
                        bool matchesId = e.Id.ToString().ToLower().Contains(searchLower);
                        bool matchesComponent = e.ComponentTypes.Any(t =>
                            t.Name.ToLower().Contains(searchLower)
                        );
                        if (!matchesId && !matchesComponent)
                            return false;
                    }

                    // 组件类型筛选
                    if (_selectedComponentType != null)
                    {
                        if (!e.ComponentTypes.Contains(_selectedComponentType))
                            return false;
                    }

                    return true;
                })
                .ToList();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawStatsBar();

            EditorGUILayout.BeginHorizontal();
            DrawEntityList();
            DrawEntityDetails();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 刷新按钮
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshData();
            }

            // 自动刷新开关
            _autoRefresh = GUILayout.Toggle(
                _autoRefresh,
                "Auto",
                EditorStyles.toolbarButton,
                GUILayout.Width(50)
            );

            GUILayout.Space(10);

            // 搜索框
            GUILayout.Label("Search:", GUILayout.Width(45));
            var newFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(150)
            );
            if (newFilter != _searchFilter)
            {
                _searchFilter = newFilter;
                FilterEntities();
            }

            GUILayout.Space(10);

            // 激活状态筛选
            _showActiveOnly = GUILayout.Toggle(
                _showActiveOnly,
                "Active Only",
                EditorStyles.toolbarButton,
                GUILayout.Width(80)
            );
            if (GUI.changed)
                FilterEntities();

            GUILayout.FlexibleSpace();

            // 组件类型筛选
            GUILayout.Label("Component:", GUILayout.Width(65));
            var componentNames = new List<string> { "All" };
            componentNames.AddRange(_availableComponentTypes.Select(t => t.Name));
            int selectedIndex =
                _selectedComponentType == null
                    ? 0
                    : _availableComponentTypes.IndexOf(_selectedComponentType) + 1;

            int newIndex = EditorGUILayout.Popup(
                selectedIndex,
                componentNames.ToArray(),
                EditorStyles.toolbarPopup,
                GUILayout.Width(120)
            );

            if (newIndex != selectedIndex)
            {
                _selectedComponentType =
                    newIndex == 0 ? null : _availableComponentTypes[newIndex - 1];
                FilterEntities();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatsBar()
        {
            if (!_showStats)
                return;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"Total: {_totalEntities}", GUILayout.Width(70));
            GUILayout.Label($"Active: {_activeEntities}", GUILayout.Width(70));
            GUILayout.Label($"Filtered: {_filteredEntities.Count}", GUILayout.Width(80));
            GUILayout.Label($"Components: {_totalComponents}", GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            _showStats = GUILayout.Toggle(_showStats, "▼", GUILayout.Width(25));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntityList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));

            GUILayout.Label("Entities", EditorStyles.boldLabel);

            _entityScrollPos = EditorGUILayout.BeginScrollView(_entityScrollPos, "box");

            for (int i = 0; i < _filteredEntities.Count; i++)
            {
                DrawEntityItem(_filteredEntities[i], i);
            }

            if (_filteredEntities.Count == 0)
            {
                EditorGUILayout.LabelField("No entities found", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEntityItem(EntityInfo info, int index)
        {
            bool isSelected = _selectedEntity == info;

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            if (isSelected)
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            else if (!info.IsActive)
                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);

            EditorGUILayout.BeginHorizontal(style);

            // 实体ID和状态
            var label = $"<b>{info.Id}</b>";
            if (!info.IsActive)
                label += " <color=gray>[Inactive]</color>";
            label += $"\n<size=10>{info.ComponentCount} components</size>";

            if (GUILayout.Button(label, new GUIStyle(EditorStyles.label) { richText = true }))
            {
                _selectedEntity = info;
            }

            // 快捷操作
            if (info.Entity != null)
            {
                bool newActive = GUILayout.Toggle(info.IsActive, "On", GUILayout.Width(35));
                if (newActive != info.IsActive)
                {
                    info.Entity.IsActive = newActive;
                    info.IsActive = newActive;
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        private void DrawEntityDetails()
        {
            EditorGUILayout.BeginVertical();

            GUILayout.Label("Entity Details", EditorStyles.boldLabel);

            if (_selectedEntity == null || _selectedEntity.Entity == null)
            {
                EditorGUILayout.HelpBox("Select an entity to view details", MessageType.Info);
            }
            else
            {
                _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);

                DrawEntityHeader();
                DrawComponentList();

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEntityHeader()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Entity ID", _selectedEntity.Id.ToString());
            EditorGUILayout.LabelField("Handle", _selectedEntity.Id.Handle.ToString());
            EditorGUILayout.LabelField("Generation", _selectedEntity.Id.Generation.ToString());

            EditorGUILayout.Space();

            bool newActive = EditorGUILayout.ToggleLeft(
                "Is Active",
                _selectedEntity.Entity.IsActive
            );
            if (newActive != _selectedEntity.Entity.IsActive)
            {
                _selectedEntity.Entity.IsActive = newActive;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentList()
        {
            GUILayout.Space(10);
            GUILayout.Label("Components", EditorStyles.boldLabel);

            _showComponents = EditorGUILayout.Foldout(
                _showComponents,
                $"Show Components ({_selectedEntity.ComponentCount})"
            );

            if (_showComponents)
            {
                EditorGUI.indentLevel++;

                foreach (var comp in _selectedEntity.Entity.GetAllComponents())
                {
                    if (comp == null)
                        continue;

                    DrawComponentItem(comp);
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawComponentItem(IEntityComponent component)
        {
            var type = component.GetType();
            var isExpanded = EditorGUILayout.Foldout(false, type.Name);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                // 显示组件字段
                var fields = type.GetFields(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                );

                foreach (var field in fields)
                {
                    var value = field.GetValue(component);
                    EditorGUILayout.LabelField(field.Name, value?.ToString() ?? "null");
                }

                // 显示属性
                var properties = type.GetProperties(
                        System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.Instance
                    )
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(component);
                        EditorGUILayout.LabelField(prop.Name, value?.ToString() ?? "null");
                    }
                    catch { }
                }

                EditorGUILayout.Space();

                // 移除组件按钮
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Remove Component", GUILayout.Height(20)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", $"Remove {type.Name}?", "Yes", "No"))
                    {
                        _selectedEntity.Entity.RemoveComponent(type);
                        RefreshData();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUI.indentLevel--;
            }
        }
    }
}
