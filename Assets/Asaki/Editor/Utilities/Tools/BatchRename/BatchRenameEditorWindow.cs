using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Editor.Utilities.Tools.BatchRename;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.UI
{
    public class BatchRenameEditorWindow : EditorWindow
    {
        // ==================== 工业级配置 ====================

        [SerializeField]
        private string _prefix = "";

        [SerializeField]
        private string _suffix = "";

        [SerializeField]
        private string _find = "";

        [SerializeField]
        private string _replace = "";

        [SerializeField]
        private bool _useRegex = false;

        [SerializeField]
        private bool _caseSensitive = true;

        [SerializeField]
        private bool _addSerialNumber = false;

        [SerializeField]
        private int _serialStart = 1;

        [SerializeField]
        private int _serialStep = 1;

        [SerializeField]
        private int _serialPadding = 3;

        [SerializeField]
        private SerialPosition _serialPosition = SerialPosition.Suffix;

        [SerializeField]
        private CaseConversion _caseConversion = CaseConversion.None;

        [SerializeField]
        private string _removeChars = "";

        [SerializeField]
        private int _keepLastNChars = 0;

        [SerializeField]
        private string _template = "{name}_{index:000}";

        // 预设管理
        [SerializeField]
        private List<RenamePreset> _presets = new List<RenamePreset>();
        private int _selectedPresetIndex = -1;
        private const string PRESET_SAVE_KEY = "Asaki_BatchRename_Presets";

        // 过滤
        [SerializeField]
        private string _nameFilter = "";

        [SerializeField]
        private ComponentFilter _componentFilter = ComponentFilter.None;

        // 状态
        private RenameOperation[] _previewOps = new RenameOperation[0];
        private Dictionary<int, ConflictType> _conflicts = new Dictionary<int, ConflictType>();
        private Vector2 _scrollPos;
        private bool _showPreview = true;
        private bool _showAdvanced = false;
        private bool _stylesInitialized;

        // GUIStyle缓存
        private GUIStyle _conflictStyle,
            _modifiedStyle,
            _unchangedStyle,
            _infoStyle;

        // ==================== 枚举定义 ====================

        public enum SerialPosition
        {
            Prefix,
            Suffix,
            Replace,
        }

        public enum CaseConversion
        {
            None,
            Upper,
            Lower,
            TitleCase,
            PascalCase,
            CamelCase,
        }

        public enum ComponentFilter
        {
            None,
            RectTransform,
            Renderer,
            Collider,
            Custom,
        }

        // ==================== 窗口入口 ====================

        [MenuItem("Asaki/Tools/Batch Rename &F2", false, 11)]
        public static void ShowWindow()
        {
            BatchRenameEditorWindow window = GetWindow<BatchRenameEditorWindow>("Batch Rename Pro");
            window.minSize = new Vector2(550, 650);
            window.LoadPresets();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPreview();
        }

        private void OnDisable()
        {
            SavePresets();
        }

        // ==================== GUI绘制 ====================

        private void OnGUI()
        {
            if (!_stylesInitialized)
                InitializeStyles();

            EditorGUILayout.BeginVertical(Styles.MainContainer);

            DrawSelectionPanel();
            DrawBasicPanel();
            DrawAdvancedPanel();
            DrawFilterPanel();
            DrawPreviewPanel();
            DrawActionPanel();

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectionPanel()
        {
            EditorGUILayout.LabelField("📋 Selection", Styles.SectionTitle);
            int total = Selection.gameObjects.Length;
            int filtered = GetFilteredTargets().Length;

            if (total == 0)
                EditorGUILayout.HelpBox("❌ No objects selected", MessageType.Error);
            else if (filtered == 0)
                EditorGUILayout.HelpBox("⚠️ All objects filtered out", MessageType.Warning);
            else
                EditorGUILayout.LabelField(
                    $"Selected: {total} | After Filter: {filtered}",
                    Styles.InfoBox
                );

            if (GUILayout.Button("Refresh Selection", GUILayout.Height(25)))
                RefreshPreview();
        }

        private void DrawBasicPanel()
        {
            EditorGUILayout.LabelField("📝 Basic Rules", Styles.SectionTitle);

            // 前缀/后缀
            _prefix = EditorGUILayout.TextField("Prefix", _prefix);
            _suffix = EditorGUILayout.TextField("Suffix", _suffix);

            // 模板系统
            EditorGUILayout.LabelField(
                "Template Variables: {name}, {index}, {date}",
                EditorStyles.miniLabel
            );
            _template = EditorGUILayout.TextField("Template", _template);

            // 序列号
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _addSerialNumber = EditorGUILayout.Toggle("Add Serial Number", _addSerialNumber);
            if (_addSerialNumber)
            {
                EditorGUI.indentLevel++;
                _serialStart = EditorGUILayout.IntField("Start", _serialStart);
                _serialStep = EditorGUILayout.IntField("Step", _serialStep);
                _serialPadding = EditorGUILayout.IntField("Padding", Mathf.Max(1, _serialPadding));
                _serialPosition = (SerialPosition)
                    EditorGUILayout.EnumPopup("Position", _serialPosition);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedPanel()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "⚙️ Advanced Options", true);
            if (!_showAdvanced)
                return;

            // 查找替换
            EditorGUILayout.LabelField("Find & Replace", Styles.SubSectionTitle);
            _find = EditorGUILayout.TextField("Find", _find);
            _replace = EditorGUILayout.TextField("Replace", _replace);
            _useRegex = EditorGUILayout.Toggle("Use Regex", _useRegex);
            _caseSensitive = EditorGUILayout.Toggle("Case Sensitive", _caseSensitive);

            // 字符操作
            EditorGUILayout.LabelField("Character Operations", Styles.SubSectionTitle);
            _removeChars = EditorGUILayout.TextField("Remove Chars", _removeChars);
            _keepLastNChars = EditorGUILayout.IntField("Keep Last N Chars", _keepLastNChars);

            // 大小写转换
            EditorGUILayout.LabelField("Case Conversion", Styles.SubSectionTitle);
            _caseConversion = (CaseConversion)
                EditorGUILayout.EnumPopup("Conversion", _caseConversion);

            // 预设管理
            EditorGUILayout.LabelField("Presets", Styles.SubSectionTitle);
            DrawPresetManager();
        }

        private void DrawFilterPanel()
        {
            EditorGUILayout.LabelField("🔍 Filters", Styles.SectionTitle);

            // 名称过滤
            EditorGUILayout.BeginHorizontal();
            _nameFilter = EditorGUILayout.TextField(
                "Name Filter",
                _nameFilter,
                GUI.skin.FindStyle("SearchTextField")
            );
            if (GUILayout.Button("", GUI.skin.FindStyle("SearchCancelButton")))
            {
                _nameFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // 组件过滤
            _componentFilter = (ComponentFilter)
                EditorGUILayout.EnumPopup("Component Filter", _componentFilter);
        }

        private void DrawPreviewPanel()
        {
            EditorGUILayout.LabelField("👁️ Preview", Styles.SectionTitle);

            _showPreview = EditorGUILayout.Toggle("Show Preview", _showPreview);
            if (!_showPreview || _previewOps.Length == 0)
                return;

            // 统计信息
            int modifiedCount = _previewOps.Count(op => op.OriginalName != op.NewName);
            EditorGUILayout.LabelField(
                $"Total: {_previewOps.Length} | Modified: {modifiedCount} | Conflicts: {_conflicts.Count}",
                Styles.InfoBox
            );

            // 滚动视图
            using EditorGUILayout.ScrollViewScope scrollScope = new EditorGUILayout.ScrollViewScope(
                _scrollPos,
                Styles.PreviewScroll
            );
            _scrollPos = scrollScope.scrollPosition;

            // 预览列表
            EditorGUILayout.BeginVertical(Styles.PreviewBox);
            for (int i = 0; i < _previewOps.Length; i++)
            {
                DrawPreviewItem(_previewOps[i], i);
                if (i < _previewOps.Length - 1)
                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionPanel()
        {
            EditorGUILayout.BeginHorizontal();

            bool canApply = _conflicts.Count == 0 && _previewOps.Length > 0;
            using (new EditorGUI.DisabledScope(!canApply))
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                if (GUILayout.Button("✅ Apply", Styles.ApplyButton, GUILayout.Height(32)))
                    OnApply();
                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button("❌ Cancel", Styles.CancelButton, GUILayout.Height(32)))
                Close();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPresetManager()
        {
            EditorGUILayout.BeginHorizontal();

            // 预设下拉
            string[] presetNames = _presets.Select(p => p.Name).Prepend("Load Preset...").ToArray();
            _selectedPresetIndex = EditorGUILayout.Popup(_selectedPresetIndex + 1, presetNames) - 1;

            if (_selectedPresetIndex >= 0 && GUILayout.Button("Load", GUILayout.Width(60)))
                LoadPreset(_presets[_selectedPresetIndex]);

            EditorGUILayout.EndHorizontal();

            // 保存/删除
            EditorGUILayout.BeginHorizontal();
            _newPresetName = EditorGUILayout.TextField("Preset Name", _newPresetName);

            if (
                GUILayout.Button("Save", GUILayout.Width(60))
                && !string.IsNullOrWhiteSpace(_newPresetName)
            )
                SaveCurrentPreset();

            if (GUILayout.Button("Delete", GUILayout.Width(60)) && _selectedPresetIndex >= 0)
                DeletePreset(_selectedPresetIndex);

            EditorGUILayout.EndHorizontal();
        }

        // ==================== 逻辑实现 ====================

        private void RefreshPreview()
        {
            var targets = GetFilteredTargets();
            IBatchRenameStrategy strategy = CreateCompositeStrategy();
            _previewOps = strategy.GeneratePreview(targets);
            _conflicts = BatchRenameService.DetectConflicts(_previewOps);
            Repaint();
        }

        private IBatchRenameStrategy CreateCompositeStrategy()
        {
            var strategies = new List<IBatchRenameStrategy>();

            // 1. 查找替换策略
            if (!string.IsNullOrEmpty(_find))
            {
                strategies.Add(new FindReplaceStrategy(_find, _replace, _useRegex, _caseSensitive));
            }

            // 2. 字符移除策略
            if (!string.IsNullOrEmpty(_removeChars))
            {
                strategies.Add(new RemoveCharsStrategy(_removeChars));
            }

            // 3. 保留最后N字符策略
            if (_keepLastNChars > 0)
            {
                strategies.Add(new KeepLastNCharsStrategy(_keepLastNChars));
            }

            // 4. 大小写转换策略
            if (_caseConversion != CaseConversion.None)
            {
                strategies.Add(new CaseConversionStrategy(_caseConversion));
            }

            // 5. 模板策略（必须最后执行，因为它包含index变量）
            strategies.Add(
                new TemplateStrategy(
                    _template,
                    _prefix,
                    _suffix,
                    _addSerialNumber,
                    _serialStart,
                    _serialStep,
                    _serialPadding,
                    _serialPosition
                )
            );

            return new CompositeStrategy(strategies);
        }

        private GameObject[] GetFilteredTargets()
        {
            var all = Selection.gameObjects;
            if (string.IsNullOrEmpty(_nameFilter) && _componentFilter == ComponentFilter.None)
                return all;

            return all.Where(go =>
                    (string.IsNullOrEmpty(_nameFilter) || go.name.Contains(_nameFilter))
                    && (_componentFilter == ComponentFilter.None || HasComponentFilter(go))
                )
                .ToArray();
        }

        private bool HasComponentFilter(GameObject go)
        {
            return _componentFilter switch
            {
                ComponentFilter.RectTransform => go.GetComponent<RectTransform>() != null,
                ComponentFilter.Renderer => go.GetComponent<Renderer>() != null,
                ComponentFilter.Collider => go.GetComponent<Collider>() != null,
                _ => false,
            };
        }

        private void OnApply()
        {
            if (_previewOps.Length == 0)
                return;

            using UndoScope undoScope = new UndoScope($"Batch Rename {_previewOps.Length} objects");
            foreach (RenameOperation op in _previewOps)
            {
                op.Apply();
            }
            Close();
        }

        private void OnSelectionChange()
        {
            RefreshPreview();
        }

        // ==================== 预设管理 ====================

        private string _newPresetName = "";

        private void LoadPresets()
        {
            string json = EditorPrefs.GetString(PRESET_SAVE_KEY, "[]");
            _presets = JsonUtility.FromJson<PresetList>(json)?.Presets ?? new List<RenamePreset>();
        }

        private void SavePresets()
        {
            PresetList wrapper = new PresetList { Presets = _presets };
            EditorPrefs.SetString(PRESET_SAVE_KEY, JsonUtility.ToJson(wrapper));
        }

        private void LoadPreset(RenamePreset preset)
        {
            _prefix = preset.Prefix;
            _suffix = preset.Suffix;
            _find = preset.Find;
            _replace = preset.Replace;
            _useRegex = preset.UseRegex;
            _caseSensitive = preset.CaseSensitive;
            _addSerialNumber = preset.AddSerialNumber;
            _serialStart = preset.SerialStart;
            _serialStep = preset.SerialStep;
            _serialPadding = preset.SerialPadding;
            _serialPosition = preset.SerialPosition;
            _caseConversion = preset.CaseConversion;
            _removeChars = preset.RemoveChars;
            _keepLastNChars = preset.KeepLastNChars;
            _template = preset.Template;
            RefreshPreview();
        }

        private void SaveCurrentPreset()
        {
            RenamePreset preset = new RenamePreset
            {
                Name = _newPresetName,
                Prefix = _prefix,
                Suffix = _suffix,
                Find = _find,
                Replace = _replace,
                UseRegex = _useRegex,
                CaseSensitive = _caseSensitive,
                AddSerialNumber = _addSerialNumber,
                SerialStart = _serialStart,
                SerialStep = _serialStep,
                SerialPadding = _serialPadding,
                SerialPosition = _serialPosition,
                CaseConversion = _caseConversion,
                RemoveChars = _removeChars,
                KeepLastNChars = _keepLastNChars,
                Template = _template,
            };

            int existingIndex = _presets.FindIndex(p => p.Name == _newPresetName);
            if (existingIndex >= 0)
            {
                _presets[existingIndex] = preset;
                _selectedPresetIndex = existingIndex;
            }
            else
            {
                _presets.Add(preset);
                _selectedPresetIndex = _presets.Count - 1;
            }

            _newPresetName = "";
            SavePresets();
        }

        private void DeletePreset(int index)
        {
            if (index >= 0 && index < _presets.Count)
            {
                _presets.RemoveAt(index);
                _selectedPresetIndex = -1;
                SavePresets();
            }
        }

        // ==================== GUI辅助 ====================

        private void InitializeStyles()
        {
            _conflictStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
            };
            _modifiedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0f, 0.8f, 0f) },
            };
            _unchangedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.gray },
            };
            _infoStyle = new GUIStyle(EditorStyles.helpBox) { richText = true };
            _stylesInitialized = true;
        }

        private void DrawPreviewItem(RenameOperation op, int index)
        {
            ConflictType conflictType = _conflicts.GetValueOrDefault(
                op.InstanceId,
                ConflictType.None
            );
            GUIStyle style = GetPreviewStyle(op, conflictType);

            EditorGUILayout.BeginHorizontal(Styles.PreviewItem);

            // 索引
            EditorGUILayout.LabelField($"#{index + 1}", Styles.IndexLabel, GUILayout.Width(40f));

            // 图标
            EditorGUILayout.LabelField(
                EditorGUIUtility.IconContent("GameObject Icon"),
                GUILayout.Width(20f),
                GUILayout.Height(20f)
            );

            // 原名称
            GUIContent original = new GUIContent(op.OriginalName, "Original Name");
            EditorGUILayout.LabelField(original, style, GUILayout.Width(180f));

            // 箭头
            EditorGUILayout.LabelField("→", Styles.ArrowLabel, GUILayout.Width(20f));

            // 新名称
            GUIContent newNameContent = new GUIContent(op.NewName, "New Name");
            if (conflictType != ConflictType.None)
                newNameContent.tooltip = $"Conflict: {conflictType}";

            EditorGUILayout.LabelField(newNameContent, style, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
        }

        private GUIStyle GetPreviewStyle(RenameOperation op, ConflictType conflict)
        {
            if (conflict != ConflictType.None)
                return _conflictStyle;
            if (op.OriginalName != op.NewName)
                return _modifiedStyle;
            return _unchangedStyle;
        }

        // ==================== 静态样式 ====================

        private static class Styles
        {
            public static readonly GUIStyle MainContainer = new GUIStyle
            {
                padding = new RectOffset(8, 8, 8, 8),
            };
            public static readonly GUIStyle SectionTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 8, 8),
            };
            public static readonly GUIStyle SubSectionTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                margin = new RectOffset(0, 0, 4, 4),
            };
            public static readonly GUIStyle PreviewItem = new GUIStyle
            {
                margin = new RectOffset(0, 0, 2, 2),
            };
            public static readonly GUIStyle IndexLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.gray },
            };
            public static readonly GUIStyle ArrowLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
            };
            public static readonly GUIStyle InfoBox = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(4, 4, 4, 4),
            };
            public static readonly GUIStyle ApplyButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
            };
            public static readonly GUIStyle CancelButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
            };
            public static readonly GUIStyle PreviewScroll = new GUIStyle
            {
                padding = new RectOffset(2, 2, 2, 2),
            };
            public static readonly GUIStyle PreviewBox = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(4, 4, 4, 4),
            };
        }
    }

    // ==================== 数据模型 ====================

    [Serializable]
    public class RenamePreset
    {
        public string Name;
        public string Prefix;
        public string Suffix;
        public string Find;
        public string Replace;
        public bool UseRegex;
        public bool CaseSensitive;
        public bool AddSerialNumber;
        public int SerialStart;
        public int SerialStep;
        public int SerialPadding;
        public BatchRenameEditorWindow.SerialPosition SerialPosition;
        public BatchRenameEditorWindow.CaseConversion CaseConversion;
        public string RemoveChars;
        public int KeepLastNChars;
        public string Template;
    }

    [Serializable]
    public class PresetList
    {
        public List<RenamePreset> Presets = new List<RenamePreset>();
    }

    // ==================== 冲突类型 ====================

    public enum ConflictType
    {
        None,
        DuplicateName,
        InvalidCharacters,
        NameTooLong,
    }
}
