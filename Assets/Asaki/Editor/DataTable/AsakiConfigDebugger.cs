using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Asaki.Core.Context;
using Asaki.Core.DataTable;
using Asaki.Unity.Services.DataTable;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.DataTable
{
    public class AsakiConfigRuntimeEditor : EditorWindow
    {
        [MenuItem("Asaki/DataTable/Runtime Editor", false, 102)]
        public static void ShowWindow()
        {
            AsakiConfigRuntimeEditor wnd = GetWindow<AsakiConfigRuntimeEditor>(
                "DataTable Runtime Editor"
            );
            wnd.minSize = new Vector2(900, 600);
            wnd.Show();
        }

        // =========================================================
        // 状态与引用
        // =========================================================
        private TwoPaneSplitView _splitView;
        private ListView _tableListView;
        private MultiColumnListView _dataGrid; // 核心组件：多列列表
        private VisualElement _rightPanelContainer;
        private Label _statusLabel;

        private IAsakiConfigService _service;
        private FieldInfo _storeField; // 用于反射获取数据源

        private Type _currentType;
        private IList _currentDataList; // 当前显示的配置数据列表
        private PropertyInfo[] _currentProps; // 当前配置类的属性缓存

        // =========================================================
        // UI 构建
        // =========================================================
        private void OnEnable()
        {
            // 缓存反射信息
            _storeField = typeof(AsakiConfigService).GetField(
                "_configStore",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // 1. 顶部状态栏 (运行检测)
            _statusLabel = new Label("Waiting for Runtime...")
            {
                style =
                {
                    paddingBottom = 5,
                    paddingTop = 5,
                    paddingLeft = 5,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                },
            };
            root.Add(_statusLabel);

            // 2. 主分割视图
            _splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(_splitView);

            // --- 左侧：表单列表 ---
            VisualElement leftPane = new VisualElement();
            leftPane.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            Toolbar leftToolbar = new Toolbar();
            leftToolbar.Add(new ToolbarButton(RefreshTables) { text = "Refresh Tables" });
            leftPane.Add(leftToolbar);

            _tableListView = new ListView();
            _tableListView.makeItem = () => new Label();
            _tableListView.bindItem = (e, i) =>
            {
                Type type = (Type)_tableListView.itemsSource[i];
                (e as Label).text = type.Name;
            };
            _tableListView.selectionChanged += OnTableSelectionChanged;
            _tableListView.style.flexGrow = 1;
            leftPane.Add(_tableListView);
            _splitView.Add(leftPane);

            // --- 右侧：数据表格 ---
            VisualElement rightPane = new VisualElement();

            // 工具栏
            Toolbar rightToolbar = new Toolbar();
            rightToolbar.Add(
                new ToolbarButton(() =>
                {
                    if (_currentType != null)
                        ReloadCurrentTable();
                })
                {
                    text = "Reload from Disk",
                }
            );

            rightToolbar.Add(new ToolbarSpacer());

            // 核心功能：保存回 CSV
            ToolbarButton saveBtn = new ToolbarButton(() =>
            {
                if (_currentType != null)
                    SaveCurrentToCsv();
            })
            {
                text = "Save Memory to CSV",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.8f, 1f, 0.8f),
                },
            };
            rightToolbar.Add(saveBtn);

            rightPane.Add(rightToolbar);

            _rightPanelContainer = new VisualElement { style = { flexGrow = 1 } };
            rightPane.Add(_rightPanelContainer);
            _splitView.Add(rightPane);

            // 检查运行状态
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            bool isPlaying = Application.isPlaying;
            _splitView.SetEnabled(isPlaying);

            if (!isPlaying)
            {
                _statusLabel.text = "⚠️ Runtime Only. Please enter Play Mode.";
                _statusLabel.style.color = Color.yellow;
                _service = null;
            }
            else if (_service == null)
            {
                // 尝试获取服务
                _service = AsakiContext.Get<IAsakiConfigService>();
                if (_service != null)
                {
                    _statusLabel.text = "🟢 Connected to Asaki DataTable Service";
                    _statusLabel.style.color = Color.green;
                    RefreshTables();
                }
                else
                {
                    _statusLabel.text = "⚠️ Waiting for AsakiConfigService registration...";
                }
            }
        }

        // =========================================================
        // 逻辑控制
        // =========================================================

        private void RefreshTables()
        {
            if (_service == null || _storeField == null)
                return;

            IDictionary store = _storeField.GetValue(_service) as IDictionary;
            if (store == null)
                return;

            var types = new List<Type>();
            foreach (object key in store.Keys)
            {
                if (key is Type t)
                    types.Add(t);
            }
            types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            _tableListView.itemsSource = types;
            _tableListView.Rebuild();
        }

        private void OnTableSelectionChanged(IEnumerable<object> selection)
        {
            _currentType = selection.FirstOrDefault() as Type;
            if (_currentType != null)
            {
                BuildDataGrid(_currentType);
            }
        }

        private void ReloadCurrentTable()
        {
            if (_currentType == null)
                return;

            // 反射调用 ReloadAsync
            MethodInfo method = _service.GetType().GetMethod("ReloadAsync");
            MethodInfo generic = method.MakeGenericMethod(_currentType);
            generic.Invoke(_service, null);

            // 延迟刷新 UI 以等待异步加载完成 (简单处理，实际应await task)
            rootVisualElement
                .schedule.Execute(() => BuildDataGrid(_currentType))
                .ExecuteLater(200);
        }

        // =========================================================
        // 核心：构建多列表格 (MultiColumnListView)
        // =========================================================

        private void BuildDataGrid(Type type)
        {
            _rightPanelContainer.Clear();

            // 1. 获取数据
            MethodInfo getAllMethod = _service
                .GetType()
                .GetMethod("GetAll")
                .MakeGenericMethod(type);
            IEnumerable enumerable = getAllMethod.Invoke(_service, null) as IEnumerable;

            // 转为非泛型 List 以便索引访问
            _currentDataList = new ArrayList();
            foreach (object item in enumerable)
                _currentDataList.Add(item);

            // 2. 获取属性 (Columns)
            _currentProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite) // 仅显示可读写属性
                .ToArray();

            // 3. 创建 MultiColumnListView
            _dataGrid = new MultiColumnListView
            {
                itemsSource = _currentDataList,
                style = { flexGrow = 1 },
            };

            // 4. 动态生成列
            foreach (PropertyInfo prop in _currentProps)
            {
                Column column = new Column
                {
                    name = prop.Name,
                    title = $"{prop.Name} ({GetTypeName(prop.PropertyType)})",
                    width = GetColumnWidth(prop.PropertyType),

                    // 核心：生成单元格编辑器
                    makeCell = () => CreateCellEditor(prop.PropertyType),

                    // 核心：绑定数据与回调
                    bindCell = (element, index) => BindCellEditor(element, index, prop),
                };

                _dataGrid.columns.Add(column);
            }

            _rightPanelContainer.Add(_dataGrid);
            _statusLabel.text =
                $"Editing {type.Name} ({_currentDataList.Count} rows) - Changes are applied to MEMORY immediately.";
        }

        // =========================================================
        // 单元格编辑逻辑 (Cell Rendering & Binding)
        // =========================================================

        private VisualElement CreateCellEditor(Type type)
        {
            if (type == typeof(bool))
                return new Toggle();
            if (type == typeof(int))
                return new IntegerField();
            if (type == typeof(float))
                return new FloatField();
            if (type == typeof(string))
                return new TextField();
            if (type == typeof(Vector3))
                return new Vector3Field();

            // 默认回退
            return new TextField();
        }

        private void BindCellEditor(VisualElement element, int index, PropertyInfo prop)
        {
            if (index >= _currentDataList.Count)
                return;
            object dataObject = _currentDataList[index];
            object value = prop.GetValue(dataObject);

            // 绑定逻辑 + 注册修改回调
            // 注意：必须先解绑旧事件(虽然创建新Cell较少复用，但MCLV会有复用机制)
            // UIToolkit 的 Bind 最佳实践通常是使用userData或者清理Callback，这里简化处理

            if (element is Toggle toggle)
            {
                toggle.SetValueWithoutNotify((bool)value);
                toggle.RegisterValueChangedCallback(evt => UpdateValue(index, prop, evt.newValue));
            }
            else if (element is IntegerField intField)
            {
                intField.SetValueWithoutNotify((int)value);
                intField.RegisterValueChangedCallback(evt =>
                    UpdateValue(index, prop, evt.newValue)
                );
            }
            else if (element is FloatField floatField)
            {
                floatField.SetValueWithoutNotify((float)value);
                floatField.RegisterValueChangedCallback(evt =>
                    UpdateValue(index, prop, evt.newValue)
                );
            }
            else if (element is Vector3Field v3Field)
            {
                v3Field.SetValueWithoutNotify((Vector3)value);
                v3Field.RegisterValueChangedCallback(evt => UpdateValue(index, prop, evt.newValue));
            }
            else if (element is TextField txtField)
            {
                txtField.SetValueWithoutNotify(value?.ToString() ?? "");
                // 字符串或其他类型的回退处理
                txtField.RegisterValueChangedCallback(evt =>
                {
                    // 如果是纯字符串
                    if (prop.PropertyType == typeof(string))
                        UpdateValue(index, prop, evt.newValue);
                    // 如果是其他复杂类型，可以在这里解析字符串
                });
            }
        }

        private void UpdateValue(int index, PropertyInfo prop, object newValue)
        {
            if (index >= _currentDataList.Count)
                return;
            object dataObject = _currentDataList[index];

            // 修改内存中的对象
            prop.SetValue(dataObject, newValue);

            // 可选：高亮修改过的行，或者在控制台打印
            // Debug.Log($"[RuntimeEdit] Modified {prop.Name} -> {newValue}");
        }

        // =========================================================
        // CSV 保存逻辑
        // =========================================================

        private void SaveCurrentToCsv()
        {
            if (_currentType == null || _currentDataList == null)
                return;

            string path = Path.Combine(
                Application.streamingAssetsPath,
                "FrameworkSettings",
                _currentType.Name + ".csv"
            );

            if (
                EditorUtility.DisplayDialog(
                    "Save to CSV",
                    $"Are you sure you want to overwrite '{_currentType.Name}.csv' with current runtime memory data?",
                    "Save & Overwrite",
                    "Cancel"
                )
            )
            {
                try
                {
                    StringBuilder sb = new StringBuilder();

                    // 1. Header
                    sb.AppendLine(string.Join(",", _currentProps.Select(p => p.Name)));

                    // 2. Data Rows
                    foreach (object item in _currentDataList)
                    {
                        var values = _currentProps.Select(p =>
                            FormatValueForCsv(p.GetValue(item), p.PropertyType)
                        );
                        sb.AppendLine(string.Join(",", values));
                    }

                    File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                    Debug.Log($"[AsakiFrameworkSetting] Saved runtime data to {path}");
                    AssetDatabase.Refresh();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save CSV: {ex.Message}");
                }
            }
        }

        private string FormatValueForCsv(object val, Type type)
        {
            if (val == null)
                return "";
            if (type == typeof(Vector3))
            {
                Vector3 v = (Vector3)val;
                return $"\"{v.x},{v.y},{v.z}\""; // 加引号
            }
            // 简单处理字符串内的逗号
            if (type == typeof(string) && val.ToString().Contains(","))
            {
                return $"\"{val}\"";
            }
            return val.ToString();
        }

        // =========================================================
        // 辅助方法
        // =========================================================

        private string GetTypeName(Type t)
        {
            if (t == typeof(int))
                return "int";
            if (t == typeof(float))
                return "float";
            if (t == typeof(string))
                return "string";
            if (t == typeof(bool))
                return "bool";
            if (t == typeof(Vector3))
                return "Vec3";
            return t.Name;
        }

        private float GetColumnWidth(Type t)
        {
            if (t == typeof(int))
                return 60;
            if (t == typeof(bool))
                return 40;
            if (t == typeof(Vector3))
                return 150;
            if (t == typeof(string))
                return 120;
            return 100;
        }
    }
}
