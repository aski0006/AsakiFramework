using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Asaki.Core.Configuration;
using Asaki.Editor.Utilities.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.Configuration
{
    public class AsakiConfigDashboardWindow : EditorWindow
    {
        [MenuItem("Asaki/Configuration/Dashboard (Editor)")]
        public static void ShowWindow()
        {
            AsakiConfigDashboardWindow wnd = GetWindow<AsakiConfigDashboardWindow>(
                "Configuration Dashboard"
            );
            wnd.minSize = new Vector2(1000, 600);
            wnd.Show();
        }

        // =========================================================
        // 状态变量
        // =========================================================
        private static string ConfigPath =>
            Path.Combine(Application.streamingAssetsPath, "Configs");

        private ListView _classListView;
        private VisualElement _detailContainer;
        private VisualElement _tableContainer;
        private Label _pageInfoLabel;

        private List<Type> _allConfigTypes;
        private Type _selectedType;

        // CSV 数据状态
        private List<string> _headers = new List<string>();
        private List<List<string>> _csvData = new List<List<string>>();
        private Dictionary<string, Type> _columnTypeMap = new Dictionary<string, Type>();

        // [新增] 行选择状态
        private HashSet<int> _selectedIndices = new HashSet<int>();

        private bool _isDirty = false;
        private int _currentPage = 0;
        private const int PAGE_SIZE = 10;

        // =========================================================
        // UI 构建
        // =========================================================

        public void CreateGUI()
        {
            if (!Directory.Exists(ConfigPath))
                Directory.CreateDirectory(ConfigPath);

            VisualElement root = rootVisualElement;
            TwoPaneSplitView splitView = new TwoPaneSplitView(
                0,
                250,
                TwoPaneSplitViewOrientation.Horizontal
            );
            root.Add(splitView);

            // --- 左侧：列表栏 ---
            VisualElement leftPane = new VisualElement
            {
                style = { backgroundColor = new Color(0.18f, 0.18f, 0.18f) },
            };

            Toolbar toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(RefreshList) { text = "Refresh Scan" });
            toolbar.Add(
                new ToolbarButton(() => EditorUtility.RevealInFinder(ConfigPath))
                {
                    text = "Open Folder",
                }
            );
            leftPane.Add(toolbar);

            _classListView = new ListView();
            _classListView.makeItem = () => new Label();
            _classListView.bindItem = BindListItem;
            _classListView.selectionChanged += OnConfigListSelected;
            _classListView.style.flexGrow = 1;
            leftPane.Add(_classListView);

            splitView.Add(leftPane);

            // --- 右侧：编辑器 ---
            VisualElement rightPane = new VisualElement
            {
                style =
                {
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                },
            };
            _detailContainer = new VisualElement { style = { flexGrow = 1 } };
            rightPane.Add(_detailContainer);

            splitView.Add(rightPane);

            root.schedule.Execute(RefreshList).ExecuteLater(10);
        }

        private void RefreshList()
        {
            _allConfigTypes = TypeCache
                .GetTypesDerivedFrom<IAsakiConfig>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToList();

            if (_classListView != null)
            {
                _classListView.itemsSource = _allConfigTypes;
                _classListView.Rebuild();
            }
        }

        private void BindListItem(VisualElement element, int index)
        {
            if (index >= _allConfigTypes.Count)
                return;
            Type type = _allConfigTypes[index];
            Label label = element as Label;
            if (label == null)
                return;

            string path = Path.Combine(ConfigPath, type.Name + ".csv");
            label.text = type.Name;
            label.style.color = File.Exists(path)
                ? new Color(0.6f, 1f, 0.6f)
                : new Color(1f, 0.6f, 0.6f);
        }

        private void OnConfigListSelected(IEnumerable<object> selection)
        {
            if (_isDirty)
            {
                bool save = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    "Save changes before switching?",
                    "Save",
                    "Discard"
                );
                if (save)
                    SaveCsv();
            }

            _selectedType = selection.FirstOrDefault() as Type;
            if (_selectedType != null)
            {
                LoadCsvForType(_selectedType);
                RebuildDetailPanel();
            }
        }

        // =========================================================
        // CSV 核心逻辑
        // =========================================================

        private void LoadCsvForType(Type type)
        {
            _csvData.Clear();
            _headers.Clear();
            _columnTypeMap.Clear();
            _selectedIndices.Clear(); // 清空选择
            _currentPage = 0;
            _isDirty = false;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in props)
            {
                _columnTypeMap[prop.Name] = prop.PropertyType;
            }

            string path = Path.Combine(ConfigPath, type.Name + ".csv");
            if (!File.Exists(path))
                return;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                if (lines.Length > 0)
                {
                    _headers = ParseCsvLine(lines[0]);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i]))
                            continue;
                        _csvData.Add(ParseCsvLine(lines[i]));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Load CSV Error: {e.Message}");
            }
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool insideQuotes = false;
            StringBuilder current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                    insideQuotes = !insideQuotes;
                else if (c == ',' && !insideQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }
            result.Add(current.ToString());
            return result;
        }

        // =========================================================
        // 右侧编辑器绘制
        // =========================================================

        private void RebuildDetailPanel()
        {
            _detailContainer.Clear();
            if (_selectedType == null)
                return;

            string path = Path.Combine(ConfigPath, _selectedType.Name + ".csv");
            bool fileExists = File.Exists(path);

            DrawHeaderInfo(fileExists);

            if (fileExists)
            {
                DrawTableEditor();
            }
            else
            {
                Button btn = new Button(() => GenerateDefaultCsv(_selectedType, path))
                {
                    text = "Generate Default CSV",
                    style =
                    {
                        height = 40,
                        marginTop = 20,
                        backgroundColor = new Color(0.2f, 0.5f, 0.2f),
                    },
                };
                _detailContainer.Add(btn);
            }
        }

        private void DrawHeaderInfo(bool fileExists)
        {
            // 标题栏容器
            VisualElement headerBox = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column, marginBottom = 10 },
            };

            // 第一行：标题 + 状态
            VisualElement titleRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                },
            };
            Label title = new Label(_selectedType.Name)
            {
                style = { fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold },
            };
            titleRow.Add(title);

            if (fileExists)
            {
                VisualElement rightTools = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row },
                };
                Button saveBtn = new Button(SaveCsv) { text = "Save Changes" };
                if (_isDirty)
                    saveBtn.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                rightTools.Add(saveBtn);
                titleRow.Add(rightTools);
            }
            headerBox.Add(titleRow);

            // 第二行：操作工具栏
            if (fileExists)
            {
                VisualElement toolbar = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginTop = 5,
                        backgroundColor = new Color(0.22f, 0.22f, 0.22f),
                        borderBottomLeftRadius = 4,
                        borderBottomRightRadius = 4,
                    },
                };
                toolbar.style.SetPadding(4);
                // 数据操作
                toolbar.Add(
                    new Label("Data Ops: ")
                    {
                        style =
                        {
                            alignSelf = Align.Center,
                            marginRight = 5,
                            color = Color.gray,
                        },
                    }
                );
                toolbar.Add(
                    new Button(ReindexIds) { text = "Reindex ID", style = { marginRight = 2 } }
                );
                toolbar.Add(
                    new Button(async () => await ReorderIdsAsync())
                    {
                        text = "Sort ID",
                        style = { marginRight = 10 },
                    }
                );

                // [需求1] 行操作
                toolbar.Add(
                    new Label("Row Ops: ")
                    {
                        style =
                        {
                            alignSelf = Align.Center,
                            marginRight = 5,
                            color = Color.gray,
                        },
                    }
                );
                toolbar.Add(
                    new Button(AddNewRow)
                    {
                        text = "+ Add Row",
                        style = { backgroundColor = new Color(0.2f, 0.4f, 0.2f) },
                    }
                );

                // [需求2] 批量删除
                string delText =
                    _selectedIndices.Count > 0
                        ? $"Delete Selected ({_selectedIndices.Count})"
                        : "Delete (Select Rows)";
                Button delBtn = new Button(DeleteSelectedRows) { text = delText };
                if (_selectedIndices.Count > 0)
                    delBtn.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                else
                    delBtn.SetEnabled(false);

                toolbar.Add(delBtn);

                headerBox.Add(toolbar);
            }
            _detailContainer.Add(headerBox);
        }

        private void DrawTableEditor()
        {
            _tableContainer = new ScrollView(ScrollViewMode.Vertical);
            _tableContainer.style.flexGrow = 1;
            _tableContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            // --- Header Row ---
            VisualElement headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                    paddingBottom = 5,
                    paddingTop = 5,
                },
            };

            // 选择全选框列
            Toggle selectAllToggle = new Toggle { style = { width = 30, paddingLeft = 5 } };
            selectAllToggle.RegisterValueChangedCallback(evt => SelectAllOnPage(evt.newValue));
            headerRow.Add(selectAllToggle);

            for (int i = 0; i < _headers.Count; i++)
            {
                string colName = _headers[i];
                float width = IsVectorType(colName) ? 200 : 120;
                Label label = new Label(colName)
                {
                    style =
                    {
                        width = width,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    },
                };

                // [需求3] 批量列操作：右键菜单
                int colIndex = i; // capture
                label.RegisterCallback<ContextClickEvent>(evt =>
                    ShowColumnContextMenu(evt, colIndex, colName)
                );

                headerRow.Add(label);
            }
            _tableContainer.Add(headerRow);

            // --- Data Rows ---
            int start = _currentPage * PAGE_SIZE;
            int end = Mathf.Min(start + PAGE_SIZE, _csvData.Count);

            for (int i = start; i < end; i++)
            {
                var rowData = _csvData[i];
                bool isSelected = _selectedIndices.Contains(i);

                VisualElement rowVis = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        borderBottomWidth = 1,
                        borderBottomColor = new Color(0.3f, 0.3f, 0.3f),
                        alignItems = Align.Center,
                    },
                };
                // 选中高亮
                if (isSelected)
                    rowVis.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f, 0.5f);

                int rowIndex = i;

                // 1. 行选择 Checkbox
                Toggle rowSelect = new Toggle
                {
                    value = isSelected,
                    style = { width = 30, paddingLeft = 5 },
                };
                rowSelect.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                        _selectedIndices.Add(rowIndex);
                    else
                        _selectedIndices.Remove(rowIndex);
                    // 重新绘制以刷新 Header 上的删除按钮状态
                    RebuildDetailPanel();
                });
                rowVis.Add(rowSelect);

                // 2. 数据列
                for (int c = 0; c < _headers.Count; c++)
                {
                    int colIndex = c;
                    string colName = _headers[c];
                    string val = c < rowData.Count ? rowData[c] : "";

                    Type colType = _columnTypeMap.ContainsKey(colName)
                        ? _columnTypeMap[colName]
                        : typeof(string);
                    VisualElement cellElement = CreateCell(rowIndex, colIndex, val, colType);
                    rowVis.Add(cellElement);
                }
                _tableContainer.Add(rowVis);
            }

            _detailContainer.Add(_tableContainer);
            DrawPaginationControls();
        }

        // =========================================================
        // [需求3] 批量列操作逻辑
        // =========================================================

        private void ShowColumnContextMenu(ContextClickEvent evt, int colIndex, string colName)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent($"Batch Edit '{colName}'/Fill ALL Rows"),
                false,
                () => OpenBulkEditPopup(colIndex, colName, false)
            );

            if (_selectedIndices.Count > 0)
            {
                menu.AddItem(
                    new GUIContent(
                        $"Batch Edit '{colName}'/Fill SELECTED Rows ({_selectedIndices.Count})"
                    ),
                    false,
                    () => OpenBulkEditPopup(colIndex, colName, true)
                );
            }
            else
            {
                menu.AddDisabledItem(new GUIContent($"Batch Edit '{colName}'/Fill SELECTED Rows"));
            }

            menu.ShowAsContext();
        }

        private void OpenBulkEditPopup(int colIndex, string colName, bool onlySelected)
        {
            // 弹出一个小窗口输入值
            BulkEditPopup.Show(
                colName,
                (newVal) =>
                {
                    ApplyBulkEdit(colIndex, newVal, onlySelected);
                }
            );
        }

        private void ApplyBulkEdit(int colIndex, string newValue, bool onlySelected)
        {
            int count = 0;
            for (int i = 0; i < _csvData.Count; i++)
            {
                if (onlySelected && !_selectedIndices.Contains(i))
                    continue;

                // 确保行有足够的列
                while (colIndex >= _csvData[i].Count)
                {
                    _csvData[i].Add("");
                }

                if (_csvData[i][colIndex] != newValue)
                {
                    _csvData[i][colIndex] = newValue;
                    count++;
                }
            }

            if (count > 0)
            {
                _isDirty = true;
                RebuildDetailPanel();
                Debug.Log(
                    $"[Bulk Edit] Updated {count} rows in column '{_headers[colIndex]}' to '{newValue}'"
                );
            }
        }

        // =========================================================
        // [需求1 & 2] 行操作逻辑
        // =========================================================

        private void AddNewRow()
        {
            // 1. 创建默认行
            var newRow = new List<string>();
            foreach (string header in _headers)
            {
                Type t = _columnTypeMap.ContainsKey(header)
                    ? _columnTypeMap[header]
                    : typeof(string);
                // 特殊处理 ID：自动递增
                if (header == "Id" || header == "ID")
                {
                    int maxId = 0;
                    foreach (var row in _csvData)
                    {
                        // 尝试查找最大ID
                        int idx = _headers.IndexOf(header);
                        if (idx >= 0 && idx < row.Count && int.TryParse(row[idx], out int id))
                        {
                            if (id > maxId)
                                maxId = id;
                        }
                    }
                    newRow.Add((maxId + 1).ToString());
                }
                else
                {
                    newRow.Add(GetDefaultValueString(t).Replace("\"", "")); // 去除默认值的引号
                }
            }

            _csvData.Add(newRow);
            _isDirty = true;

            // 2. 跳转到最后一页
            int totalPages = Mathf.CeilToInt((float)_csvData.Count / PAGE_SIZE);
            if (totalPages > 0)
                _currentPage = totalPages - 1;

            RebuildDetailPanel();
        }

        private void DeleteSelectedRows()
        {
            if (_selectedIndices.Count == 0)
                return;

            if (
                EditorUtility.DisplayDialog(
                    "Confirm Delete",
                    $"Delete {_selectedIndices.Count} rows?",
                    "Delete",
                    "Cancel"
                )
            )
            {
                // 从后往前删，防止索引偏移
                var sortedIndices = _selectedIndices.OrderByDescending(i => i).ToList();
                foreach (int index in sortedIndices)
                {
                    if (index < _csvData.Count)
                    {
                        _csvData.RemoveAt(index);
                    }
                }

                _selectedIndices.Clear();
                _isDirty = true;

                // 防止页码溢出
                int totalPages = Mathf.CeilToInt((float)_csvData.Count / PAGE_SIZE);
                if (_currentPage >= totalPages && totalPages > 0)
                    _currentPage = totalPages - 1;

                RebuildDetailPanel();
            }
        }

        private void SelectAllOnPage(bool select)
        {
            int start = _currentPage * PAGE_SIZE;
            int end = Mathf.Min(start + PAGE_SIZE, _csvData.Count);

            for (int i = start; i < end; i++)
            {
                if (select)
                    _selectedIndices.Add(i);
                else
                    _selectedIndices.Remove(i);
            }
            RebuildDetailPanel();
        }

        // =========================================================
        // 辅助方法 (Cell Creation & Parsing)
        // =========================================================

        private bool IsVectorType(string colName)
        {
            if (_columnTypeMap.TryGetValue(colName, out Type t))
                return t == typeof(Vector3) || t == typeof(Vector2);
            return false;
        }

        private VisualElement CreateCell(int rowIndex, int colIndex, string val, Type type)
        {
            if (type == typeof(bool))
            {
                bool boolVal = val.ToLower() == "true" || val == "1";
                Toggle toggle = new Toggle
                {
                    value = boolVal,
                    style =
                    {
                        width = 120,
                        marginRight = 2,
                        justifyContent = Justify.Center,
                    },
                };
                toggle.RegisterValueChangedCallback(evt =>
                    UpdateCellData(rowIndex, colIndex, evt.newValue.ToString().ToLower())
                );
                return toggle;
            }
            else if (type == typeof(Vector3))
            {
                Vector3 vec3 = ParseVector3(val);
                Vector3Field field = new Vector3Field
                {
                    value = vec3,
                    style = { width = 200, marginRight = 2 },
                };
                field.RegisterValueChangedCallback(evt =>
                    UpdateCellData(
                        rowIndex,
                        colIndex,
                        $"{evt.newValue.x},{evt.newValue.y},{evt.newValue.z}"
                    )
                );
                return field;
            }
            else if (type == typeof(Vector2))
            {
                Vector2 vec2 = ParseVector2(val);
                Vector2Field field = new Vector2Field
                {
                    value = vec2,
                    style = { width = 200, marginRight = 2 },
                };
                field.RegisterValueChangedCallback(evt =>
                    UpdateCellData(rowIndex, colIndex, $"{evt.newValue.x},{evt.newValue.y}")
                );
                return field;
            }
            else
            {
                TextField textField = new TextField
                {
                    value = val,
                    style = { width = 120, marginRight = 2 },
                };
                textField.RegisterValueChangedCallback(evt =>
                    UpdateCellData(rowIndex, colIndex, evt.newValue)
                );
                return textField;
            }
        }

        private Vector3 ParseVector3(string s)
        {
            s = s.Trim('"');
            string[] parts = s.Split(',');
            if (parts.Length < 3)
                return Vector3.zero;
            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            float.TryParse(parts[2], out float z);
            return new Vector3(x, y, z);
        }

        private Vector2 ParseVector2(string s)
        {
            s = s.Trim('"');
            string[] parts = s.Split(',');
            if (parts.Length < 2)
                return Vector2.zero;
            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            return new Vector2(x, y);
        }

        private void UpdateCellData(int row, int col, string newValue)
        {
            while (row >= _csvData.Count)
            {
                _csvData.Add(new List<string>());
            }
            var rowList = _csvData[row];
            while (col >= rowList.Count)
            {
                rowList.Add("");
            }

            if (rowList[col] != newValue)
            {
                rowList[col] = newValue;
                _isDirty = true;
            }
        }

        private void ReindexIds()
        {
            int idIndex = _headers.IndexOf("Id");
            if (idIndex == -1)
                idIndex = _headers.IndexOf("ID");
            if (idIndex == -1)
            {
                EditorUtility.DisplayDialog("Error", "Column 'Id' not found.", "OK");
                return;
            }

            for (int i = 0; i < _csvData.Count; i++)
            {
                while (idIndex >= _csvData[i].Count)
                {
                    _csvData[i].Add("");
                }
                _csvData[i][idIndex] = (i + 1).ToString();
            }
            _isDirty = true;
            RebuildDetailPanel();
            Debug.Log("[AsakiDashboard] IDs reindexed from 1.");
        }

        private async Task ReorderIdsAsync()
        {
            int idIndex = _headers.IndexOf("Id");
            if (idIndex == -1)
                idIndex = _headers.IndexOf("ID");
            if (idIndex == -1)
            {
                EditorUtility.DisplayDialog("Error", "Column 'Id' not found.", "OK");
                return;
            }

            await Task.Run(() =>
            {
                _csvData.Sort(
                    (rowA, rowB) =>
                    {
                        string valA = idIndex < rowA.Count ? rowA[idIndex] : "0";
                        string valB = idIndex < rowB.Count ? rowB[idIndex] : "0";
                        if (int.TryParse(valA, out int a) && int.TryParse(valB, out int b))
                            return a.CompareTo(b);
                        return string.Compare(valA, valB);
                    }
                );
            });

            _isDirty = true;
            _currentPage = 0;
            RebuildDetailPanel();
            Debug.Log("[AsakiDashboard] Async Sort Completed.");
        }

        private void SaveCsv()
        {
            if (_selectedType == null)
                return;
            string path = Path.Combine(ConfigPath, _selectedType.Name + ".csv");
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Join(",", _headers));
                foreach (var row in _csvData)
                {
                    var escapedRow = row.Select(cell =>
                    {
                        if (cell.Contains(","))
                            return $"\"{cell}\"";
                        return cell;
                    });
                    sb.AppendLine(string.Join(",", escapedRow));
                }
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                _isDirty = false;
                AssetDatabase.Refresh();
                RebuildDetailPanel();
                Debug.Log($"[AsakiDashboard] Saved {_selectedType.Name}.csv");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save Failed: {ex.Message}");
            }
        }

        private void GenerateDefaultCsv(Type type, string path)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.CanRead)
                .ToArray();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join(",", props.Select(p => p.Name)));
            var defaultValues = props.Select(p => GetDefaultValueString(p.PropertyType));
            sb.AppendLine(string.Join(",", defaultValues));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            RefreshList();
            LoadCsvForType(type);
            RebuildDetailPanel();
        }

        private string GetDefaultValueString(Type t)
        {
            if (t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double))
                return "0";
            if (t == typeof(string))
                return "default";
            if (t == typeof(bool))
                return "false";
            if (t == typeof(Vector3))
                return "\"0,0,0\"";
            if (t == typeof(Vector2))
                return "\"0,0\"";
            return "null";
        }

        private void DrawPaginationControls()
        {
            VisualElement footer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center,
                    marginTop = 10,
                    height = 30,
                },
            };
            int totalPages = Mathf.CeilToInt((float)_csvData.Count / PAGE_SIZE);
            if (totalPages == 0)
                totalPages = 1;

            Button prevBtn = new Button(() =>
            {
                if (_currentPage > 0)
                {
                    _currentPage--;
                    RebuildDetailPanel();
                }
            })
            {
                text = "< Prev",
            };
            Button nextBtn = new Button(() =>
            {
                if (_currentPage < totalPages - 1)
                {
                    _currentPage++;
                    RebuildDetailPanel();
                }
            })
            {
                text = "Next >",
            };

            _pageInfoLabel = new Label(
                $"Page {_currentPage + 1} / {totalPages} (Total: {_csvData.Count})"
            )
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 5,
                },
            };

            footer.Add(prevBtn);
            footer.Add(_pageInfoLabel);
            footer.Add(nextBtn);
            if (_isDirty)
                footer.Add(
                    new Label("  [Unsaved Changes]")
                    {
                        style = { color = Color.yellow, paddingTop = 5 },
                    }
                );
            _detailContainer.Add(footer);
        }
    }

    // =========================================================
    // 简单的输入弹窗 (用于批量修改)
    // =========================================================
    public class BulkEditPopup : EditorWindow
    {
        private static Action<string> _onConfirm;
        private static string _title;
        private string _input = "";

        public static void Show(string colName, Action<string> onConfirm)
        {
            _title = $"Bulk Edit: {colName}";
            _onConfirm = onConfirm;
            BulkEditPopup wnd = GetWindow<BulkEditPopup>(true, "Batch Edit", true);
            wnd.minSize = new Vector2(300, 100);
            wnd.maxSize = new Vector2(300, 100);
            wnd.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter new value for this column (Vector format: x,y,z)",
                MessageType.Info
            );

            GUI.SetNextControlName("BulkInput");
            _input = EditorGUILayout.TextField(_input);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply"))
            {
                _onConfirm?.Invoke(_input);
                Close();
            }

            // 自动聚焦
            if (Event.current.type == EventType.Repaint)
                EditorGUI.FocusTextInControl("BulkInput");
        }
    }
}
