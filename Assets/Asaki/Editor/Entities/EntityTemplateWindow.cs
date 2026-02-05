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
    /// 实体模板编辑器 - 创建和管理实体模板
    /// </summary>
    public class EntityTemplateWindow : EditorWindow
    {
        [MenuItem("Asaki/Entities/Entity Templates", false, 20)]
        public static void ShowWindow()
        {
            GetWindow<EntityTemplateWindow>("Entity Templates");
        }

        // 模板数据
        private List<TemplateData> _templates = new();
        private TemplateData _selectedTemplate;

        // UI状态
        private Vector2 _templateListScroll;
        private Vector2 _templateDetailScroll;
        private string _newTemplateName = "";
        private string _searchFilter = "";

        // 组件选择
        private List<Type> _availableComponentTypes = new();
        private Vector2 _componentPickerScroll;

        // 编辑状态
        private bool _isEditing = false;
        private TemplateData _editingTemplate;
        private List<ComponentConfig> _editingComponents = new();
        private string _editingName = "";

        private class TemplateData
        {
            public string Name;
            public EntityTemplate Template;
            public List<Type> ComponentTypes = new();
            public DateTime LastModified;
        }

        private class ComponentConfig
        {
            public Type ComponentType;
            public bool IsConfigured;
            public Dictionary<FieldInfo, object> FieldValues = new();
        }

        private void OnEnable()
        {
            RefreshAvailableComponents();
            LoadTemplates();
        }

        private void RefreshAvailableComponents()
        {
            _availableComponentTypes = TypeCache
                .GetTypesDerivedFrom<IEntityComponent>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToList();
        }

        private void LoadTemplates()
        {
            _templates.Clear();

            var names = EntityTemplateRegistry.GetTemplateNames();
            foreach (var name in names)
            {
                var template = EntityTemplateRegistry.Get(name);
                if (template != null)
                {
                    _templates.Add(
                        new TemplateData
                        {
                            Name = name,
                            Template = template,
                            LastModified = DateTime.Now,
                        }
                    );
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawTemplateList();
            DrawTemplateDetails();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadTemplates();
            }

            GUILayout.Space(10);

            GUILayout.Label("Search:", GUILayout.Width(45));
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(150)
            );

            GUILayout.FlexibleSpace();

            GUILayout.Label($"Templates: {_templates.Count}", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTemplateList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            // 新建模板区域
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("New Template", EditorStyles.miniBoldLabel);
            _newTemplateName = EditorGUILayout.TextField(_newTemplateName);

            GUI.enabled =
                !string.IsNullOrWhiteSpace(_newTemplateName)
                && !_templates.Any(t => t.Name == _newTemplateName);

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Create Template"))
            {
                CreateNewTemplate(_newTemplateName);
                _newTemplateName = "";
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("Templates", EditorStyles.boldLabel);

            _templateListScroll = EditorGUILayout.BeginScrollView(_templateListScroll, "box");

            var filtered = _templates
                .Where(t =>
                    string.IsNullOrEmpty(_searchFilter)
                    || t.Name.ToLower().Contains(_searchFilter.ToLower())
                )
                .ToList();

            foreach (var template in filtered)
            {
                DrawTemplateItem(template);
            }

            if (filtered.Count == 0)
            {
                EditorGUILayout.LabelField("No templates", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTemplateItem(TemplateData data)
        {
            bool isSelected = _selectedTemplate == data;
            bool isEditing = _editingTemplate == data && _isEditing;

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            if (isSelected || isEditing)
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);

            EditorGUILayout.BeginHorizontal(style);

            var label =
                $"<b>{data.Name}</b>\n<size=10>{data.ComponentTypes.Count} components</size>";

            if (GUILayout.Button(label, new GUIStyle(EditorStyles.label) { richText = true }))
            {
                if (_isEditing)
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Discard Changes?",
                            "You have unsaved changes. Discard them?",
                            "Yes",
                            "No"
                        )
                    )
                    {
                        _isEditing = false;
                        _editingTemplate = null;
                        _selectedTemplate = data;
                    }
                }
                else
                {
                    _selectedTemplate = data;
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        private void DrawTemplateDetails()
        {
            EditorGUILayout.BeginVertical();

            if (_isEditing && _editingTemplate != null)
            {
                DrawEditPanel();
            }
            else if (_selectedTemplate == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a template to view details or create a new one",
                    MessageType.Info
                );
            }
            else
            {
                DrawTemplateView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTemplateView()
        {
            _templateDetailScroll = EditorGUILayout.BeginScrollView(_templateDetailScroll);

            EditorGUILayout.BeginVertical("box");

            // 标题
            GUILayout.Label(_selectedTemplate.Name, EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // 组件列表
            GUILayout.Label("Components:", EditorStyles.miniBoldLabel);

            foreach (var type in _selectedTemplate.ComponentTypes)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(type.Name);
                GUILayout.FlexibleSpace();

                if (type.IsSubclassOf(typeof(TagComponent)))
                {
                    GUILayout.Label("[Tag]", EditorStyles.miniLabel, GUILayout.Width(40));
                }

                EditorGUILayout.EndHorizontal();
            }

            if (_selectedTemplate.ComponentTypes.Count == 0)
            {
                EditorGUILayout.LabelField("No components configured", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("Edit Template", GUILayout.Height(30)))
            {
                StartEditing(_selectedTemplate);
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete", GUILayout.Height(30), GUILayout.Width(80)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Delete Template",
                        $"Delete template '{_selectedTemplate.Name}'?",
                        "Yes",
                        "No"
                    )
                )
                {
                    DeleteTemplate(_selectedTemplate);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 测试创建按钮
            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("Test Create Entity", GUILayout.Height(35)))
                {
                    TestCreateEntity(_selectedTemplate);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to test create entities",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEditPanel()
        {
            _templateDetailScroll = EditorGUILayout.BeginScrollView(_templateDetailScroll);

            EditorGUILayout.BeginVertical("box");

            GUILayout.Label("Edit Template", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // 名称编辑
            _editingName = EditorGUILayout.TextField("Name", _editingName);

            EditorGUILayout.Space();

            // 组件选择
            GUILayout.Label("Add Components:", EditorStyles.miniBoldLabel);

            _componentPickerScroll = EditorGUILayout.BeginScrollView(
                _componentPickerScroll,
                GUILayout.Height(150)
            );

            foreach (var type in _availableComponentTypes)
            {
                bool alreadyAdded = _editingComponents.Any(c => c.ComponentType == type);
                if (alreadyAdded)
                    continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(type.Name);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    AddComponentToEdit(type);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 已添加的组件
            GUILayout.Label("Configured Components:", EditorStyles.miniBoldLabel);

            foreach (var config in _editingComponents.ToList())
            {
                DrawComponentConfig(config);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            // 保存/取消按钮
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Save Changes", GUILayout.Height(35)))
            {
                SaveEditingTemplate();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Cancel", GUILayout.Height(35), GUILayout.Width(80)))
            {
                _isEditing = false;
                _editingTemplate = null;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawComponentConfig(ComponentConfig config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                $"<b>{config.ComponentType.Name}</b>",
                new GUIStyle(EditorStyles.label) { richText = true }
            );
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                _editingComponents.Remove(config);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            // 显示可配置字段
            var fields = config.ComponentType.GetFields(
                BindingFlags.Public | BindingFlags.Instance
            );
            foreach (var field in fields)
            {
                DrawFieldEditor(config, field);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private void DrawFieldEditor(ComponentConfig config, FieldInfo field)
        {
            var value = config.FieldValues.ContainsKey(field)
                ? config.FieldValues[field]
                : GetDefaultValue(field.FieldType);

            object newValue = null;

            EditorGUILayout.BeginHorizontal();

            if (field.FieldType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(field.Name, (int)value);
            }
            else if (field.FieldType == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(field.Name, (float)value);
            }
            else if (field.FieldType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(field.Name, (bool)value);
            }
            else if (field.FieldType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(field.Name, (string)value);
            }
            else if (field.FieldType == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(field.Name, (Vector2)value);
            }
            else if (field.FieldType == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(field.Name, (Vector3)value);
            }
            else if (field.FieldType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(field.Name, (Enum)value);
            }
            else
            {
                EditorGUILayout.LabelField(field.Name, value?.ToString() ?? "null");
            }

            if (newValue != null && !Equals(newValue, value))
            {
                config.FieldValues[field] = newValue;
            }

            EditorGUILayout.EndHorizontal();
        }

        private object GetDefaultValue(Type type)
        {
            if (type == typeof(int))
                return 0;
            if (type == typeof(float))
                return 0f;
            if (type == typeof(bool))
                return false;
            if (type == typeof(string))
                return "";
            if (type == typeof(Vector2))
                return Vector2.zero;
            if (type == typeof(Vector3))
                return Vector3.zero;
            if (type.IsEnum)
                return Enum.GetValues(type).GetValue(0);
            return null;
        }

        private void CreateNewTemplate(string name)
        {
            var template = new EntityTemplate();
            EntityTemplateRegistry.Register(name, template);

            var data = new TemplateData
            {
                Name = name,
                Template = template,
                LastModified = DateTime.Now,
            };

            _templates.Add(data);
            _selectedTemplate = data;
        }

        private void StartEditing(TemplateData data)
        {
            _isEditing = true;
            _editingTemplate = data;
            _editingName = data.Name;
            _editingComponents = new List<ComponentConfig>();

            // 复制当前组件配置
            foreach (var type in data.ComponentTypes)
            {
                _editingComponents.Add(
                    new ComponentConfig { ComponentType = type, IsConfigured = true }
                );
            }
        }

        private void AddComponentToEdit(Type type)
        {
            _editingComponents.Add(
                new ComponentConfig { ComponentType = type, IsConfigured = true }
            );
        }

        private void SaveEditingTemplate()
        {
            // 创建新模板
            var newTemplate = new EntityTemplate();

            foreach (var config in _editingComponents)
            {
                var type = config.ComponentType;

                // 构建配置lambda
                if (type.IsSubclassOf(typeof(TagComponent)))
                {
                    // 标签组件使用WithTag
                    typeof(EntityTemplate)
                        .GetMethod("WithTag")
                        .MakeGenericMethod(type)
                        .Invoke(newTemplate, null);
                }
                else
                {
                    // 普通组件使用With并配置字段
                    var method = typeof(EntityTemplate)
                        .GetMethods()
                        .First(m => m.Name == "With" && m.GetParameters().Length == 1)
                        .MakeGenericMethod(type);

                    // 创建配置委托
                    Action<object> configureAction = (comp) =>
                    {
                        foreach (var kvp in config.FieldValues)
                        {
                            kvp.Key.SetValue(comp, kvp.Value);
                        }
                    };

                    method.Invoke(newTemplate, new object[] { configureAction });
                }
            }

            // 注册新模板
            EntityTemplateRegistry.Unregister(_editingTemplate.Name);
            EntityTemplateRegistry.Register(_editingName, newTemplate);

            // 更新数据
            _editingTemplate.Name = _editingName;
            _editingTemplate.Template = newTemplate;
            _editingTemplate.LastModified = DateTime.Now;
            _editingTemplate.ComponentTypes = _editingComponents
                .Select(c => c.ComponentType)
                .ToList();

            _isEditing = false;
            _editingTemplate = null;
        }

        private void DeleteTemplate(TemplateData data)
        {
            EntityTemplateRegistry.Unregister(data.Name);
            _templates.Remove(data);
            _selectedTemplate = null;
        }

        private void TestCreateEntity(TemplateData data)
        {
            var worlds = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IEntityWorld>()
                .ToList();

            if (worlds.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No EntityWorld found!", "OK");
                return;
            }

            var entity = EntityTemplateRegistry.Instantiate(data.Name, worlds[0]);
            if (entity != null)
            {
                Debug.Log(
                    $"[EntityTemplate] Created entity {entity.Id} from template '{data.Name}'"
                );
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Created entity {entity.Id} with {entity.ComponentCount} components",
                    "OK"
                );
            }
        }
    }
}
