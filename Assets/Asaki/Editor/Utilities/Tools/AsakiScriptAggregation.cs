// ================================================
// Unity Script Aggregator for AI Collaboration
// Version: 1.0
// Author: Unity Architecture Master
// ================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools
{
    // ============================================================================
    // 数据模型层
    // ============================================================================
    [Serializable]
    public class ScriptInfo
    {
        public string assetPath; // Assets/Scripts/Player.cs
        public string projectPath; // Project/Scripts/Player.cs (相对Assets)
        public long fileSize; // 字节数
        public DateTime modifiedTime;
        public string guid; // Unity GUID
        public bool isSelected; // UI选择状态

        public string FileName => Path.GetFileName(assetPath);
        public string Extension => Path.GetExtension(assetPath);
        public string ContentPreview { get; set; }
    }

    // ============================================================================
    // 配置管理（ScriptableObject）
    // ============================================================================
    public class ExportConfig : ScriptableObject
    {
        public string outputPath = "Assets/AI_Code.txt";
        public TemplateType templateType = TemplateType.Markdown;
        public bool includeGuid = true;
        public bool includeFileSize = true;
        public bool includeModifiedTime = true;
        public bool openAfterExport = true;
        public bool useRelativePath = true;
        public string[] excludedFolders = { "Library", "Temp", "Logs", "obj", "bin" };
        public string fileFilterPattern = "*.cs";

        public enum TemplateType
        {
            Markdown = 0,
            Xml = 1,
            Raw = 2,
        }

        private static ExportConfig _instance;
        public static ExportConfig Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                string path = "Assets/Editor/ScriptAggregatorConfig.asset";
                _instance = AssetDatabase.LoadAssetAtPath<ExportConfig>(path);
                if (_instance == null)
                {
                    _instance = CreateInstance<ExportConfig>();
                    Directory.CreateDirectory("Assets/Editor");
                    AssetDatabase.CreateAsset(_instance, path);
                    AssetDatabase.SaveAssets();
                }
                return _instance;
            }
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }

    // ============================================================================
    // 模板策略（策略模式）
    // ============================================================================
    public interface IExportTemplate
    {
        string Header(ScriptInfo info);
        string Content(string content);
        string Separator();
        string Footer();
        string FileExtension();
    }

    public class MarkdownTemplate : IExportTemplate
    {
        public string Header(ScriptInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<!-- File: {info.projectPath} -->");
            if (ExportConfig.Instance.includeGuid)
                sb.AppendLine($"<!-- GUID: {info.guid} -->");
            if (ExportConfig.Instance.includeFileSize)
                sb.AppendLine($"<!-- Size: {info.fileSize} bytes -->");
            if (ExportConfig.Instance.includeModifiedTime)
                sb.AppendLine($"<!-- Modified: {info.modifiedTime:yyyy-MM-dd HH:mm:ss} -->");
            sb.AppendLine("```csharp");
            return sb.ToString();
        }

        public string Content(string content)
        {
            return content;
        }

        public string Separator()
        {
            return "```\n\n";
        }

        public string Footer()
        {
            return "```\n";
        }

        public string FileExtension()
        {
            return ".md";
        }
    }

    public class XmlTemplate : IExportTemplate
    {
        public string Header(ScriptInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(
                $"<File Path=\"{SecurityElement.Escape(info.projectPath)}\" "
                    + $"GUID=\"{info.guid}\" "
                    + $"Size=\"{info.fileSize}\">"
            );
            sb.AppendLine("<Content><![CDATA[");
            return sb.ToString();
        }

        public string Content(string content)
        {
            return content;
        }

        public string Separator()
        {
            return "]]></Content></File>\n";
        }

        public string Footer()
        {
            return "";
        }

        public string FileExtension()
        {
            return ".xml";
        }
    }

    public class RawTemplate : IExportTemplate
    {
        public string Header(ScriptInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"/{'*'} {info.projectPath} {'*'}/");
            return sb.ToString();
        }

        public string Content(string content)
        {
            return content;
        }

        public string Separator()
        {
            return "\n\n";
        }

        public string Footer()
        {
            return "";
        }

        public string FileExtension()
        {
            return ".txt";
        }
    }

    // ============================================================================
    // 核心服务层（纯业务逻辑，无UI依赖）
    // ============================================================================
    public class ScriptAggregationService
    {
        private readonly ExportConfig _config;
        private readonly Dictionary<ExportConfig.TemplateType, IExportTemplate> _templates;

        public ScriptAggregationService(ExportConfig config)
        {
            _config = config;
            _templates = new Dictionary<ExportConfig.TemplateType, IExportTemplate>
            {
                { ExportConfig.TemplateType.Markdown, new MarkdownTemplate() },
                { ExportConfig.TemplateType.Xml, new XmlTemplate() },
                { ExportConfig.TemplateType.Raw, new RawTemplate() },
            };
        }

        // 扫描项目中的所有脚本
        public List<ScriptInfo> ScanAllScripts(string rootPath = null)
        {
            var scripts = new List<ScriptInfo>();
            string root = string.IsNullOrEmpty(rootPath)
                ? Application.dataPath
                : Path.GetFullPath(rootPath);

            try
            {
                var csFiles = Directory
                    .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !IsPathExcluded(path))
                    .Where(path => !Path.GetFileName(path).StartsWith("."));

                foreach (string filePath in csFiles)
                {
                    string assetPath =
                        "Assets"
                        + filePath.Substring(Application.dataPath.Length).Replace("\\", "/");
                    ScriptInfo info = CreateScriptInfo(assetPath);
                    if (info != null)
                        scripts.Add(info);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScriptAggregator] 扫描失败: {ex.Message}");
            }

            return scripts.OrderBy(s => s.projectPath).ToList();
        }

        private bool IsPathExcluded(string path)
        {
            return _config.excludedFolders.Any(excluded =>
                path.Contains($"/{excluded}/") || path.Contains($"\\{excluded}\\")
            );
        }

        private ScriptInfo CreateScriptInfo(string assetPath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(assetPath);
                return new ScriptInfo
                {
                    assetPath = assetPath,
                    projectPath = _config.useRelativePath
                        ? assetPath.Substring("Assets/".Length)
                        : assetPath,
                    fileSize = fileInfo.Length,
                    modifiedTime = fileInfo.LastWriteTime,
                    guid = AssetDatabase.AssetPathToGUID(assetPath),
                    isSelected = false,
                    ContentPreview = "",
                };
            }
            catch
            {
                return null;
            }
        }

        // 导出核心方法（支持进度回调）
        public async Task<bool> ExportAsync(
            List<ScriptInfo> selectedScripts,
            Action<float> onProgress = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (selectedScripts.Count == 0)
            {
                Debug.LogWarning("[ScriptAggregator] 未选择任何文件");
                return false;
            }

            IExportTemplate template = _templates[_config.templateType];
            string tempPath = _config.outputPath + ".tmp";
            long totalSize = selectedScripts.Sum(s => s.fileSize);
            long processedSize = 0L;

            try
            {
                using (
                    StreamWriter writer = new StreamWriter(tempPath, false, new UTF8Encoding(true))
                )
                {
                    // 写入头部
                    await writer.WriteLineAsync($"# Unity Scripts Aggregation");
                    await writer.WriteLineAsync($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    await writer.WriteLineAsync($"# Total Files: {selectedScripts.Count}");
                    await writer.WriteLineAsync($"# Total Size: {totalSize} bytes");
                    await writer.WriteLineAsync();

                    // 写入每个文件
                    for (int i = 0; i < selectedScripts.Count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            writer.Close();
                            File.Delete(tempPath);
                            return false;
                        }

                        ScriptInfo script = selectedScripts[i];
                        await writer.WriteAsync(template.Header(script));

                        // 读取文件内容
                        string content = await File.ReadAllTextAsync(script.assetPath);
                        await writer.WriteAsync(template.Content(content));
                        await writer.WriteAsync(template.Separator());

                        processedSize += script.fileSize;
                        onProgress?.Invoke((float)i / selectedScripts.Count);
                    }

                    await writer.WriteAsync(template.Footer());
                }

                // 原子替换
                File.Delete(_config.outputPath);
                File.Move(tempPath, _config.outputPath);

                AssetDatabase.Refresh();
                Debug.Log($"[ScriptAggregator] 导出成功: {_config.outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScriptAggregator] 导出失败: {ex.Message}");
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                return false;
            }
        }
    }

    // ============================================================================
    // UI层（EditorWindow）
    // ============================================================================
    public class ScriptAggregatorWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Script Aggregator";
        private const string PREFS_KEY = "ScriptAggregator_v1";
        private const string MENU_PATH = "Asaki/Tools/AI Collaboration/Script Aggregator";

        private Vector2 _leftScrollPos;
        private Vector2 _rightScrollPos;
        private List<ScriptInfo> _allScripts;
        private List<ScriptInfo> _filteredScripts;
        private string _searchText = "";
        private bool _isExporting = false;
        private float _exportProgress = 0f;
        private CancellationTokenSource _cancellationTokenSource;

        // 配置
        private ExportConfig _config;
        private ScriptAggregationService _service;

        // UI状态
        private GUIStyle _selectedStyle;
        private GUIStyle _normalStyle;
        private GUIStyle _searchFieldStyle;
        private bool _showConfig = true;

        // 选择统计
        private int _selectedCount = 0;
        private long _selectedSize = 0;

        [MenuItem(MENU_PATH, false, 1000)]
        public static void ShowWindow()
        {
            ScriptAggregatorWindow window = GetWindow<ScriptAggregatorWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(900f, 500f);
            window.Show();
        }

        [MenuItem(MENU_PATH + " (Export)", true)]
        private static bool ValidateQuickExport()
        {
            return !Application.isPlaying;
        }

        [MenuItem(MENU_PATH + " (Export)", false, 1001)]
        public static async void QuickExport()
        {
            ScriptAggregatorWindow window = GetWindow<ScriptAggregatorWindow>(WINDOW_TITLE);
            if (window._config == null)
                window.Initialize();

            // 全选所有脚本
            window.SelectAll();

            // 执行导出
            await window.ExportAsync();
        }

        private void OnEnable()
        {
            Initialize();
            LoadSelectionState();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SaveSelectionState();
            EditorApplication.update -= OnEditorUpdate;
            _cancellationTokenSource?.Cancel();
        }

        private void Initialize()
        {
            _config = ExportConfig.Instance;
            _service = new ScriptAggregationService(_config);
            _allScripts = _service.ScanAllScripts();
            _filteredScripts = new List<ScriptInfo>(_allScripts);

            CalculateSelectionStats();
        }

        private void OnEditorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            InitializeStyles();

            // 顶部工具栏
            DrawToolbar();

            // 主布局
            EditorGUILayout.BeginHorizontal();
            {
                // 左侧面板：文件列表
                DrawFileListPanel();

                // 右侧配置面板
                DrawConfigPanel();
            }
            EditorGUILayout.EndHorizontal();

            // 底部状态栏
            DrawStatusBar();

            // 进度条
            if (_isExporting)
            {
                DrawProgressBar();
            }

            // 快捷键处理
            HandleKeyboardEvents();
        }

        private void InitializeStyles()
        {
            if (_selectedStyle != null)
                return;

            _selectedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { background = CreateColorTexture(new Color(0.2f, 0.4f, 0.8f, 0.3f)) },
                padding = new RectOffset(4, 4, 2, 2),
            };

            _normalStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(4, 4, 2, 2),
            };

            _searchFieldStyle = new GUIStyle("SearchTextField");
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private async void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Initialize();
                }

                if (GUILayout.Button("全选", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SelectAll();
                }

                if (GUILayout.Button("反选", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    InvertSelection();
                }

                if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ClearSelection();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("导出", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    await ExportAsync();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFileListPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width * 0.6f));
            {
                // 搜索框
                EditorGUI.BeginChangeCheck();
                _searchText = EditorGUILayout.TextField(_searchText, _searchFieldStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    FilterScripts();
                }

                // 文件列表
                _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos);
                {
                    foreach (ScriptInfo script in _filteredScripts)
                    {
                        DrawScriptItem(script);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawScriptItem(ScriptInfo script)
        {
            EditorGUILayout.BeginHorizontal();
            {
                Rect rect = EditorGUILayout.GetControlRect();
                GUIContent content = new GUIContent(
                    $" {script.FileName}",
                    $"路径: {script.projectPath}\n大小: {script.fileSize} bytes\n修改: {script.modifiedTime}"
                );

                bool wasSelected = script.isSelected;
                bool isSelected = GUI.Toggle(
                    rect,
                    wasSelected,
                    content,
                    wasSelected ? _selectedStyle : _normalStyle
                );

                if (isSelected != wasSelected)
                {
                    script.isSelected = isSelected;
                    CalculateSelectionStats();
                }

                GUILayout.FlexibleSpace();

                // 文件大小标签
                string sizeLabel = $"{script.fileSize / 1024.0f:F1}KB";
                Rect sizeRect = GUILayoutUtility.GetRect(
                    new GUIContent(sizeLabel),
                    EditorStyles.label,
                    GUILayout.Width(60)
                );
                EditorGUI.LabelField(sizeRect, sizeLabel, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfigPanel()
        {
            _rightScrollPos = EditorGUILayout.BeginScrollView(
                _rightScrollPos,
                GUI.skin.box,
                GUILayout.Width(position.width * 0.4f)
            );
            {
                EditorGUILayout.LabelField("导出配置", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                // 输出路径
                EditorGUILayout.LabelField("输出路径:");
                EditorGUILayout.BeginHorizontal();
                {
                    _config.outputPath = EditorGUILayout.TextField(_config.outputPath);
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        string path = EditorUtility.SaveFilePanel(
                            "选择输出文件",
                            "Assets/",
                            "AI_Code",
                            "txt"
                        );
                        if (!string.IsNullOrEmpty(path))
                        {
                            _config.outputPath =
                                "Assets" + path.Substring(Application.dataPath.Length);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // 模板类型
                _config.templateType = (ExportConfig.TemplateType)
                    EditorGUILayout.EnumPopup("文件格式", _config.templateType);

                EditorGUILayout.Space();

                // 选项
                _config.includeGuid = EditorGUILayout.Toggle("包含GUID", _config.includeGuid);
                _config.includeFileSize = EditorGUILayout.Toggle(
                    "包含文件大小",
                    _config.includeFileSize
                );
                _config.includeModifiedTime = EditorGUILayout.Toggle(
                    "包含修改时间",
                    _config.includeModifiedTime
                );
                _config.useRelativePath = EditorGUILayout.Toggle(
                    "使用相对路径",
                    _config.useRelativePath
                );
                _config.openAfterExport = EditorGUILayout.Toggle(
                    "导出后打开",
                    _config.openAfterExport
                );

                EditorGUILayout.Space();

                // 排除文件夹
                EditorGUILayout.LabelField("排除文件夹 (逗号分隔):");
                string excluded = EditorGUILayout.TextArea(
                    string.Join(", ", _config.excludedFolders)
                );
                _config.excludedFolders = excluded.Split(',').Select(s => s.Trim()).ToArray();

                EditorGUILayout.Space();

                // 预览
                if (GUILayout.Button(_showConfig ? "▼ 隐藏预览" : "▶ 显示预览"))
                    _showConfig = !_showConfig;

                if (_showConfig && _selectedCount > 0)
                {
                    DrawPreview();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPreview()
        {
            ScriptInfo previewScript = _allScripts.FirstOrDefault(s => s.isSelected);
            if (previewScript == null)
                return;

            EditorGUILayout.LabelField("预览:", EditorStyles.boldLabel);
            string preview = File.ReadAllText(previewScript.assetPath, Encoding.UTF8);
            string previewText = preview.Length > 500 ? preview.Substring(0, 500) + "..." : preview;
            EditorGUILayout.HelpBox(previewText, MessageType.None);
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(30));
            {
                EditorGUILayout.BeginHorizontal();
                {
                    string status = _isExporting
                        ? $"正在导出... {_exportProgress:P0}"
                        : $"已选择 {_selectedCount} 文件 | 总计 {_selectedSize / 1024.0f:F1}KB";
                    EditorGUILayout.LabelField(status, EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

                    if (_isExporting && GUILayout.Button("取消", GUILayout.Width(60)))
                    {
                        _cancellationTokenSource?.Cancel();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawProgressBar()
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, _exportProgress, $"导出中... {_exportProgress:P0}");
        }

        private async void HandleKeyboardEvents()
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.E when evt.control:
                        await ExportAsync();
                        evt.Use();
                        break;
                    case KeyCode.A when evt.control:
                        SelectAll();
                        evt.Use();
                        break;
                    case KeyCode.Escape:
                        _cancellationTokenSource?.Cancel();
                        evt.Use();
                        break;
                }
            }
        }

        private void FilterScripts()
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredScripts = new List<ScriptInfo>(_allScripts);
            }
            else
            {
                _filteredScripts = _allScripts
                    .Where(s =>
                        s.FileName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0
                        || s.projectPath.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase)
                            >= 0
                    )
                    .ToList();
            }
        }

        private void SelectAll()
        {
            foreach (ScriptInfo script in _allScripts)
                script.isSelected = true;
            CalculateSelectionStats();
        }

        private void InvertSelection()
        {
            foreach (ScriptInfo script in _allScripts)
                script.isSelected = !script.isSelected;
            CalculateSelectionStats();
        }

        private void ClearSelection()
        {
            foreach (ScriptInfo script in _allScripts)
                script.isSelected = false;
            CalculateSelectionStats();
        }

        private void CalculateSelectionStats()
        {
            _selectedCount = _allScripts.Count(s => s.isSelected);
            _selectedSize = _allScripts.Where(s => s.isSelected).Sum(s => s.fileSize);
        }

        private async Task ExportAsync()
        {
            if (_isExporting)
                return;

            var selectedScripts = _allScripts.Where(s => s.isSelected).ToList();
            if (selectedScripts.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择要导出的脚本文件", "确定");
                return;
            }

            _isExporting = true;
            _exportProgress = 0f;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                bool success = await _service.ExportAsync(
                    selectedScripts,
                    progress => _exportProgress = progress,
                    _cancellationTokenSource.Token
                );

                if (success && _config.openAfterExport)
                {
                    Application.OpenURL(Path.GetFullPath(_config.outputPath));
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[ScriptAggregator] 导出已取消");
            }
            finally
            {
                _isExporting = false;
                _exportProgress = 0f;
            }
        }

        private void SaveSelectionState()
        {
            string[] selectedPaths = _allScripts
                .Where(s => s.isSelected)
                .Select(s => s.assetPath)
                .ToArray();
            EditorPrefs.SetString($"{PREFS_KEY}_Selected", string.Join(";", selectedPaths));
        }

        private void LoadSelectionState()
        {
            string saved = EditorPrefs.GetString($"{PREFS_KEY}_Selected", "");
            if (string.IsNullOrEmpty(saved))
                return;

            var selectedPaths = new HashSet<string>(saved.Split(';'));
            foreach (ScriptInfo script in _allScripts)
            {
                script.isSelected = selectedPaths.Contains(script.assetPath);
            }
            CalculateSelectionStats();
        }
    }

    // ============================================================================
    // 命令行模式（支持CI/CD）
    // ============================================================================
    public static class ScriptAggregatorCLI
    {
        public static void ExportFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outputPath = GetArgValue(args, "-output", "Assets/AI_Code.txt");
            string filter = GetArgValue(args, "-filter", "*.cs");

            ExportConfig config = ExportConfig.Instance;
            config.outputPath = outputPath;
            config.fileFilterPattern = filter;

            ScriptAggregationService service = new ScriptAggregationService(config);
            var allScripts = service.ScanAllScripts();

            Debug.Log($"[ScriptAggregatorCLI] 找到 {allScripts.Count} 个脚本文件");

            var task = service.ExportAsync(allScripts);
            task.Wait();

            Debug.Log($"[ScriptAggregatorCLI] 导出完成: {outputPath}");
            EditorApplication.Exit(task.Result ? 0 : 1);
        }

        private static string GetArgValue(string[] args, string key, string defaultValue)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == key)
                    return args[i + 1];
            }
            return defaultValue;
        }
    }
}
