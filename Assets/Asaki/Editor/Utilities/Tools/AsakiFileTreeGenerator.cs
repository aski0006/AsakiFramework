using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools
{
    public class AsakiFileTreeGenerator : EditorWindow
    {
        // 配置项键名（用于保存设置）
        private const string PREF_ROOT_PATH = "Asaki_Tree_RootPath";
        private const string PREF_OUTPUT_PATH = "Asaki_Tree_OutputPath";
        private const string PREF_EXTENSIONS = "Asaki_Tree_Extensions";
        private const string PREF_IGNORE_META = "Asaki_Tree_IgnoreMeta";
        private const string PREF_MAX_DEPTH = "Asaki_Tree_MaxDepth";
        private const string PREF_SHOW_SIZE = "Asaki_Tree_ShowSize";

        // 运行时变量
        private string _rootPath;
        private string _outputPath;
        private string _extensions = ".cs,.asmdef,.txt,.shader,.json,.xml"; // 默认白名单
        private bool _ignoreMeta = true;
        private int _maxDepth = 10;
        private bool _showSize = false;

        // 统计数据
        private int _statsDirCount;
        private int _statsFileCount;
        private long _statsTotalSize;

        [MenuItem("Asaki/Tools/File Tree Generator", false, 16)]
        public static void ShowWindow()
        {
            AsakiFileTreeGenerator window = GetWindow<AsakiFileTreeGenerator>("File Tree Gen");
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 加载配置
            _rootPath = EditorPrefs.GetString(PREF_ROOT_PATH, Application.dataPath + "/Asaki");
            _outputPath = EditorPrefs.GetString(
                PREF_OUTPUT_PATH,
                Application.dataPath + "/Asaki/file_tree.txt"
            );
            _extensions = EditorPrefs.GetString(PREF_EXTENSIONS, _extensions);
            _ignoreMeta = EditorPrefs.GetBool(PREF_IGNORE_META, true);
            _maxDepth = EditorPrefs.GetInt(PREF_MAX_DEPTH, 10);
            _showSize = EditorPrefs.GetBool(PREF_SHOW_SIZE, false);
        }

        private void OnDisable()
        {
            // 保存配置
            EditorPrefs.SetString(PREF_ROOT_PATH, _rootPath);
            EditorPrefs.SetString(PREF_OUTPUT_PATH, _outputPath);
            EditorPrefs.SetString(PREF_EXTENSIONS, _extensions);
            EditorPrefs.SetBool(PREF_IGNORE_META, _ignoreMeta);
            EditorPrefs.SetInt(PREF_MAX_DEPTH, _maxDepth);
            EditorPrefs.SetBool(PREF_SHOW_SIZE, _showSize);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Asaki Project Structure Scanner", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // 1. 扫描路径设置
            EditorGUILayout.LabelField("Scan Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _rootPath = EditorGUILayout.TextField("Root Path", _rootPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string path = EditorUtility.OpenFolderPanel(
                            "Select Root Folder",
                            _rootPath,
                            ""
                        );
                        if (!string.IsNullOrEmpty(path))
                            _rootPath = path;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _outputPath = EditorGUILayout.TextField("Output File", _outputPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string path = EditorUtility.SaveFilePanel(
                            "Save Tree To",
                            Path.GetDirectoryName(_outputPath),
                            "file_tree",
                            "txt"
                        );
                        if (!string.IsNullOrEmpty(path))
                            _outputPath = path;
                    }
                }
            }

            GUILayout.Space(5);

            // 2. 过滤设置
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _ignoreMeta = EditorGUILayout.Toggle("Ignore .meta Files", _ignoreMeta);
                _showSize = EditorGUILayout.Toggle("Show File Size", _showSize);
                _maxDepth = EditorGUILayout.IntSlider("Max Depth", _maxDepth, 1, 20);

                GUILayout.Space(2);
                EditorGUILayout.LabelField("Include Extensions (comma separated, empty for all):");
                _extensions = EditorGUILayout.TextField(_extensions);
            }

            GUILayout.Space(15);

            // 3. 生成按钮
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Generate File Tree", GUILayout.Height(40)))
            {
                GenerateTree();
            }
            GUI.backgroundColor = Color.white;

            // 4. 打开文件按钮
            if (File.Exists(_outputPath))
            {
                GUILayout.Space(5);
                if (GUILayout.Button("Open Generated File"))
                {
                    Application.OpenURL("file://" + _outputPath);
                }
            }
        }

        private void GenerateTree()
        {
            if (!Directory.Exists(_rootPath))
            {
                EditorUtility.DisplayDialog("Error", $"Directory not found:\n{_rootPath}", "OK");
                return;
            }

            _statsDirCount = 0;
            _statsFileCount = 0;
            _statsTotalSize = 0;

            StringBuilder sb = new StringBuilder();

            // 头部信息
            sb.AppendLine(new string('=', 60));
            sb.AppendLine($"Directory Tree: {_rootPath}");
            sb.AppendLine($"Generated Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();
            sb.AppendLine($"{Path.GetFileName(_rootPath)}/");

            // 递归生成
            var validExtensions = _extensions
                .Split(',')
                .Select(e => e.Trim().ToLower())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            Traverse(new DirectoryInfo(_rootPath), "", true, 0, sb, validExtensions);

            // 统计信息
            sb.AppendLine();
            sb.AppendLine(new string('=', 60));
            sb.AppendLine($"Stats: {_statsDirCount} directories, {_statsFileCount} files");
            sb.AppendLine($"Total Size: {EditorUtility.FormatBytes(_statsTotalSize)}");
            if (_ignoreMeta)
                sb.AppendLine("Ignored: .meta files");
            if (validExtensions.Count > 0)
                sb.AppendLine($"Whitelist: {string.Join(", ", validExtensions)}");
            sb.AppendLine(new string('=', 60));

            try
            {
                // 确保输出目录存在
                string outDir = Path.GetDirectoryName(_outputPath);
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                File.WriteAllText(_outputPath, sb.ToString(), Encoding.UTF8);
                Debug.Log($"<color=#44FFaa>[Asaki]</color> File tree generated at: {_outputPath}");
                AssetDatabase.Refresh(); // 刷新资源，如果文件在Assets目录下
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Asaki] Failed to write file tree: {e.Message}");
            }
        }

        private void Traverse(
            DirectoryInfo dir,
            string prefix,
            bool isLast,
            int currentDepth,
            StringBuilder sb,
            List<string> validExtensions
        )
        {
            if (currentDepth >= _maxDepth)
                return;

            try
            {
                // 获取所有子目录和文件
                var dirs = dir.GetDirectories();
                var files = dir.GetFiles();

                // 过滤隐藏文件夹
                dirs = dirs.Where(d => !d.Name.StartsWith(".")).ToArray();

                // 过滤文件
                files = files
                    .Where(f => !f.Name.StartsWith(".") && ShouldIncludeFile(f, validExtensions))
                    .ToArray();

                // 排序：文件夹优先，然后按名称排序 (复刻 Python 逻辑)
                var allItems = new List<FileSystemInfo>();
                allItems.AddRange(dirs.OrderBy(d => d.Name));
                allItems.AddRange(files.OrderBy(f => f.Name));

                int count = allItems.Count;
                for (int i = 0; i < count; i++)
                {
                    FileSystemInfo item = allItems[i];
                    bool isLastItem = i == count - 1;

                    // 构建前缀
                    string connector = isLastItem ? "└── " : "├── ";
                    string childPrefix = prefix + (isLastItem ? "    " : "│   ");

                    if (item is DirectoryInfo dInfo)
                    {
                        _statsDirCount++;
                        sb.AppendLine($"{prefix}{connector}{dInfo.Name}/");
                        Traverse(
                            dInfo,
                            childPrefix,
                            isLastItem,
                            currentDepth + 1,
                            sb,
                            validExtensions
                        );
                    }
                    else if (item is FileInfo fInfo)
                    {
                        _statsFileCount++;
                        _statsTotalSize += fInfo.Length;

                        string sizeStr = _showSize
                            ? $" ({EditorUtility.FormatBytes(fInfo.Length)})"
                            : "";
                        sb.AppendLine($"{prefix}{connector}{fInfo.Name}{sizeStr}");
                    }
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                sb.AppendLine($"{prefix}└── [ACCESS DENIED]");
            }
        }

        private bool ShouldIncludeFile(FileInfo file, List<string> validExtensions)
        {
            if (_ignoreMeta && file.Extension.ToLower() == ".meta")
                return false;

            // 如果没有设置白名单，则包含所有（除了被忽略的）
            if (validExtensions == null || validExtensions.Count == 0)
                return true;

            // 检查白名单
            return validExtensions.Contains(file.Extension.ToLower());
        }
    }
}
