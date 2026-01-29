using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.Debugging
{
    public class AsakiSaveInspector : EditorWindow
    {
        // === UI 成员变量 (直接持有引用，拒绝 Queries) ===
        private ListView _slotListView;
        private Label _contentLabel;
        private Label _statusLabel;
        private Label _fileNameLabel;
        private Label _metaLabel;
        private VisualElement _rightPane; // 直接持有右侧面板
        private ToolbarSearchField _searchField;

        // === 数据 ===
        private string _saveRoot;

        // 存储的是目录信息，而非单个文件 - 使用新版本的数据模型
        private List<DirectoryInfo> _slots = new List<DirectoryInfo>();

        [MenuItem("Asaki/Debugger/Save Inspector", false, 100)]
        public static void ShowWindow()
        {
            AsakiSaveInspector wnd = GetWindow<AsakiSaveInspector>();
            wnd.titleContent = new GUIContent(
                "Save Inspector",
                EditorGUIUtility.IconContent("SaveActive").image
            );
            wnd.minSize = new Vector2(800, 500);
        }

        public void CreateGUI()
        {
            // 路径适配新的 Slot 结构 - 使用新版本的路径
            _saveRoot = Path.Combine(Application.persistentDataPath, "Saves");

            // 1. 根节点设置：确保填满窗口
            rootVisualElement.style.flexGrow = 1;

            // 2. 分割视图 (SplitView)
            TwoPaneSplitView splitView = new TwoPaneSplitView(
                0,
                250,
                TwoPaneSplitViewOrientation.Horizontal
            );
            splitView.style.flexGrow = 1; // 关键：让 SplitView 填满窗口
            rootVisualElement.Add(splitView);

            // =========================================================
            // 左侧栏 (Left Pane)
            // =========================================================
            VisualElement leftPane = new VisualElement();
            leftPane.style.minWidth = 200;
            splitView.Add(leftPane); // 第一个添加的是左栏

            // 工具栏
            Toolbar toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(RefreshFileList) { text = "Refresh" });
            toolbar.Add(
                new ToolbarButton(() => EditorUtility.RevealInFinder(_saveRoot))
                {
                    text = "Open Folder",
                }
            );
            leftPane.Add(toolbar);

            // 搜索框
            _searchField = new ToolbarSearchField();
            _searchField.RegisterValueChangedCallback(evt => FilterSlots(evt.newValue));
            _searchField.style.width = StyleKeyword.Auto;
            leftPane.Add(_searchField);

            // Slot 列表 (ListView) - 保持旧版本的UI风格
            _slotListView = new ListView();
            _slotListView.style.flexGrow = 1; // 关键：让列表填满剩余空间
            _slotListView.makeItem = () =>
            {
                Label label = new Label();
                label.style.paddingLeft = 5;
                label.style.paddingTop = 2;
                label.style.paddingBottom = 2;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                return label;
            };
            _slotListView.bindItem = (element, index) =>
            {
                Label label = (Label)element;
                label.text = _slots[index].Name;
                // 偶数行稍微变色，增加可读性
                // label.style.backgroundColor = index % 2 == 0 ? new Color(0,0,0,0) : new Color(1,1,1,0.03f);
            };
            _slotListView.selectionType = SelectionType.Single;
            _slotListView.selectionChanged += OnSlotSelected;
            _slotListView.fixedItemHeight = 20;
            leftPane.Add(_slotListView);

            // 状态栏
            _statusLabel = new Label("Ready");
            _statusLabel.style.fontSize = 10;
            _statusLabel.style.color = Color.gray;
            _statusLabel.style.paddingLeft = 5;
            _statusLabel.style.borderTopWidth = 1;
            _statusLabel.style.borderTopColor = new Color(0.15f, 0.15f, 0.15f);
            leftPane.Add(_statusLabel);

            // =========================================================
            // 右侧栏 (Right Pane)
            // =========================================================
            _rightPane = new VisualElement();
            _rightPane.style.flexGrow = 1;
            _rightPane.style.paddingTop = 10;
            _rightPane.style.paddingLeft = 15;
            _rightPane.style.paddingRight = 5;
            // 使用 Unity 默认背景色，避免太黑
            _rightPane.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            splitView.Add(_rightPane); // 第二个添加的是右栏

            // 详情头部
            VisualElement headerContainer = new VisualElement();
            headerContainer.style.borderBottomWidth = 1;
            headerContainer.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            headerContainer.style.marginBottom = 10;
            headerContainer.style.paddingBottom = 5;
            _rightPane.Add(headerContainer);

            _fileNameLabel = new Label("No Selection");
            _fileNameLabel.style.fontSize = 18;
            _fileNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _fileNameLabel.style.color = new Color(0.4f, 0.7f, 1.0f); // Asaki Blue
            headerContainer.Add(_fileNameLabel);

            _metaLabel = new Label("-");
            _metaLabel.style.fontSize = 11;
            _metaLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            headerContainer.Add(_metaLabel);

            // JSON 内容区域 (ScrollView)
            ScrollView scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            _rightPane.Add(scrollView);

            _contentLabel = new Label();
            _contentLabel.style.whiteSpace = WhiteSpace.Normal;
            _contentLabel.selection.isSelectable = true; // 允许复制
            // 尝试设置等宽字体 (Consolas / Menlo)
            _contentLabel.style.unityFont = Font.CreateDynamicFontFromOSFont("Consolas", 12);
            if (_contentLabel.style.unityFont.value == null)
                _contentLabel.style.unityFont = Font.CreateDynamicFontFromOSFont("Menlo", 12);

            _contentLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            scrollView.Add(_contentLabel);

            // 初始刷新
            RefreshFileList();
        }

        private void RefreshFileList()
        {
            if (!Directory.Exists(_saveRoot))
            {
                _slots.Clear();
                _slotListView.itemsSource = _slots;
                _slotListView.Rebuild();
                _statusLabel.text = "Save folder not found.";
                return;
            }

            // 扫描所有以 Slot_ 开头的文件夹 - 使用新版本的逻辑
            DirectoryInfo dir = new DirectoryInfo(_saveRoot);
            _slots = dir.GetDirectories("Slot_*").OrderByDescending(d => d.LastWriteTime).ToList();

            _slotListView.itemsSource = _slots;
            _slotListView.Rebuild();
            _statusLabel.text = $"{_slots.Count} save slots found.";

            // 恢复搜索
            if (_searchField != null && !string.IsNullOrEmpty(_searchField.value))
            {
                FilterSlots(_searchField.value);
            }
        }

        private void FilterSlots(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                _slotListView.itemsSource = _slots;
            }
            else
            {
                var filtered = _slots
                    .Where(s => s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                _slotListView.itemsSource = filtered;
            }
            _slotListView.Rebuild();
        }

        private void OnSlotSelected(IEnumerable<object> selection)
        {
            DirectoryInfo dirInfo = selection.FirstOrDefault() as DirectoryInfo;

            if (dirInfo == null)
            {
                _fileNameLabel.text = "No Selection";
                _metaLabel.text = "-";
                _contentLabel.text = "";
                return;
            }

            // 显示 Slot_X - 使用新版本的显示逻辑
            _fileNameLabel.text = dirInfo.Name;

            string metaPath = Path.Combine(dirInfo.FullName, "meta.json");
            string dataPath = Path.Combine(dirInfo.FullName, "data.bin");

            try
            {
                if (File.Exists(metaPath))
                {
                    string json = File.ReadAllText(metaPath);

                    // 保留旧版本的大文件保护逻辑
                    if (json.Length > 100 * 1024)
                        _contentLabel.text =
                            json.Substring(0, 100 * 1024) + "\n... <File too large> ...";
                    else
                        _contentLabel.text = json;

                    // 组合元数据信息 - 保留旧版本的格式化风格
                    string metaInfo = "";
                    if (File.Exists(dataPath))
                    {
                        FileInfo dataFile = new FileInfo(dataPath);
                        metaInfo = $"{FormatSize(dataFile.Length)}  |  ";
                    }
                    metaInfo += $"{dirInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
                    _metaLabel.text = metaInfo;
                }
                else
                {
                    _contentLabel.text = "<Meta file missing>";
                    _metaLabel.text = $"Folder  |  {dirInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
                }
            }
            catch (Exception e)
            {
                _contentLabel.text = $"<Error reading slot>: {e.Message}";
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }
    }
}
