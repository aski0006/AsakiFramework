using UnityEditor;

namespace Asaki.Editor.Entities
{
    /// <summary>
    /// Entities系统菜单快捷入口
    /// </summary>
    public static class EntityMenu
    {
        [MenuItem("Asaki/Entities/Open Documentation", false, 1000)]
        public static void OpenDocumentation()
        {
            // 打开IMPROVEMENTS.md文件
            string path = "Assets/Asaki/Core/Architecture/Entities/IMPROVEMENTS.md";
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EntityMenu] Documentation file not found at: " + path);
            }
        }
    }
}
