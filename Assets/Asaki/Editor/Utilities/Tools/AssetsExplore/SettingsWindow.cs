using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.AssetsExplore
{
    public class SettingsWindow : EditorWindow
    {
        private AssetScanner _scanner;
        private Vector2 _scrollPos;
        private string _newExcludedFolder = "";
        private List<string> _excludedFolders;

        public SettingsWindow(AssetScanner scanner)
        {
            _scanner = scanner ?? throw new System.ArgumentNullException(nameof(scanner));
            // 安全转换：从 IReadOnlyCollection 转为 List
            _excludedFolders = scanner.ExcludedFolders?.ToList() ?? new List<string>();
        }

        private void OnGUI()
        {
            titleContent = new GUIContent("扫描设置");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("排除文件夹", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这些文件夹将被忽略，不扫描其中的资源", MessageType.Info);

            // 显示现有排除项
            for (int i = 0; i < _excludedFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_excludedFolders[i]);
                if (GUILayout.Button("移除", GUILayout.Width(50)))
                {
                    _excludedFolders.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            // 添加新排除项
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _newExcludedFolder = EditorGUILayout.TextField("新文件夹:", _newExcludedFolder);
            if (GUILayout.Button("添加", GUILayout.Width(50)))
            {
                if (!string.IsNullOrWhiteSpace(_newExcludedFolder))
                {
                    _excludedFolders.Add(_newExcludedFolder.TrimEnd('/', '\\'));
                    _newExcludedFolder = "";
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("保存设置"))
            {
                _scanner.SetExcludedFolders(_excludedFolders);
                Close();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
