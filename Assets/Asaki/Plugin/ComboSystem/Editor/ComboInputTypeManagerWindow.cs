using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Asaki.Plugin.ComboSystem.Editor
{
    /// <summary>
    /// 连招输入类型管理器窗口
    /// 用于添加、编辑和管理自定义输入类型
    /// </summary>
    public class ComboInputTypeManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _newTypeId = "";
        private string _newTypeName = "";
        private string _newTypeCategory = "customization";
        private Color _newTypeColor = Color.white;
        private int _newTypePriority = 100;

        private bool _showBuiltIn = true;
        private bool _showCustom = true;
        private string _searchFilter = "";

        [MenuItem("Asaki/ComboSystem/Input type management", priority = 21)]
        public static void ShowWindow()
        {
            var window = GetWindow<ComboInputTypeManagerWindow>("Input type management");
            window.minSize = new Vector2(400, 500);
        }

        void OnEnable()
        {
            // 确保注册表已初始化
            ComboInputTypeRegistry.Initialize();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Combo input type manager", EditorStyles.largeLabel);
            EditorGUILayout.Space(10);

            // 搜索栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField
            );
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 筛选选项
            EditorGUILayout.BeginHorizontal();
            _showBuiltIn = GUILayout.Toggle(
                _showBuiltIn,
                "Show built-in",
                EditorStyles.miniButtonLeft
            );
            _showCustom = GUILayout.Toggle(
                _showCustom,
                "Show custom",
                EditorStyles.miniButtonRight
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 类型列表
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var categories = ComboInputTypeRegistry.GetDefinitionsByCategory();

            foreach (var category in categories.OrderBy(c => c.Key))
            {
                var types = category
                    .Value.Where(t =>
                    {
                        if (ComboInputTypeRegistry.IsBuiltInType(t.Id) && !_showBuiltIn)
                            return false;
                        if (!ComboInputTypeRegistry.IsBuiltInType(t.Id) && !_showCustom)
                            return false;
                        if (!string.IsNullOrEmpty(_searchFilter))
                        {
                            return t.Id.ToLower().Contains(_searchFilter.ToLower())
                                || t.DisplayName.ToLower().Contains(_searchFilter.ToLower());
                        }
                        return true;
                    })
                    .ToList();

                if (types.Count == 0)
                    continue;

                EditorGUILayout.LabelField(category.Key, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                foreach (var type in types)
                {
                    DrawTypeItem(type);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 添加新类型
            EditorGUILayout.LabelField("Add new type", EditorStyles.boldLabel);
            DrawAddNewTypeSection();
        }

        /// <summary>
        /// 绘制类型项
        /// </summary>
        void DrawTypeItem(ComboInputTypeDefinition type)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 颜色指示
            EditorGUILayout.ColorField(
                GUIContent.none,
                type.Color,
                false,
                false,
                false,
                GUILayout.Width(30)
            );

            EditorGUILayout.BeginVertical();

            // ID和名称
            EditorGUILayout.LabelField(type.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"ID: {type.Id} | Priority: {type.Priority}",
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndVertical();

            // 删除按钮（仅自定义类型）
            if (!ComboInputTypeRegistry.IsBuiltInType(type.Id))
            {
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Confirm Delete",
                            $"Are you sure you want to delete the input type '{type.DisplayName}'?\n"
                                + "Nodes using this type will become invalid.",
                            "Delete",
                            "Cancel"
                        )
                    )
                    {
                        ComboInputTypeRegistry.RemoveUserType(type.Id);
                        Repaint();
                    }
                }
            }
            else
            {
                GUILayout.Label("Built-in", EditorStyles.miniLabel, GUILayout.Width(40));
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制添加新类型区域
        /// </summary>
        void DrawAddNewTypeSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _newTypeId = EditorGUILayout.TextField("Type ID", _newTypeId);
            _newTypeName = EditorGUILayout.TextField("Display Name", _newTypeName);
            _newTypeCategory = EditorGUILayout.TextField("Category", _newTypeCategory);
            _newTypeColor = EditorGUILayout.ColorField("Color", _newTypeColor);
            _newTypePriority = EditorGUILayout.IntField("Priority", _newTypePriority);

            EditorGUILayout.Space(5);

            // 验证
            bool isValid =
                !string.IsNullOrEmpty(_newTypeId)
                && !string.IsNullOrEmpty(_newTypeName)
                && !ComboInputTypeRegistry.HasType(_newTypeId);

            EditorGUI.BeginDisabledGroup(!isValid);

            if (GUILayout.Button("Add Type", GUILayout.Height(30)))
            {
                var definition = new ComboInputTypeDefinition
                {
                    Id = _newTypeId,
                    DisplayName = _newTypeName,
                    Category = _newTypeCategory,
                    Color = _newTypeColor,
                    Priority = _newTypePriority,
                };

                ComboInputTypeRegistry.Register(definition);
                ComboInputTypeRegistry.SaveUserDefinedTypes();

                // 清空输入
                _newTypeId = "";
                _newTypeName = "";

                Repaint();
            }

            EditorGUI.EndDisabledGroup();

            if (!isValid)
            {
                if (string.IsNullOrEmpty(_newTypeId))
                {
                    EditorGUILayout.HelpBox("Please enter a Type ID", MessageType.Warning);
                }
                else if (string.IsNullOrEmpty(_newTypeName))
                {
                    EditorGUILayout.HelpBox("Please enter a Display Name", MessageType.Warning);
                }
                else if (ComboInputTypeRegistry.HasType(_newTypeId))
                {
                    EditorGUILayout.HelpBox(
                        $"Type ID '{_newTypeId}' already exists",
                        MessageType.Error
                    );
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 帮助信息
            EditorGUILayout.HelpBox(
                "Type ID is used to identify input types in code. Suggest using English and underscores.\n"
                    + "Lower priority values will be sorted higher in the list.\n"
                    + "Built-in types cannot be deleted but can be overridden by custom types.",
                MessageType.Info
            );
        }
    }
}
