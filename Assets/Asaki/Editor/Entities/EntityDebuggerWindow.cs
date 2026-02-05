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
    /// 实体调试器 - 实时查看和编辑实体数据
    /// </summary>
    public class EntityDebuggerWindow : EditorWindow
    {
        [MenuItem("Asaki/Entities/Entity Debugger", false, 11)]
        public static void ShowWindow()
        {
            GetWindow<EntityDebuggerWindow>("Entity Debugger");
        }

        // 运行时数据
        private IEntityWorld _world;
        private List<IEntity> _entities = new();
        private IEntity _selectedEntity;
        private IEntityComponent _selectedComponent;

        // UI状态
        private Vector2 _entityScrollPos;
        private Vector2 _inspectorScrollPos;
        private string _searchFilter = "";
        private bool _pauseAutoRefresh = false;
        private double _lastUpdateTime;
        private const double RefreshInterval = 0.1f;

        // 组件编辑
        private Dictionary<FieldInfo, object> _fieldEdits = new();
        private Dictionary<PropertyInfo, object> _propertyEdits = new();

        // 创建实体
        private bool _showCreatePanel = false;
        private Vector2 _createScrollPos;
        private List<ComponentEntry> _newComponents = new();

        private class ComponentEntry
        {
            public bool IsSelected;
            public Type ComponentType;
        }

        private void OnEnable()
        {
            RefreshAvailableComponents();
        }

        private List<Type> _availableComponentTypes = new();

        private void RefreshAvailableComponents()
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
            if (_pauseAutoRefresh)
                return;

            if (EditorApplication.timeSinceStartup - _lastUpdateTime > RefreshInterval)
            {
                RefreshEntities();
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void RefreshEntities()
        {
            if (_world == null)
            {
                var worlds = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .OfType<IEntityWorld>()
                    .ToList();
                if (worlds.Count > 0)
                    _world = worlds[0];
            }

            if (_world != null)
            {
                _entities = _world.GetAllEntities().ToList();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawEntityList();
            DrawInspector();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshEntities();
            }

            _pauseAutoRefresh = GUILayout.Toggle(
                _pauseAutoRefresh,
                "Pause",
                EditorStyles.toolbarButton,
                GUILayout.Width(50)
            );

            GUILayout.Space(10);

            GUILayout.Label("Search:", GUILayout.Width(45));
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(150)
            );

            GUILayout.FlexibleSpace();

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (
                GUILayout.Button(
                    "+ Create Entity",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(100)
                )
            )
            {
                _showCreatePanel = !_showCreatePanel;
            }
            GUI.backgroundColor = originalColor;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntityList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(280));

            GUILayout.Label($"Entities ({_entities.Count})", EditorStyles.boldLabel);

            _entityScrollPos = EditorGUILayout.BeginScrollView(_entityScrollPos, "box");

            var filtered = _entities
                .Where(e =>
                {
                    if (string.IsNullOrEmpty(_searchFilter))
                        return true;
                    return e.Id.ToString().ToLower().Contains(_searchFilter.ToLower())
                        || e.GetAllComponents()
                            .Any(c => c.GetType().Name.ToLower().Contains(_searchFilter.ToLower()));
                })
                .ToList();

            foreach (var entity in filtered)
            {
                DrawEntityItem(entity);
            }

            if (filtered.Count == 0)
            {
                EditorGUILayout.LabelField("No entities", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEntityItem(IEntity entity)
        {
            bool isSelected = _selectedEntity == entity;

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            if (isSelected)
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            else if (!entity.IsActive)
                GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f);

            EditorGUILayout.BeginHorizontal(style);

            var content = $"<b>{entity.Id}</b>\n<size=10>";
            var components = entity.GetAllComponents().Take(3);
            content += string.Join(", ", components.Select(c => c.GetType().Name));
            if (entity.ComponentCount > 3)
                content += "...";
            content += $"</size>\n<size=9>({entity.ComponentCount} components)</size>";

            if (GUILayout.Button(content, new GUIStyle(EditorStyles.label) { richText = true }))
            {
                _selectedEntity = entity;
                _selectedComponent = null;
            }

            // 快速操作
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginVertical(GUILayout.Width(40));

            if (GUILayout.Toggle(entity.IsActive, "", GUILayout.Height(15)) != entity.IsActive)
            {
                entity.IsActive = !entity.IsActive;
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("×", GUILayout.Height(15)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Destroy Entity",
                        $"Destroy {entity.Id}?",
                        "Yes",
                        "No"
                    )
                )
                {
                    _world.DestroyEntity(entity.Id);
                    if (_selectedEntity == entity)
                    {
                        _selectedEntity = null;
                        _selectedComponent = null;
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInspector()
        {
            EditorGUILayout.BeginVertical();

            if (_showCreatePanel)
            {
                DrawCreatePanel();
            }
            else if (_selectedEntity == null)
            {
                EditorGUILayout.HelpBox("Select an entity to inspect", MessageType.Info);
            }
            else
            {
                DrawEntityInspector();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCreatePanel()
        {
            GUILayout.Label("Create New Entity", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            GUILayout.Label("Select Components to Add:", EditorStyles.miniBoldLabel);

            _createScrollPos = EditorGUILayout.BeginScrollView(
                _createScrollPos,
                GUILayout.Height(300)
            );

            foreach (var type in _availableComponentTypes)
            {
                var entry = _newComponents.FirstOrDefault(e => e.ComponentType == type);
                if (entry == null)
                {
                    entry = new ComponentEntry { ComponentType = type };
                    _newComponents.Add(entry);
                }

                entry.IsSelected = EditorGUILayout.ToggleLeft(type.Name, entry.IsSelected);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All"))
            {
                foreach (var entry in _newComponents)
                    entry.IsSelected = true;
            }

            if (GUILayout.Button("Clear All"))
            {
                foreach (var entry in _newComponents)
                    entry.IsSelected = false;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Create Entity", GUILayout.Height(35)))
            {
                CreateNewEntity();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void CreateNewEntity()
        {
            if (_world == null)
            {
                EditorUtility.DisplayDialog("Error", "No EntityWorld found!", "OK");
                return;
            }

            var entity = _world.CreateEntity();

            foreach (var entry in _newComponents.Where(e => e.IsSelected))
            {
                var method = typeof(IEntity)
                    .GetMethod("AddComponent")
                    .MakeGenericMethod(entry.ComponentType);
                method.Invoke(entity, null);
            }

            _selectedEntity = entity;
            _showCreatePanel = false;

            // 清除选择
            foreach (var entry in _newComponents)
                entry.IsSelected = false;

            RefreshEntities();
        }

        private void DrawEntityInspector()
        {
            _inspectorScrollPos = EditorGUILayout.BeginScrollView(_inspectorScrollPos);

            // 实体头部信息
            EditorGUILayout.BeginVertical("box");

            GUILayout.Label($"Entity {_selectedEntity.Id}", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "Handle",
                _selectedEntity.Id.Handle.ToString(),
                GUILayout.Width(150)
            );
            EditorGUILayout.LabelField("Generation", _selectedEntity.Id.Generation.ToString());
            EditorGUILayout.EndHorizontal();

            bool newActive = EditorGUILayout.ToggleLeft("Is Active", _selectedEntity.IsActive);
            if (newActive != _selectedEntity.IsActive)
            {
                _selectedEntity.IsActive = newActive;
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 组件列表
            GUILayout.Label(
                $"Components ({_selectedEntity.ComponentCount})",
                EditorStyles.boldLabel
            );

            foreach (var component in _selectedEntity.GetAllComponents())
            {
                DrawComponentInspector(component);
            }

            // 添加组件按钮
            GUILayout.Space(10);
            DrawAddComponentDropdown();

            EditorGUILayout.EndScrollView();
        }

        private void DrawComponentInspector(IEntityComponent component)
        {
            var type = component.GetType();
            bool isExpanded = _selectedComponent == component;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            GUIStyle headerStyle = new GUIStyle(EditorStyles.foldoutHeader);
            if (isExpanded)
                GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);

            bool newExpanded = EditorGUILayout.Foldout(isExpanded, type.Name, true, headerStyle);
            if (newExpanded != isExpanded)
            {
                _selectedComponent = newExpanded ? component : null;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            // 移除组件按钮
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Remove Component",
                        $"Remove {type.Name}?",
                        "Yes",
                        "No"
                    )
                )
                {
                    var method = typeof(IEntity)
                        .GetMethod("RemoveComponent")
                        .MakeGenericMethod(type);
                    method.Invoke(_selectedEntity, null);
                    if (_selectedComponent == component)
                        _selectedComponent = null;
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (isExpanded)
            {
                DrawComponentEditableFields(component);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentEditableFields(IEntityComponent component)
        {
            EditorGUI.indentLevel++;

            var type = component.GetType();

            // 字段
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                DrawEditableField(component, field);
            }

            // 属性 (只读)
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && !p.CanWrite);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(component);
                    EditorGUILayout.LabelField(prop.Name, value?.ToString() ?? "null");
                }
                catch { }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawEditableField(IEntityComponent component, FieldInfo field)
        {
            var value = field.GetValue(component);
            var fieldType = field.FieldType;

            EditorGUILayout.BeginHorizontal();

            object newValue = null;
            bool valueChanged = false;

            if (fieldType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(field.Name, (int)value);
                valueChanged = !Equals(newValue, value);
            }
            else if (fieldType == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(field.Name, (float)value);
                valueChanged = !Equals(newValue, value);
            }
            else if (fieldType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(field.Name, (bool)value);
                valueChanged = !Equals(newValue, value);
            }
            else if (fieldType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(field.Name, (string)value);
                valueChanged = !Equals(newValue, value);
            }
            else if (fieldType == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(field.Name, (Vector2)value);
                valueChanged = !Equals(newValue, value);
            }
            else if (fieldType == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(field.Name, (Vector3)value);
                valueChanged = !Equals(newValue, value);
            }
            else if (fieldType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(field.Name, (Enum)value);
                valueChanged = !Equals(newValue, value);
            }
            else
            {
                EditorGUILayout.LabelField(field.Name, value?.ToString() ?? "null");
            }

            if (valueChanged && newValue != null)
            {
                field.SetValue(component, newValue);
            }

            EditorGUILayout.EndHorizontal();
        }

        private Vector2 _addComponentScroll;
        private bool _showAddComponent = false;

        private void DrawAddComponentDropdown()
        {
            _showAddComponent = EditorGUILayout.Foldout(_showAddComponent, "+ Add Component");

            if (_showAddComponent)
            {
                EditorGUILayout.BeginVertical("box");

                _addComponentScroll = EditorGUILayout.BeginScrollView(
                    _addComponentScroll,
                    GUILayout.Height(150)
                );

                foreach (var type in _availableComponentTypes)
                {
                    if (_selectedEntity.HasComponent(type))
                        continue;

                    if (GUILayout.Button(type.Name, EditorStyles.miniButton))
                    {
                        MethodInfo method = typeof(IEntity)
                            .GetMethod("AddComponent")
                            ?.MakeGenericMethod(type);
                        if (method != null)
                        {
                            method.Invoke(_selectedEntity, null);
                        }
                        _showAddComponent = false;
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }
    }
}
