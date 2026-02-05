using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Architecture.Entities;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Entities
{
    /// <summary>
    /// 实体查询窗口 - 高级查询和批量操作
    /// </summary>
    public class EntityQueryWindow : EditorWindow
    {
        [MenuItem("Asaki/Entities/Query Builder", false, 40)]
        public static void ShowWindow()
        {
            GetWindow<EntityQueryWindow>("Query Builder");
        }

        // 世界引用
        private IEntityWorld _world;

        // 查询条件
        private List<QueryCondition> _conditions = new();
        private List<Type> _availableComponentTypes = new();

        // 查询结果
        private List<IEntity> _queryResults = new();

        // UI状态
        private Vector2 _conditionsScroll;
        private Vector2 _resultsScroll;
        private bool _showResults = true;

        // 批量操作
        private bool _showBatchActions = false;
        private Type _componentToAdd;
        private Type _componentToRemove;

        private enum ConditionType
        {
            HasComponent,
            NotHasComponent,
            IsActive,
            IsInactive,
            HasTag,
            Custom,
        }

        private class QueryCondition
        {
            public ConditionType Type;
            public Type ComponentType;
            public string Tag;
            public bool IsEnabled = true;
        }

        private void OnEnable()
        {
            RefreshComponentTypes();
        }

        private void RefreshComponentTypes()
        {
            _availableComponentTypes = TypeCache
                .GetTypesDerivedFrom<IEntityComponent>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToList();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawQueryBuilder();
            DrawQueryResults();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh World", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                RefreshWorld();
            }

            GUILayout.FlexibleSpace();

            if (_world != null)
            {
                var entityCount = _world.GetAllEntities().Count();
                GUILayout.Label($"World Entities: {entityCount}", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("No World Found", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshWorld()
        {
            var worlds = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IEntityWorld>()
                .ToList();

            if (worlds.Count > 0)
            {
                _world = worlds[0];
                ExecuteQuery();
            }
        }

        private void DrawQueryBuilder()
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.Label("Query Conditions", EditorStyles.boldLabel);

            _conditionsScroll = EditorGUILayout.BeginScrollView(
                _conditionsScroll,
                GUILayout.Height(150)
            );

            for (int i = 0; i < _conditions.Count; i++)
            {
                DrawConditionItem(i, _conditions[i]);
            }

            if (_conditions.Count == 0)
            {
                EditorGUILayout.HelpBox("Add conditions to build a query", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 添加条件按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Has Component"))
            {
                _conditions.Add(new QueryCondition { Type = ConditionType.HasComponent });
            }

            if (GUILayout.Button("+ Not Has Component"))
            {
                _conditions.Add(new QueryCondition { Type = ConditionType.NotHasComponent });
            }

            if (GUILayout.Button("+ Tag"))
            {
                _conditions.Add(new QueryCondition { Type = ConditionType.HasTag });
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Is Active"))
            {
                _conditions.Add(new QueryCondition { Type = ConditionType.IsActive });
            }

            if (GUILayout.Button("+ Is Inactive"))
            {
                _conditions.Add(new QueryCondition { Type = ConditionType.IsInactive });
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 执行查询按钮
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("Execute Query", GUILayout.Height(30)))
            {
                ExecuteQuery();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawConditionItem(int index, QueryCondition condition)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            condition.IsEnabled = EditorGUILayout.Toggle(condition.IsEnabled, GUILayout.Width(20));

            GUI.enabled = condition.IsEnabled;

            // 条件类型
            condition.Type = (ConditionType)
                EditorGUILayout.EnumPopup(condition.Type, GUILayout.Width(120));

            // 根据类型显示额外选项
            switch (condition.Type)
            {
                case ConditionType.HasComponent:
                case ConditionType.NotHasComponent:
                    DrawComponentSelector(condition);
                    break;

                case ConditionType.HasTag:
                    condition.Tag = EditorGUILayout.TextField(condition.Tag);
                    break;
            }

            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            // 删除按钮
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                _conditions.RemoveAt(index);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawComponentSelector(QueryCondition condition)
        {
            var componentNames = _availableComponentTypes.Select(t => t.Name).ToArray();
            int currentIndex =
                condition.ComponentType != null
                    ? _availableComponentTypes.IndexOf(condition.ComponentType)
                    : -1;

            int newIndex = EditorGUILayout.Popup(Mathf.Max(0, currentIndex), componentNames);
            if (newIndex >= 0 && newIndex < _availableComponentTypes.Count)
            {
                condition.ComponentType = _availableComponentTypes[newIndex];
            }
        }

        private void ExecuteQuery()
        {
            if (_world == null)
            {
                RefreshWorld();
                if (_world == null)
                    return;
            }

            _queryResults = _world
                .GetAllEntities()
                .Where(e =>
                {
                    foreach (var condition in _conditions)
                    {
                        if (!condition.IsEnabled)
                            continue;

                        bool passes = condition.Type switch
                        {
                            ConditionType.HasComponent => condition.ComponentType != null
                                && e.HasComponent(condition.ComponentType),
                            ConditionType.NotHasComponent => condition.ComponentType == null
                                || !e.HasComponent(condition.ComponentType),
                            ConditionType.IsActive => e.IsActive,
                            ConditionType.IsInactive => !e.IsActive,
                            ConditionType.HasTag => !string.IsNullOrEmpty(condition.Tag)
                                && e.TryGetComponent<TagsComponent>(out var tags)
                                && tags.HasTag(condition.Tag),
                            _ => true,
                        };

                        if (!passes)
                            return false;
                    }
                    return true;
                })
                .ToList();
        }

        private void DrawQueryResults()
        {
            _showResults = EditorGUILayout.BeginFoldoutHeaderGroup(
                _showResults,
                $"Query Results ({_queryResults.Count})"
            );

            if (_showResults)
            {
                _resultsScroll = EditorGUILayout.BeginScrollView(
                    _resultsScroll,
                    GUILayout.MinHeight(200)
                );

                foreach (var entity in _queryResults)
                {
                    DrawResultItem(entity);
                }

                if (_queryResults.Count == 0)
                {
                    EditorGUILayout.LabelField(
                        "No matching entities",
                        EditorStyles.centeredGreyMiniLabel
                    );
                }

                EditorGUILayout.EndScrollView();

                // 批量操作
                DrawBatchActions();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawResultItem(IEntity entity)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label($"{entity.Id}", GUILayout.Width(120));
            GUILayout.Label($"Components: {entity.ComponentCount}", GUILayout.Width(100));
            GUILayout.Label(entity.IsActive ? "Active" : "Inactive", GUILayout.Width(60));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("View", GUILayout.Width(50)))
            {
                EntityDebuggerWindow.ShowWindow();
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Destroy", GUILayout.Width(60)))
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
                    ExecuteQuery();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBatchActions()
        {
            GUILayout.Space(10);

            _showBatchActions = EditorGUILayout.Foldout(_showBatchActions, "Batch Actions");

            if (_showBatchActions)
            {
                EditorGUILayout.BeginVertical("box");

                // 批量添加组件
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Add Component:", GUILayout.Width(100));

                var componentNames = new[] { "Select..." }
                    .Concat(_availableComponentTypes.Select(t => t.Name))
                    .ToArray();
                int selectedIndex =
                    _componentToAdd != null
                        ? _availableComponentTypes.IndexOf(_componentToAdd) + 1
                        : 0;

                int newIndex = EditorGUILayout.Popup(selectedIndex, componentNames);
                if (newIndex > 0)
                {
                    _componentToAdd = _availableComponentTypes[newIndex - 1];
                }

                GUI.enabled = _componentToAdd != null && _queryResults.Count > 0;
                if (GUILayout.Button("Add to All", GUILayout.Width(100)))
                {
                    BatchAddComponent(_componentToAdd);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                // 批量移除组件
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Remove Component:", GUILayout.Width(100));

                selectedIndex =
                    _componentToRemove != null
                        ? _availableComponentTypes.IndexOf(_componentToRemove) + 1
                        : 0;

                newIndex = EditorGUILayout.Popup(selectedIndex, componentNames);
                if (newIndex > 0)
                {
                    _componentToRemove = _availableComponentTypes[newIndex - 1];
                }

                GUI.enabled = _componentToRemove != null && _queryResults.Count > 0;
                if (GUILayout.Button("Remove from All", GUILayout.Width(100)))
                {
                    BatchRemoveComponent(_componentToRemove);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                // 批量激活/禁用
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("Activate All") && _queryResults.Count > 0)
                {
                    BatchSetActive(true);
                }
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
                if (GUILayout.Button("Deactivate All") && _queryResults.Count > 0)
                {
                    BatchSetActive(false);
                }
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Destroy All") && _queryResults.Count > 0)
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Destroy All",
                            $"Destroy all {_queryResults.Count} entities?",
                            "Yes",
                            "No"
                        )
                    )
                    {
                        BatchDestroy();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }

        private void BatchAddComponent(Type componentType)
        {
            int count = 0;
            foreach (var entity in _queryResults)
            {
                if (!entity.HasComponent(componentType))
                {
                    var method = typeof(IEntity)
                        .GetMethod("AddComponent")
                        .MakeGenericMethod(componentType);
                    method.Invoke(entity, null);
                    count++;
                }
            }
            Debug.Log($"[EntityQuery] Added {componentType.Name} to {count} entities");
            ExecuteQuery();
        }

        private void BatchRemoveComponent(Type componentType)
        {
            int count = 0;
            foreach (var entity in _queryResults)
            {
                if (entity.HasComponent(componentType))
                {
                    var method = typeof(IEntity)
                        .GetMethod("RemoveComponent")
                        .MakeGenericMethod(componentType);
                    method.Invoke(entity, null);
                    count++;
                }
            }
            Debug.Log($"[EntityQuery] Removed {componentType.Name} from {count} entities");
            ExecuteQuery();
        }

        private void BatchSetActive(bool active)
        {
            int count = 0;
            foreach (var entity in _queryResults)
            {
                if (entity.IsActive != active)
                {
                    entity.IsActive = active;
                    count++;
                }
            }
            Debug.Log($"[EntityQuery] Set {count} entities to {(active ? "active" : "inactive")}");
        }

        private void BatchDestroy()
        {
            int count = 0;
            foreach (var entity in _queryResults.ToList())
            {
                _world.DestroyEntity(entity.Id);
                count++;
            }
            Debug.Log($"[EntityQuery] Destroyed {count} entities");
            _queryResults.Clear();
        }
    }
}
