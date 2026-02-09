using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Asaki.Plungin.ComboSystem.Editor
{
    /// <summary>
    /// ComboTree导出工具窗口
    /// 提供可视化导出界面和批量导出功能
    /// </summary>
    public class ComboTreeExporterWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private ComboGraphAsset _selectedGraph;
        private List<ComboGraphAsset> _allGraphs = new List<ComboGraphAsset>();
        private bool[] _selectedFlags;

        // 导出设置
        private string _outputFolder = "Assets/Game/Configs/Combos";
        private bool _overwriteExisting = true;
        private bool _pingAfterExport = true;
        private bool _validateBeforeExport = true;
        private ExportMode _exportMode = ExportMode.SelectedOnly;

        private enum ExportMode
        {
            SelectedOnly,
            AllInProject,
            MultipleSelection,
        }

        [MenuItem("Asaki/ComboSystem/导出ComboTree", priority = 22)]
        public static void ShowWindow()
        {
            var window = GetWindow<ComboTreeExporterWindow>("导出 ComboTree");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshGraphList();
        }

        private void RefreshGraphList()
        {
            _allGraphs = AssetDatabase
                .FindAssets("t:ComboGraphAsset")
                .Select(guid =>
                    AssetDatabase.LoadAssetAtPath<ComboGraphAsset>(
                        AssetDatabase.GUIDToAssetPath(guid)
                    )
                )
                .Where(g => g != null)
                .ToList();

            _selectedFlags = new bool[_allGraphs.Count];
            for (int i = 0; i < _selectedFlags.Length; i++)
            {
                _selectedFlags[i] = _allGraphs[i] == _selectedGraph;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("ComboTree 导出工具", EditorStyles.largeLabel);
            EditorGUILayout.Space(10);

            // 导出模式选择
            EditorGUILayout.LabelField("导出模式", EditorStyles.boldLabel);
            _exportMode = (ExportMode)EditorGUILayout.EnumPopup("导出模式", _exportMode);

            EditorGUILayout.Space(10);

            // 根据模式显示不同内容
            switch (_exportMode)
            {
                case ExportMode.SelectedOnly:
                    DrawSelectedOnlyMode();
                    break;
                case ExportMode.AllInProject:
                    DrawAllInProjectMode();
                    break;
                case ExportMode.MultipleSelection:
                    DrawMultipleSelectionMode();
                    break;
            }

            EditorGUILayout.Space(10);

            // 导出设置
            EditorGUILayout.LabelField("导出设置", EditorStyles.boldLabel);
            DrawExportSettings();

            EditorGUILayout.Space(10);

            // 导出按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("开始导出", GUILayout.Width(120), GUILayout.Height(30)))
            {
                ExportComboTrees();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 帮助信息
            EditorGUILayout.HelpBox(
                "导出说明:\n"
                    + "1. ComboGraph 是编辑器中使用的可视化图表\n"
                    + "2. ComboTree 是运行时使用的轻量级数据资产\n"
                    + "3. 导出后会生成 .asset 文件，可直接用于 AsakiComboController",
                MessageType.Info
            );
        }

        /// <summary>
        /// 绘制单个选择模式
        /// </summary>
        private void DrawSelectedOnlyMode()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("选择图表");
            _selectedGraph =
                EditorGUILayout.ObjectField(_selectedGraph, typeof(ComboGraphAsset), false)
                as ComboGraphAsset;
            EditorGUILayout.EndHorizontal();

            if (_selectedGraph != null)
            {
                EditorGUILayout.Space(5);
                DrawGraphPreview(_selectedGraph);
            }
            else
            {
                EditorGUILayout.HelpBox("请选择一个 ComboGraph 资产进行导出", MessageType.Warning);
            }
        }

        /// <summary>
        /// 绘制全部导出模式
        /// </summary>
        private void DrawAllInProjectMode()
        {
            EditorGUILayout.LabelField(
                $"项目中共找到 {_allGraphs.Count} 个 ComboGraph",
                EditorStyles.helpBox
            );

            if (_allGraphs.Count == 0)
            {
                EditorGUILayout.HelpBox("项目中没有找到任何 ComboGraph 资产", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.Height(150)
            );
            foreach (var graph in _allGraphs)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(graph.name, GUILayout.Width(200));
                EditorGUILayout.LabelField($"{graph.Nodes.Count} 节点", GUILayout.Width(80));
                if (GUILayout.Button("预览", GUILayout.Width(60)))
                {
                    Selection.activeObject = graph;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制多选模式
        /// </summary>
        private void DrawMultipleSelectionMode()
        {
            EditorGUILayout.LabelField(
                $"项目中共找到 {_allGraphs.Count} 个 ComboGraph",
                EditorStyles.helpBox
            );

            if (_allGraphs.Count == 0)
            {
                EditorGUILayout.HelpBox("项目中没有找到任何 ComboGraph 资产", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选"))
            {
                for (int i = 0; i < _selectedFlags.Length; i++)
                    _selectedFlags[i] = true;
            }
            if (GUILayout.Button("全不选"))
            {
                for (int i = 0; i < _selectedFlags.Length; i++)
                    _selectedFlags[i] = false;
            }
            if (GUILayout.Button("刷新列表"))
            {
                RefreshGraphList();
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.Height(150)
            );
            for (int i = 0; i < _allGraphs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                _selectedFlags[i] = EditorGUILayout.Toggle(_selectedFlags[i], GUILayout.Width(20));
                EditorGUILayout.LabelField(_allGraphs[i].name, GUILayout.Width(180));
                EditorGUILayout.LabelField(
                    $"{_allGraphs[i].Nodes.Count} 节点",
                    GUILayout.Width(80)
                );
                if (GUILayout.Button("预览", GUILayout.Width(60)))
                {
                    Selection.activeObject = _allGraphs[i];
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            int selectedCount = _selectedFlags?.Count(f => f) ?? 0;
            EditorGUILayout.LabelField($"已选择: {selectedCount} 个", EditorStyles.miniLabel);
        }

        /// <summary>
        /// 绘制导出设置
        /// </summary>
        private void DrawExportSettings()
        {
            // 输出文件夹
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("输出文件夹");
            _outputFolder = EditorGUILayout.TextField(_outputFolder);
            if (GUILayout.Button("浏览...", GUILayout.Width(80)))
            {
                string selected = EditorUtility.OpenFolderPanel("选择输出文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // 转换为相对路径
                    if (selected.StartsWith(Application.dataPath))
                    {
                        _outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // 其他选项
            _overwriteExisting = EditorGUILayout.Toggle("覆盖已有文件", _overwriteExisting);
            _pingAfterExport = EditorGUILayout.Toggle("导出后高亮显示", _pingAfterExport);
            _validateBeforeExport = EditorGUILayout.Toggle("导出前验证图表", _validateBeforeExport);
        }

        /// <summary>
        /// 绘制图表预览
        /// </summary>
        private void DrawGraphPreview(ComboGraphAsset graph)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("图表信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {graph.ComboTreeId}");
            EditorGUILayout.LabelField($"描述: {graph.Description}");
            EditorGUILayout.LabelField($"节点数: {graph.Nodes.Count}");
            EditorGUILayout.LabelField($"边数: {graph.Edges.Count}");

            var moveCount = graph.Nodes.OfType<MoveNode>().Count();
            var transitionCount = graph.Nodes.OfType<TransitionNode>().Count();
            EditorGUILayout.LabelField($"招式节点: {moveCount}, 转换节点: {transitionCount}");

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 执行导出
        /// </summary>
        private void ExportComboTrees()
        {
            // 获取要导出的图表列表
            List<ComboGraphAsset> graphsToExport = new List<ComboGraphAsset>();

            switch (_exportMode)
            {
                case ExportMode.SelectedOnly:
                    if (_selectedGraph != null)
                        graphsToExport.Add(_selectedGraph);
                    break;
                case ExportMode.AllInProject:
                    graphsToExport.AddRange(_allGraphs);
                    break;
                case ExportMode.MultipleSelection:
                    for (int i = 0; i < _allGraphs.Count; i++)
                    {
                        if (_selectedFlags[i])
                            graphsToExport.Add(_allGraphs[i]);
                    }
                    break;
            }

            if (graphsToExport.Count == 0)
            {
                EditorUtility.DisplayDialog("导出失败", "没有选择任何要导出的图表", "确定");
                return;
            }

            // 确保输出目录存在
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            // 导出每个图表
            int successCount = 0;
            int failCount = 0;
            var exportResults = new List<string>();

            for (int i = 0; i < graphsToExport.Count; i++)
            {
                var graph = graphsToExport[i];
                EditorUtility.DisplayProgressBar(
                    "导出 ComboTree",
                    $"正在导出: {graph.name}",
                    (float)i / graphsToExport.Count
                );

                try
                {
                    // 验证图表
                    if (_validateBeforeExport)
                    {
                        var validation = ValidateGraph(graph);
                        if (validation.HasErrors)
                        {
                            exportResults.Add($"[失败] {graph.name}: {validation.ErrorMessage}");
                            failCount++;
                            continue;
                        }
                    }

                    // 导出
                    string result = ExportSingleGraph(graph);
                    exportResults.Add($"[成功] {graph.name} -> {result}");
                    successCount++;
                }
                catch (Exception e)
                {
                    exportResults.Add($"[失败] {graph.name}: {e.Message}");
                    failCount++;
                }
            }

            EditorUtility.ClearProgressBar();

            // 显示结果
            ShowExportResults(exportResults, successCount, failCount);

            // 刷新AssetDatabase
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 导出单个图表
        /// </summary>
        private string ExportSingleGraph(ComboGraphAsset graph)
        {
            var comboTree = graph.ExportToComboTree();

            string fileName = string.IsNullOrEmpty(graph.ComboTreeId)
                ? graph.name
                : graph.ComboTreeId;
            string path = Path.Combine(_outputFolder, $"{fileName}.asset");

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<ComboTree>(path);
            if (existing != null)
            {
                if (!_overwriteExisting)
                {
                    return $"已跳过（文件已存在）: {path}";
                }

                // 复制数据到现有资产
                EditorUtility.CopySerialized(comboTree, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(comboTree, path);
            }

            // 高亮显示
            if (_pingAfterExport)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ComboTree>(path);
                if (asset != null)
                {
                    EditorApplication.delayCall += () => EditorGUIUtility.PingObject(asset);
                }
            }

            return path;
        }

        /// <summary>
        /// 验证图表
        /// </summary>
        private ValidationResult ValidateGraph(ComboGraphAsset graph)
        {
            var result = new ValidationResult();

            // 检查ID
            if (string.IsNullOrEmpty(graph.ComboTreeId))
            {
                result.AddError("ComboTreeId 不能为空");
                return result;
            }

            // 检查是否有招式节点
            var moveNodes = graph.Nodes.OfType<MoveNode>().ToList();
            if (moveNodes.Count == 0)
            {
                result.AddError("图表中没有任何招式节点");
                return result;
            }

            // 检查招式ID唯一性
            var moveIds = new HashSet<string>();
            foreach (var moveNode in moveNodes)
            {
                if (string.IsNullOrEmpty(moveNode.MoveData.MoveId))
                {
                    result.AddError($"招式节点 '{moveNode.MoveData.MoveName}' 没有设置 MoveId");
                }
                else if (!moveIds.Add(moveNode.MoveData.MoveId))
                {
                    result.AddError($"重复的 MoveId: {moveNode.MoveData.MoveId}");
                }
            }

            // 检查无效的输入类型
            foreach (var transitionNode in graph.Nodes.OfType<TransitionNode>())
            {
                if (!ComboInputTypeRegistry.HasType(transitionNode.InputType))
                {
                    result.AddError($"转换节点使用未知的输入类型: {transitionNode.InputType}");
                }
            }

            return result;
        }

        /// <summary>
        /// 显示导出结果
        /// </summary>
        private void ShowExportResults(List<string> results, int successCount, int failCount)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"导出完成: {successCount} 成功, {failCount} 失败");
            sb.AppendLine("=".PadRight(50, '='));
            sb.AppendLine();

            foreach (var result in results)
            {
                sb.AppendLine(result);
            }

            if (failCount > 0)
            {
                EditorUtility.DisplayDialog("导出完成（有错误）", sb.ToString(), "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("导出成功", sb.ToString(), "确定");
            }

            Debug.Log($"[ComboTreeExporter] {sb}");
        }

        /// <summary>
        /// 验证结果
        /// </summary>
        private class ValidationResult
        {
            private List<string> _errors = new List<string>();

            public bool HasErrors => _errors.Count > 0;
            public string ErrorMessage => string.Join("\n", _errors);

            public void AddError(string message)
            {
                _errors.Add(message);
            }
        }
    }
}
