using System;
using System.Collections.Generic;
using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Graphs;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.GraphEditors
{
    /// <summary>
    /// 负责管理 GraphView 中的 Blackboard 面板 (UI Toolkit)
    /// [V5.0 Refactor] 支持 AsakiValueBase 多态类型与原生序列化绘制
    /// </summary>
    public class AsakiBlackboardProvider
    {
        private const string AssetPath =
            "Assets/Resources/Asaki/Configuration/GlobalBlackboard.asset";
        public Blackboard Blackboard { get; private set; }

        private readonly AsakiGraphView _graphView;
        private readonly AsakiGraphAsset _graphAsset;
        private readonly SerializedObject _serializedGraph;

        private AsakiGlobalBlackboardAsset _globalAsset;
        private SerializedObject _serializedGlobal; // [New] 用于全局变量的 PropertyField 绘制

        public AsakiBlackboardProvider(
            AsakiGraphView graphView,
            AsakiGraphAsset graphAsset,
            SerializedObject serializedGraph
        )
        {
            _graphView = graphView;
            _graphAsset = graphAsset;
            _serializedGraph = serializedGraph;

            LoadGlobalBlackboard();
            InitializeBlackboard();
            RefreshBlackboard();
        }

        private void LoadGlobalBlackboard()
        {
            _globalAsset = AssetDatabase.LoadAssetAtPath<AsakiGlobalBlackboardAsset>(AssetPath);

            if (!_globalAsset)
            {
                // 可以在这里提示或者静默失败
                // Debug.LogWarning("[Asaki] Global Blackboard asset not found.");
            }
            else
            {
                // 初始化全局资产的 SerializedObject，用于绘制只读属性
                _serializedGlobal = new SerializedObject(_globalAsset);
            }
        }

        private void InitializeBlackboard()
        {
            // 1. 创建 Blackboard UI 控件
            Blackboard = new Blackboard(_graphView)
            {
                title = "Blackboard",
                subTitle = "Variables",

                // 2. [重构] 动态生成添加菜单 (TypeCache)
                addItemRequested = _ =>
                {
                    GenericMenu menu = new GenericMenu();

                    // 查找所有继承自 AsakiValueBase 的非抽象类型
                    TypeCache.TypeCollection valueTypes =
                        TypeCache.GetTypesDerivedFrom<AsakiValueBase>();

                    foreach (Type type in valueTypes)
                    {
                        if (type.IsAbstract)
                            continue;

                        // 美化菜单名称：移除 "Asaki" 前缀 (e.g., AsakiInt -> Int)
                        string menuName = type.Name.Replace("Asaki", "");
                        menu.AddItem(new GUIContent(menuName), false, () => AddVariable(type));
                    }
                    menu.ShowAsContext();
                },

                // 3. 配置重命名
                editTextRequested = (bb, element, newName) =>
                {
                    BlackboardField field = (BlackboardField)element;
                    AsakiVariableDef variable = (AsakiVariableDef)field.userData;

                    if (string.IsNullOrEmpty(newName) || newName == variable.Name)
                        return;

                    // 检查是否试图在局部面板修改全局变量
                    if (_globalAsset != null && _globalAsset.GlobalVariables.Contains(variable))
                    {
                        EditorUtility.DisplayDialog(
                            "Error",
                            "Cannot edit global variables here. Use Global Blackboard Editor.",
                            "OK"
                        );
                        return;
                    }

                    // 局部重名检查
                    if (_graphAsset.Variables.Exists(v => v.Name == newName))
                    {
                        EditorUtility.DisplayDialog("Error", "Variable name already exists!", "OK");
                        return;
                    }

                    Undo.RecordObject(_graphAsset, "Rename Variable");
                    variable.Name = newName;
                    field.text = newName;
                    EditorUtility.SetDirty(_graphAsset);
                },
            };

            // 4. 设置位置与滚动
            Blackboard.SetPosition(new Rect(10, 30, 200, 300));
            _graphView.Add(Blackboard);
            Blackboard.scrollable = true;

            // 5. 键盘删除支持
            Blackboard.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
                {
                    DeleteSelectedVariables();
                    evt.StopImmediatePropagation();
                }
            });

            // 6. 右键菜单：提升为全局变量
            Blackboard.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                if (Blackboard.selection.Count > 0)
                {
                    evt.menu.AppendAction(
                        "Promote to Global",
                        OnPromoteToGlobal,
                        DropdownMenuAction.Status.Normal
                    );
                }
            });
        }

        // [重构] 提升变量逻辑
        private void OnPromoteToGlobal(DropdownMenuAction action)
        {
            var selection = Blackboard.selection;
            if (selection == null || selection.Count == 0)
                return;

            if (!_globalAsset)
            {
                LoadGlobalBlackboard(); // 尝试重新加载
                if (!_globalAsset)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "Global Blackboard asset not found.",
                        "OK"
                    );
                    return;
                }
            }

            Undo.RecordObject(_graphAsset, "Promote Variables");
            Undo.RecordObject(_globalAsset, "Promote Variables");

            bool promotedAny = false;

            foreach (ISelectable element in selection)
            {
                if (element is BlackboardField { userData: AsakiVariableDef localVar })
                {
                    if (localVar.ValueData == null)
                        continue;

                    // 1. 获取类型
                    Type valType = localVar.ValueData.GetType();

                    // 2. 在全局创建变量
                    AsakiVariableDef globalVar = _globalAsset.GetOrCreateVariable(
                        localVar.Name,
                        valType
                    );

                    // 3. 复制数据 (Clone)
                    globalVar.ValueData = localVar.ValueData.Clone();

                    // 4. 移除局部
                    _graphAsset.Variables.Remove(localVar);
                    promotedAny = true;
                }
            }

            if (promotedAny)
            {
                EditorUtility.SetDirty(_graphAsset);
                EditorUtility.SetDirty(_globalAsset);
                RefreshBlackboard();
                AssetDatabase.SaveAssets(); // 建议保存，防止引用丢失
            }
        }

        private void DeleteSelectedVariables()
        {
            var selection = Blackboard.selection;
            if (selection == null || selection.Count == 0)
                return;

            Undo.RecordObject(_graphAsset, "Delete Variables");

            bool changed = false;
            var varsToRemove = new List<AsakiVariableDef>();

            foreach (ISelectable element in selection)
            {
                if (element is BlackboardField field && field.userData is AsakiVariableDef def)
                {
                    // 确保不是全局变量
                    if (_globalAsset == null || !_globalAsset.GlobalVariables.Contains(def))
                    {
                        varsToRemove.Add(def);
                    }
                }
            }

            foreach (AsakiVariableDef def in varsToRemove)
            {
                _graphAsset.Variables.Remove(def);
                changed = true;
            }

            if (changed)
            {
                RefreshBlackboard();
                EditorUtility.SetDirty(_graphAsset);
            }
        }

        // [重构] 添加变量 (参数改为 Type)
        private void AddVariable(Type valueType)
        {
            Undo.RecordObject(_graphAsset, "Add Variable");

            // 实例化多态数据
            AsakiValueBase valInstance = Activator.CreateInstance(valueType) as AsakiValueBase;
            string typeName = valueType.Name.Replace("Asaki", "");

            // 生成唯一名称
            string name = "New" + typeName;
            int i = 1;
            while (_graphAsset.Variables.Exists(v => v.Name == name))
            {
                name = $"New{typeName}_{i++}";
            }

            // 创建定义
            AsakiVariableDef newVar = new AsakiVariableDef { Name = name, ValueData = valInstance };

            _graphAsset.Variables.Add(newVar);
            EditorUtility.SetDirty(_graphAsset);
            RefreshBlackboard();
        }

        public void RefreshBlackboard()
        {
            Blackboard.Clear();

            // 1. 全局变量节 (只读)
            if (_globalAsset != null && _globalAsset.GlobalVariables.Count > 0)
            {
                // 确保 SO 是新的
                if (_serializedGlobal == null || _serializedGlobal.targetObject != _globalAsset)
                    _serializedGlobal = new SerializedObject(_globalAsset);

                _serializedGlobal.Update();

                BlackboardSection globalSection = new BlackboardSection
                {
                    title = "Global Variables (Read-Only)",
                    style = { backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f)) },
                };
                Blackboard.Add(globalSection);

                foreach (AsakiVariableDef globalVar in _globalAsset.GlobalVariables)
                {
                    VisualElement field = CreateGlobalVariableField(globalVar);
                    globalSection.Add(field);
                }
            }

            // 2. 局部变量节
            BlackboardSection localSection = new BlackboardSection { title = "Local Variables" };
            Blackboard.Add(localSection);

            // 确保 Graph SO 更新
            _serializedGraph.Update();

            foreach (AsakiVariableDef variable in _graphAsset.Variables)
            {
                VisualElement field = CreateLocalVariableField(variable);
                localSection.Add(field);
            }
        }

        // [New] 创建全局变量行
        private VisualElement CreateGlobalVariableField(AsakiVariableDef globalVar)
        {
            BlackboardField field = new BlackboardField
            {
                text = globalVar.Name,
                typeText = $"[Global] {globalVar.TypeName}", // 使用 TypeName
                userData = globalVar,
                style =
                {
                    opacity = 0.6f,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    color = new StyleColor(Color.gray),
                },
            };

            EnableDrag(field, globalVar, true);
            BlackboardRow row = new BlackboardRow(field, CreateGlobalValueView(globalVar));
            return row;
        }

        // [New] 创建局部变量行
        private VisualElement CreateLocalVariableField(AsakiVariableDef variable)
        {
            BlackboardField field = new BlackboardField
            {
                text = variable.Name,
                typeText = variable.TypeName, // 使用 TypeName
                userData = variable,
            };

            EnableDrag(field, variable, false);
            BlackboardRow row = new BlackboardRow(field, CreateValueView(variable));
            return row;
        }

        // [重构] 绘制全局变量值 (使用 PropertyField 并禁用)
        private VisualElement CreateGlobalValueView(AsakiVariableDef variable)
        {
            VisualElement container = new VisualElement();
            container.style.paddingLeft = 10;
            container.style.opacity = 0.7f;

            if (_serializedGlobal == null)
                return container;

            // 1. 查找变量在 Global SO 中的位置
            int index = _globalAsset.GlobalVariables.IndexOf(variable);
            if (index < 0)
                return container;

            SerializedProperty listProp = _serializedGlobal.FindProperty("GlobalVariables");
            if (listProp != null && index < listProp.arraySize)
            {
                SerializedProperty varProp = listProp.GetArrayElementAtIndex(index);
                // 定位到 ValueData.Value
                SerializedProperty valueProp = varProp
                    .FindPropertyRelative("ValueData")
                    .FindPropertyRelative("Value");

                if (valueProp != null)
                {
                    PropertyField propField = new PropertyField(valueProp, " ");
                    propField.Bind(_serializedGlobal);
                    propField.SetEnabled(false); // 只读
                    container.Add(propField);
                }
            }
            return container;
        }

        // [重构] 绘制局部变量值 (通用 PropertyField)
        private VisualElement CreateValueView(AsakiVariableDef variable)
        {
            VisualElement container = new VisualElement();
            container.style.paddingLeft = 10;

            // 1. 查找变量在 Graph SO 中的位置
            int index = _graphAsset.Variables.IndexOf(variable);
            if (index < 0)
                return container;

            SerializedProperty listProp = _serializedGraph.FindProperty("Variables");
            if (listProp != null && index < listProp.arraySize)
            {
                SerializedProperty varProp = listProp.GetArrayElementAtIndex(index);

                // 2. 定位到 ValueData
                SerializedProperty valueDataProp = varProp.FindPropertyRelative("ValueData");

                // 3. 尝试定位到具体的 Value 字段 (AsakiValue<T>.Value)
                // 这样可以跳过外层的 ValueData 折叠页，直接显示 int/float/struct 字段
                SerializedProperty innerValueProp = valueDataProp.FindPropertyRelative("Value");

                if (innerValueProp != null)
                {
                    // 显示内部值 (Label 留空以节省左侧空间)
                    PropertyField propField = new PropertyField(innerValueProp, " ");
                    propField.Bind(_serializedGraph);
                    container.Add(propField);
                }
                else
                {
                    // 兜底：如果找不到 Value (可能是极其复杂的自定义类型)，绘制整个 Data
                    PropertyField propField = new PropertyField(valueDataProp, " ");
                    propField.Bind(_serializedGraph);
                    container.Add(propField);
                }
            }

            return container;
        }

        private void EnableDrag(
            VisualElement element,
            AsakiVariableDef variable,
            bool isGlobal = false
        )
        {
            Vector2 _mouseDownPos = Vector2.zero;
            bool _readyToDrag = false;

            element.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    _mouseDownPos = evt.localMousePosition;
                    _readyToDrag = true;
                }
            });

            element.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!_readyToDrag || evt.pressedButtons != 1)
                    return;

                // 防止重复拖拽
                if (DragAndDrop.GetGenericData("AsakiVariable") != null)
                {
                    _readyToDrag = false;
                    return;
                }

                // 距离阈值检测
                if (Vector2.Distance(evt.localMousePosition, _mouseDownPos) > 5f)
                {
                    _readyToDrag = false;

                    // 包装拖拽数据
                    DragVariableData dragData = new DragVariableData(variable, isGlobal);

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("AsakiVariable", dragData);
                    DragAndDrop.StartDrag($"Dragging {(isGlobal ? "[G]" : "")} {variable.Name}");

                    evt.StopImmediatePropagation();
                }
            });

            element.RegisterCallback<MouseUpEvent>(evt => _readyToDrag = false);
            element.RegisterCallback<MouseLeaveEvent>(evt => _readyToDrag = false);
        }
    }
}
