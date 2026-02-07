using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Asaki.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Asaki.Editor.UI
{
    public static class AsakiUIScriptGeneratorWindow
    {
        private const string DEFAULT_NAMESPACE = "Asaki.UI";
        private const string DEFAULT_OUTPUT_PATH = "Assets/Asaki/Generated/UI/Windows";

        // 忽略标记：GameObject名称中包含这些标记的将被跳过
        private static readonly string[] IgnoreMarkers = { "[Ignore]", "[ignore]", "_ignore", "_Ignore" };

        // ========================= 主入口 =========================

        [MenuItem("Asaki/UI/Ignore/Add [Ignore] Prefix", false, 10)]
        public static void AddIgnorePrefix()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select at least one GameObject.", "OK");
                return;
            }

            Undo.RecordObjects(selectedObjects.Select(go => go.transform).ToArray(), "Add [Ignore] Prefix");
            int count = 0;

            foreach (GameObject go in selectedObjects)
            {
                if (!ShouldIgnore(go.name))
                {
                    go.name = "[Ignore] " + go.name;
                    count++;
                    EditorUtility.SetDirty(go);
                }
            }

            Debug.Log($"[AsakiUI] Added [Ignore] prefix to {count} GameObject(s).");
        }

        [MenuItem("Asaki/UI/Ignore/Remove [Ignore] Prefix", false, 11)]
        public static void RemoveIgnorePrefix()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select at least one GameObject.", "OK");
                return;
            }

            Undo.RecordObjects(selectedObjects.Select(go => go.transform).ToArray(), "Remove [Ignore] Prefix");
            int count = 0;

            foreach (GameObject go in selectedObjects)
            {
                string newName = RemoveIgnorePrefixFromName(go.name);
                if (newName != go.name)
                {
                    go.name = newName;
                    count++;
                    EditorUtility.SetDirty(go);
                }
            }

            Debug.Log($"[AsakiUI] Removed [Ignore] prefix from {count} GameObject(s).");
        }

        [MenuItem("Asaki/UI/Ignore/Add [Ignore] Prefix", true, 10)]
        private static bool ValidateAddIgnorePrefix()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Asaki/UI/Ignore/Remove [Ignore] Prefix", true, 11)]
        private static bool ValidateRemoveIgnorePrefix()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Asaki/UI/Generate UI Script &#g", false, 20)] // Alt+Shift+G
        public static void GenerateScript()
        {
            GameObject selected = Selection.activeGameObject;
            if (!ValidateSelection(selected))
                return;

            // 生成默认文件名（去掉空格和非法字符）
            string defaultName = SanitizeClassName(selected.name) + "Window";

            string filePath = EditorUtility.SaveFilePanelInProject(
                "Save UI Script",
                defaultName,
                "cs",
                "Choose location to save the generated script",
                DEFAULT_OUTPUT_PATH
            );

            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                string scriptContent = GenerateScriptContent(selected, filePath);
                File.WriteAllText(filePath, scriptContent, Encoding.UTF8);
                AssetDatabase.Refresh();

                if (
                    EditorUtility.DisplayDialog(
                        "Success",
                        $"Script generated:\n{filePath}\n\nAdd to scene object?",
                        "Yes",
                        "No"
                    )
                )
                {
                    AddScriptToObject(selected, Path.GetFileNameWithoutExtension(filePath));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AsakiUI] Script generation failed: {e}");
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }

        // ========================= 验证 =========================

        private static bool ValidateSelection(GameObject selected)
        {
            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Please select a UI GameObject in Hierarchy.",
                    "OK"
                );
                return false;
            }

            if (selected.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Selected object must have RectTransform.",
                    "OK"
                );
                return false;
            }

            return true;
        }

        // ========================= 代码生成 =========================

        private static string GenerateScriptContent(GameObject root, string filePath)
        {
            string className = Path.GetFileNameWithoutExtension(filePath);
            string namespaceName = ExtractNamespace(filePath);
            var componentData = CollectComponents(root.transform);

            StringBuilder sb = new StringBuilder();

            // === Using 语句 ===
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated by AsakiUIScriptGenerator");
            sb.AppendLine();
            sb.AppendLine("using Asaki.Core.Attributes;");
            sb.AppendLine("using Asaki.Core.UI;");
            sb.AppendLine("using Asaki.Unity.Services.UI;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");

            if (componentData.Any(d => d.RequiresTMPro))
                sb.AppendLine("using TMPro;");

            sb.AppendLine();

            // === 命名空间与类 ===
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : AsakiUIWindow");
            sb.AppendLine("    {");
            sb.AppendLine();

            // === 字段生成 ===
            if (componentData.Count > 0)
            {
                sb.AppendLine("        #region UI Components");
                sb.AppendLine();

                foreach (ComponentInfo data in componentData)
                {
                    string parentAttr = string.IsNullOrEmpty(data.ParentPath)
                        ? ""
                        : $", Parent = \"{data.ParentPath}\"";

                    sb.AppendLine(
                        $"        [AsakiUIBuilder(AsakiUIWidgetType.{data.WidgetType}, Name = \"{data.OriginalName}\"{parentAttr})]"
                    );
                    sb.AppendLine($"        [SerializeField]");
                    sb.AppendLine($"        private {data.TypeName} {data.FieldName};");
                    sb.AppendLine();
                }

                sb.AppendLine("        #endregion");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // ========================= 组件收集 =========================

        private static List<ComponentInfo> CollectComponents(Transform root)
        {
            var components = new List<ComponentInfo>();
            var usedNames = new HashSet<string>();

            // 首先检查根节点本身是否包含UI组件
            if (!ShouldIgnore(root.name))
            {
                if (TryIdentifyComponent(root, out ComponentInfo rootInfo))
                {
                    rootInfo.ParentPath = "";
                    rootInfo.OriginalName = root.name;

                    // 生成唯一字段名
                    string baseName = GenerateFieldName(root.name);
                    rootInfo.FieldName = MakeUniqueFieldName(baseName, usedNames);

                    components.Add(rootInfo);
                }
            }

            // 深度优先遍历子节点
            foreach (Transform child in root)
            {
                Traverse(child, root, "", components, usedNames);
            }

            return components;
        }

        private static void Traverse(
            Transform current,
            Transform root,
            string parentPath,
            List<ComponentInfo> components,
            HashSet<string> usedNames
        )
        {
            // 检查是否应该忽略此GameObject
            if (ShouldIgnore(current.name))
            {
                return;
            }

            // 尝试识别组件
            if (TryIdentifyComponent(current, out ComponentInfo info))
            {
                info.ParentPath = parentPath;
                info.OriginalName = current.name;

                // 生成唯一字段名
                string baseName = GenerateFieldName(current.name);
                info.FieldName = MakeUniqueFieldName(baseName, usedNames);

                components.Add(info);
            }

            // 递归子节点
            string currentPath = string.IsNullOrEmpty(parentPath)
                ? current.name
                : $"{parentPath}/{current.name}";

            foreach (Transform child in current)
            {
                Traverse(child, root, currentPath, components, usedNames);
            }
        }

        private static bool ShouldIgnore(string gameObjectName)
        {
            if (string.IsNullOrWhiteSpace(gameObjectName))
                return true;

            foreach (var marker in IgnoreMarkers)
            {
                if (gameObjectName.Contains(marker))
                    return true;
            }

            return false;
        }

        private static string RemoveIgnorePrefixFromName(string gameObjectName)
        {
            if (string.IsNullOrWhiteSpace(gameObjectName))
                return gameObjectName;

            // 移除各种形式的[Ignore]前缀
            string[] prefixes = { "[Ignore] ", "[ignore] ", "[Ignore]", "[ignore]" };
            foreach (var prefix in prefixes)
            {
                if (gameObjectName.StartsWith(prefix))
                {
                    return gameObjectName.Substring(prefix.Length).TrimStart();
                }
            }

            // 移除下划线形式的前缀
            if (gameObjectName.StartsWith("_Ignore") || gameObjectName.StartsWith("_ignore"))
            {
                return gameObjectName.Substring(1).TrimStart('_');
            }

            return gameObjectName;
        }

        private static bool TryIdentifyComponent(Transform transform, out ComponentInfo info)
        {
            info = new ComponentInfo();

            // 优先级：Button > TMP_InputField > TMP_Dropdown > TMP_Text > Legacy > 其他
            // Button优先级最高，因为其他组件可能也包含Button
            if (transform.TryGetComponent<Button>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.Button;
                info.TypeName = "Button";
                return true;
            }

            // TextMeshPro Input Field（必须在TMP_Text之前检查，因为InputField也包含TMP_Text）
            if (transform.TryGetComponent<TMP_InputField>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.InputField;
                info.TypeName = "TMP_InputField";
                info.RequiresTMPro = true;
                return true;
            }

            // TextMeshPro Dropdown
            if (transform.TryGetComponent<TMP_Dropdown>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.Dropdown;
                info.TypeName = "TMP_Dropdown";
                info.RequiresTMPro = true;
                return true;
            }

            // TextMeshPro Text
            if (transform.TryGetComponent<TMP_Text>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.TextMeshPro;
                info.TypeName = "TMP_Text";
                info.RequiresTMPro = true;
                return true;
            }

            // Legacy Text
            if (transform.TryGetComponent<Text>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.Text;
                info.TypeName = "Text";
                return true;
            }

            // Image
            if (transform.TryGetComponent<Image>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.Image;
                info.TypeName = "Image";
                return true;
            }

            // Slider
            if (transform.TryGetComponent<Slider>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.Slider;
                info.TypeName = "Slider";
                return true;
            }

            // Toggle
            if (transform.TryGetComponent<Toggle>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.Toggle;
                info.TypeName = "Toggle";
                return true;
            }

            // ScrollRect
            if (transform.TryGetComponent<ScrollRect>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.ScrollView;
                info.TypeName = "ScrollRect";
                return true;
            }

            // Legacy Input Field
            if (transform.TryGetComponent<InputField>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.InputField;
                info.TypeName = "InputField";
                return true;
            }

            // Legacy Dropdown
            if (transform.TryGetComponent<Dropdown>(out _))
            {
                info.WidgetType = AsakiUIWidgetType.Dropdown;
                info.TypeName = "Dropdown";
                return true;
            }

            return false;
        }

        // ========================= 工具方法 =========================

        private static string SanitizeClassName(string name)
        {
            return name.Replace(" ", "").Replace("-", "_").Replace("(", "").Replace(")", "");
        }

        private static string GenerateFieldName(string objectName)
        {
            // 清理名称：移除括号及其内容（如 "Text (TMP)" -> "Text"）
            string cleanedName = RemoveParenthesesContent(objectName);

            // 清理名称：移除特殊字符，统一分隔符
            cleanedName = SanitizeFieldName(cleanedName);

            // "btn_ok" -> "btnOk" ; "TitleText" -> "titleText"
            string[] parts = cleanedName.Split('_', ' ');
            if (parts.Length == 0 || (parts.Length == 1 && string.IsNullOrEmpty(parts[0])))
                return "uiElement";

            StringBuilder sb = new StringBuilder();
            bool firstValidPart = true;

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                string part = parts[i];

                // 过滤掉常见的无意义词汇
                if (IsNoiseWord(part))
                    continue;

                if (firstValidPart)
                {
                    // 首词保持小写（Unity开发习惯）
                    sb.Append(char.ToLower(part[0]) + part.Substring(1));
                    firstValidPart = false;
                }
                else
                {
                    sb.Append(char.ToUpper(part[0]) + part.Substring(1));
                }
            }

            string result = sb.ToString();

            // 如果结果为空，使用默认名称
            if (string.IsNullOrEmpty(result))
                return "uiElement";

            // 如果结果以数字开头，添加下划线前缀使其成为有效的标识符
            if (char.IsDigit(result[0]))
                return "_" + result;

            return result;
        }

        /// <summary>
        /// 移除括号及其内容，例如 "Text (TMP)" -> "Text"
        /// </summary>
        private static string RemoveParenthesesContent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 匹配括号及其内容，包括全角和半角括号
            return Regex.Replace(input, @"[\(（].*?[\)）]", "").Trim();
        }

        /// <summary>
        /// 清理字段名称中的特殊字符
        /// </summary>
        private static string SanitizeFieldName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 替换常见分隔符为下划线
            string result = input.Replace("-", "_")
                                .Replace(".", "_")
                                .Replace("/", "_")
                                .Replace("\\", "_")
                                .Replace("@", "_")
                                .Replace("#", "_")
                                .Replace("$", "_")
                                .Replace("%", "_")
                                .Replace("&", "_")
                                .Replace("*", "_")
                                .Replace("+", "_")
                                .Replace("=", "_")
                                .Replace("!", "_")
                                .Replace("?", "_")
                                .Replace(",", "_")
                                .Replace(";", "_")
                                .Replace(":", "_")
                                .Replace("'", "")
                                .Replace('"', '_')
                                .Replace("<", "_")
                                .Replace(">", "_")
                                .Replace("[", "_")
                                .Replace("]", "_")
                                .Replace("{", "_")
                                .Replace("}", "_")
                                .Replace("|", "_");

            // 合并多个连续的下划线
            result = Regex.Replace(result, @"_+", "_");

            // 移除首尾下划线
            result = result.Trim('_');

            return result;
        }

        /// <summary>
        /// 检查是否为无意义词汇（在字段命名中应该被过滤掉的词）
        /// </summary>
        private static bool IsNoiseWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return true;

            string lowerWord = word.ToLowerInvariant();

            // 常见的无意义词汇列表
            string[] noiseWords =
            {
                "the", "a", "an",      // 冠词
                "ui", "ui_",           // 重复的UI前缀
                "game", "object",      // 通用词汇
                "component", "element", // 过于通用的后缀
                // UI组件类型名称（避免字段名中出现冗余的组件类型后缀）
                "image", "text", "button", "btn",
                "input", "inputfield", "dropdown",
                "slider", "toggle", "scroll", "scrollrect",
                "scrollbar", "mask", "rawimage"
            };

            return noiseWords.Contains(lowerWord);
        }

        private static string MakeUniqueFieldName(string baseName, HashSet<string> usedNames)
        {
            string name = baseName;
            int counter = 1;
            while (usedNames.Contains(name))
            {
                name = $"{baseName}{counter++}";
            }
            usedNames.Add(name);
            return name;
        }

        private static string ExtractNamespace(string filePath)
        {
            if (filePath.StartsWith("Assets/"))
            {
                string relativePath = filePath.Substring("Assets/".Length);
                string path = Path.GetDirectoryName(relativePath);

                if (!string.IsNullOrEmpty(path))
                {
                    // 将路径转换为有效的命名空间
                    string[] parts = path.Split('/', '\\');
                    var validParts = new List<string>();

                    foreach (var part in parts)
                    {
                        string sanitized = SanitizeNamespacePart(part);
                        if (!string.IsNullOrEmpty(sanitized))
                            validParts.Add(sanitized);
                    }

                    if (validParts.Count > 0)
                        return string.Join(".", validParts);
                }
            }
            return DEFAULT_NAMESPACE;
        }

        /// <summary>
        /// 清理命名空间部分，确保是有效的C#标识符
        /// </summary>
        private static string SanitizeNamespacePart(string part)
        {
            if (string.IsNullOrWhiteSpace(part))
                return null;

            // 移除非法字符
            string result = Regex.Replace(part, @"[^a-zA-Z0-9_]", "_");

            // 确保不以数字开头
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        private static void AddScriptToObject(GameObject target, string className)
        {
            // 动态挂载生成的脚本（需要等编译完成后）
            EditorApplication.delayCall += () =>
            {
                System.Type type = System.Type.GetType($"{className}, Assembly-CSharp");
                if (type != null)
                {
                    target.AddComponent(type);
                    EditorUtility.SetDirty(target);
                    Debug.Log($"[AsakiUI] Added {className} to {target.name}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[AsakiUI] Could not find type {className}. Please add manually after compilation."
                    );
                }
            };
        }

        private class ComponentInfo
        {
            public string FieldName;
            public string OriginalName;
            public string ParentPath;
            public string TypeName;
            public AsakiUIWidgetType WidgetType;
            public bool RequiresTMPro;
        }
    }
}
