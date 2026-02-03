using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Graphs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.GraphEditors
{
    public class GlobalBlackboardWindow : EditorWindow
    {
        private AsakiGlobalBlackboardAsset _globalAsset;
        private SerializedObject _serializedObject;

        // UI 元素缓存
        private ListView _listView;
        private PopupField<string> _typePopup;
        private TextField _nameField;

        // 类型缓存
        private List<Type> _availableTypes;
        private List<string> _availableTypeNames;

        [MenuItem("Asaki/Window/Global Blackboard", false, 30)]
        public static void ShowWindow()
        {
            GlobalBlackboardWindow window = GetWindow<GlobalBlackboardWindow>("Global Blackboard");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            LoadAsset();
            InitTypes();
        }

        private void LoadAsset()
        {
            string assetPath = "Assets/Resources/Asaki/Configuration/GlobalBlackboard.asset";
            _globalAsset = AssetDatabase.LoadAssetAtPath<AsakiGlobalBlackboardAsset>(assetPath);

            if (_globalAsset == null)
            {
                _globalAsset = CreateInstance<AsakiGlobalBlackboardAsset>();
                System.IO.Directory.CreateDirectory("Assets/Resources/Asaki/Configuration");
                AssetDatabase.CreateAsset(_globalAsset, assetPath);
                AssetDatabase.SaveAssets();
            }

            _serializedObject = new SerializedObject(_globalAsset);
        }

        private void InitTypes()
        {
            // 使用 TypeCache 缓存所有 AsakiValueBase 的实现类
            _availableTypes = TypeCache
                .GetTypesDerivedFrom<AsakiValueBase>()
                .Where(t => !t.IsAbstract)
                .ToList();

            _availableTypeNames = _availableTypes
                .Select(t => t.Name.Replace("Asaki", "")) // 移除前缀美化显示
                .ToList();
        }

        // ★ 核心：UITK 入口，替代 OnGUI
        public void CreateGUI()
        {
            // 1. 根布局
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            if (_globalAsset == null)
                LoadAsset();
            _serializedObject.Update();

            // 2. 顶部标题
            Label titleLabel = new Label("Global Blackboard Variables");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            root.Add(titleLabel);

            // 3. 创建添加栏 (Toolbar)
            VisualElement toolbar = CreateToolbar();
            root.Add(toolbar);

            // 4. 创建列表 (ListView)
            CreateListView(root);
        }

        private VisualElement CreateToolbar()
        {
            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.marginBottom = 10;
            toolbar.style.height = 25;

            // 变量名输入框
            _nameField = new TextField();
            _nameField.label = "Name";
            _nameField.style.flexGrow = 1;
            toolbar.Add(_nameField);

            // 类型下拉框
            if (_availableTypeNames.Count > 0)
            {
                _typePopup = new PopupField<string>(_availableTypeNames, 0);
                _typePopup.style.width = 100;
                _typePopup.style.marginLeft = 5;
                toolbar.Add(_typePopup);
            }

            // 添加按钮
            Button addButton = new Button(OnAddClicked) { text = "Add" };
            addButton.style.width = 60;
            addButton.style.marginLeft = 5;
            toolbar.Add(addButton);

            return toolbar;
        }

        private void CreateListView(VisualElement root)
        {
            // 获取 "GlobalVariables" 属性
            SerializedProperty listProp = _serializedObject.FindProperty("GlobalVariables");

            // 创建 ListView
            _listView = new ListView();
            _listView.style.flexGrow = 1;
            _listView.showBorder = true;
            _listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All; // 斑马纹背景

            // ★ 绑定列表源
            _listView.BindProperty(listProp);

            // ★ 定义每一行的外观 (MakeItem)
            _listView.makeItem = () =>
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center; // 垂直居中
                row.style.paddingTop = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.paddingRight = 5;

                // 名字 (左侧)
                TextField nameField = new TextField();
                nameField.name = "NameField";
                nameField.style.width = 150;
                row.Add(nameField);

                // 值 (中间，自动伸缩)
                VisualElement valueContainer = new VisualElement();
                valueContainer.name = "ValueContainer";
                valueContainer.style.flexGrow = 1;
                valueContainer.style.marginLeft = 10;
                valueContainer.style.marginRight = 10;
                row.Add(valueContainer);

                // 删除按钮 (右侧)
                Button deleteBtn = new Button();
                deleteBtn.name = "DeleteBtn";
                deleteBtn.text = "X";
                deleteBtn.style.width = 25;
                deleteBtn.style.backgroundColor = new StyleColor(new Color(0.8f, 0.3f, 0.3f)); // 红色警告色
                row.Add(deleteBtn);

                return row;
            };

            // ★ 绑定数据到行 (BindItem)
            _listView.bindItem = (element, index) =>
            {
                // 获取当前元素的 SerializedProperty
                // 注意：index 可能会变，每次 bind 时重新获取
                if (index >= listProp.arraySize)
                    return;
                SerializedProperty prop = listProp.GetArrayElementAtIndex(index);

                // 1. 绑定名字
                TextField nameField = element.Q<TextField>("NameField");
                nameField.BindProperty(prop.FindPropertyRelative("Name"));

                // 2. 绑定值 (多态核心)
                VisualElement valueContainer = element.Q("ValueContainer");
                valueContainer.Clear();

                SerializedProperty valueDataProp = prop.FindPropertyRelative("ValueData");
                // 尝试直接定位到 Value 字段以优化显示 (跳过 AsakiInt 这层外壳)
                SerializedProperty innerValueProp = valueDataProp.FindPropertyRelative("Value");

                PropertyField propField;
                if (innerValueProp != null)
                {
                    propField = new PropertyField(innerValueProp, "");
                }
                else
                {
                    // 兜底：如果是复杂结构，绘制整个 ValueData
                    propField = new PropertyField(valueDataProp, "");
                }

                propField.Bind(_serializedObject);
                valueContainer.Add(propField);

                // 3. 绑定删除按钮
                Button deleteBtn = element.Q<Button>("DeleteBtn");
                // 移除旧的事件，防止闭包捕获错误的 index
                deleteBtn.clickable.clickedWithEventInfo -= OnDeleteClicked;
                deleteBtn.userData = index; // 存储当前索引
                deleteBtn.clickable.clickedWithEventInfo += OnDeleteClicked;
            };

            // 设置行高
            _listView.fixedItemHeight = 30; // 基础高度，如果有复杂属性可能会溢出，可以用 VirtualizationMethod.DynamicHeight (Unity 2021+)
#if UNITY_2021_3_OR_NEWER
            _listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
#endif

            root.Add(_listView);
        }

        private void OnDeleteClicked(EventBase evt)
        {
            if (evt.target is Button btn && btn.userData is int index)
            {
                SerializedProperty listProp = _serializedObject.FindProperty("GlobalVariables");
                if (index >= 0 && index < listProp.arraySize)
                {
                    listProp.DeleteArrayElementAtIndex(index);
                    _serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void OnAddClicked()
        {
            string newName = _nameField.value;
            if (string.IsNullOrWhiteSpace(newName))
            {
                EditorUtility.DisplayDialog("Warning", "Variable Name cannot be empty.", "OK");
                return;
            }

            int typeIndex = _typePopup.index;
            if (typeIndex < 0 || typeIndex >= _availableTypes.Count)
                return;

            Type selectedType = _availableTypes[typeIndex];

            // 逻辑操作
            Undo.RecordObject(_globalAsset, "Add Global Variable");
            _globalAsset.GetOrCreateVariable(newName, selectedType);

            // 清空输入框
            _nameField.value = "";

            // 更新序列化对象并刷新列表
            _serializedObject.Update();
            // ListView 绑定了 Property，Update 后通常会自动刷新，但显式调用更安全
            _listView.RefreshItems();
        }
    }
}
