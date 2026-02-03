using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.UI
{
    public class AsakiUIPanel : EditorWindow
    {
        [MenuItem("Asaki/UI/UI Layout Panel", false, 13)]
        public static void ShowWindow()
        {
            AsakiUIPanel win = GetWindow<AsakiUIPanel>("UI Ops");
            win.minSize = new Vector2(200, 100);
        }

        private void OnGUI()
        {
            GUILayout.Label("Quick Anchors", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill (全屏)", GUILayout.Height(30)))
                ApplyPreset(AsakiUITools.FillParent);
            if (GUILayout.Button("Snap (吸附)", GUILayout.Height(30)))
                AsakiUITools.SnapAnchors();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Top", GUILayout.Height(25)))
                ApplyPreset(AsakiUITools.TopStretch);
            if (GUILayout.Button("Center", GUILayout.Height(25)))
                ApplyPreset(AsakiUITools.CenterAndZero);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Alignment", EditorStyles.boldLabel);

            // 这里可以添加“左对齐”、“右对齐”等功能，原理就是修改 anchoredPosition
            if (GUILayout.Button("Align X (Reset)", GUILayout.Height(20)))
            {
                foreach (GameObject go in Selection.gameObjects)
                {
                    RectTransform rt = go.GetComponent<RectTransform>();
                    if (rt)
                    {
                        Undo.RecordObject(rt, "Align X");
                        rt.anchoredPosition = new Vector2(0, rt.anchoredPosition.y);
                    }
                }
            }
        }

        // 包装一下之前的 ContextMenu 方法调用
        private void ApplyPreset(System.Action<MenuCommand> action)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt)
                    action(new MenuCommand(rt));
            }
        }
    }
}
