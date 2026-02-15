using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools
{
    public class AsakiSceneNameGeneratorWindow : EditorWindow
    {
        private const string CODE_GEN_PATH = "Assets/Asaki/Generated/Scene_2_name/SceneNames.cs";
        private const string CODE_GEN_DIRECTORY = "Assets/Asaki/Generated/Scene_2_name";

        [Serializable]
        private class SceneEntry
        {
            public string SceneName;
            public string Path;
            public bool Enabled;
            public string ConstantName;

            public SceneEntry(string sceneName, string path, bool enabled)
            {
                SceneName = sceneName;
                Path = path;
                Enabled = enabled;
                ConstantName = SanitizeName(sceneName);
            }
        }

        private List<SceneEntry> _scenes = new List<SceneEntry>();
        private Vector2 _scrollPos;

        [MenuItem("Asaki/Window/Scene Name Generator", false, 42)]
        public static void OpenWindow()
        {
            AsakiSceneNameGeneratorWindow window = GetWindow<AsakiSceneNameGeneratorWindow>(
                "Scene Name Gen"
            );
            window.minSize = new Vector2(500, 400);
            window.Show();
            window.LoadScenesFromBuildSettings();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSceneList();
            DrawFooter();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh From Build Settings", EditorStyles.toolbarButton))
            {
                LoadScenesFromBuildSettings();
            }
            if (GUILayout.Button("Select All", EditorStyles.toolbarButton))
            {
                foreach (var scene in _scenes)
                    scene.Enabled = true;
            }
            if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton))
            {
                foreach (var scene in _scenes)
                    scene.Enabled = false;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSceneList()
        {
            EditorGUILayout.LabelField(
                $"Scenes in Build Settings: {_scenes.Count}",
                EditorStyles.boldLabel
            );
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enable", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField(
                "Constant Name",
                EditorStyles.boldLabel,
                GUILayout.Width(180)
            );
            EditorGUILayout.LabelField("Scene Name", EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField("Path", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _scenes.Count; i++)
            {
                SceneEntry scene = _scenes[i];
                EditorGUILayout.BeginHorizontal("box");

                scene.Enabled = EditorGUILayout.Toggle(scene.Enabled, GUILayout.Width(50));

                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField(
                    scene.ConstantName,
                    GUILayout.Width(180)
                );
                if (EditorGUI.EndChangeCheck())
                {
                    scene.ConstantName = SanitizeName(newName);
                }

                EditorGUILayout.LabelField(scene.SceneName, GUILayout.Width(180));
                EditorGUILayout.LabelField(scene.Path, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (HasDuplicateNames())
            {
                EditorGUILayout.HelpBox("Duplicate Constant Names detected!", MessageType.Error);
            }
        }

        private void DrawFooter()
        {
            GUILayout.Space(10);
            int enabledCount = _scenes.Count(s => s.Enabled);
            GUI.enabled = enabledCount > 0 && !HasDuplicateNames();
            if (GUILayout.Button($"Generate Code ({enabledCount} scenes)", GUILayout.Height(40)))
            {
                GenerateCode();
            }
            GUI.enabled = true;
        }

        private bool HasDuplicateNames()
        {
            var enabledScenes = _scenes.Where(s => s.Enabled).Select(s => s.ConstantName);
            return enabledScenes.Count() != enabledScenes.Distinct().Count();
        }

        private void LoadScenesFromBuildSettings()
        {
            _scenes.Clear();
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

            foreach (var buildScene in buildScenes)
            {
                string path = buildScene.path;
                string sceneName = Path.GetFileNameWithoutExtension(path);
                _scenes.Add(new SceneEntry(sceneName, path, buildScene.enabled));
            }
        }

        private void GenerateCode()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Scene Name Generator", "Generating...", 0.5f);

                var enabledScenes = _scenes
                    .Where(s => s.Enabled)
                    .OrderBy(s => s.ConstantName)
                    .ToList();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("// <auto-generated/>");
                sb.AppendLine("// This file is generated by AsakiSceneNameGeneratorWindow.");
                sb.AppendLine("// Do not modify this file manually.");
                sb.AppendLine();
                sb.AppendLine("namespace Asaki.Generated");
                sb.AppendLine("{");
                sb.AppendLine("    public static class SceneNames");
                sb.AppendLine("    {");

                foreach (var scene in enabledScenes)
                {
                    sb.AppendLine(
                        $"        public const string {scene.ConstantName} = \"{scene.SceneName}\";"
                    );
                }

                sb.AppendLine();
                sb.AppendLine("        public static readonly string[] AllScenes = new string[]");
                sb.AppendLine("        {");
                foreach (var scene in enabledScenes)
                {
                    sb.AppendLine($"            {scene.ConstantName},");
                }
                sb.AppendLine("        };");
                sb.AppendLine();
                sb.AppendLine("        public static bool IsValid(string sceneName)");
                sb.AppendLine("        {");
                sb.AppendLine("            for (int i = 0; i < AllScenes.Length; i++)");
                sb.AppendLine("            {");
                sb.AppendLine("                if (AllScenes[i] == sceneName)");
                sb.AppendLine("                    return true;");
                sb.AppendLine("            }");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                WriteFile(CODE_GEN_PATH, sb.ToString());

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Generated {enabledScenes.Count} scene constants to:\n{CODE_GEN_PATH}",
                    "OK"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneNameGenerator] Failed: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string SanitizeName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return "Scene_Unknown";

            string name = rawName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "");

            if (char.IsDigit(name[0]))
                name = "Scene_" + name;

            return name;
        }

        private static void WriteFile(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir) && dir != null)
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }
}
