using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.UI
{
    /// <summary>
    /// Asaki UI 生产力工具
    /// 提供行业标准的快速布局功能，替代手动计算锚点。
    /// </summary>
    public static class AsakiUITools
    {
        // =========================================================
        // 1. 核心神器：智能锚点吸附 (Snap Anchors to Corners)
        // 行业痛点：手动拖拽 UI 到合适位置后，需要繁琐地计算 Anchor Min/Max 才能适配分辨率。
        // 解决方案：一键将 Anchor 吸附到当前 UI 的四个角，瞬间完成适配设置。
        // 快捷键：Alt + Shift + A
        // =========================================================

        [MenuItem("Asaki/Tools/Snap Anchors to Corners &#a")] // & = Alt, # = Shift
        public static void SnapAnchors()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt == null)
                    continue;

                Undo.RecordObject(rt, "Snap Anchors");
                SnapAnchorsToCorners(rt);
            }
        }

        // =========================================================
        // 2. 常用布局预设 (右键 RectTransform 组件菜单)
        // =========================================================

        [MenuItem("CONTEXT/RectTransform/Asaki - Fill Parent (全屏拉伸)")]
        public static void FillParent(MenuCommand command)
        {
            RectTransform rt = (RectTransform)command.context;
            Undo.RecordObject(rt, "Fill Parent");

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        [MenuItem("CONTEXT/RectTransform/Asaki - Center & Zero (居中归零)")]
        public static void CenterAndZero(MenuCommand command)
        {
            RectTransform rt = (RectTransform)command.context;
            Undo.RecordObject(rt, "Center Zero");

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        [MenuItem("CONTEXT/RectTransform/Asaki - Top Stretch (顶部吸附)")]
        public static void TopStretch(MenuCommand command)
        {
            RectTransform rt = (RectTransform)command.context;
            Undo.RecordObject(rt, "Top Stretch");

            // 保持当前高度
            float height = rt.rect.height;

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, height); // Width=0(Stretch), Height=Keep
        }

        // =========================================================
        // 内部算法
        // =========================================================

        private static void SnapAnchorsToCorners(RectTransform rt)
        {
            RectTransform parent = rt.parent as RectTransform;
            if (parent == null)
                return;

            // 获取当前 rect 的四个角在父级空间中的位置
            Vector2 offsetMin = rt.offsetMin;
            Vector2 offsetMax = rt.offsetMax;
            Vector2 anchorMin = rt.anchorMin;
            Vector2 anchorMax = rt.anchorMax;
            Vector2 parentSize = parent.rect.size;

            // 计算新的锚点 (归一化坐标 0~1)
            // 公式：NewAnchorMin = OldAnchorMin + (OffsetMin / ParentSize)
            Vector2 newAnchorMin =
                anchorMin + new Vector2(offsetMin.x / parentSize.x, offsetMin.y / parentSize.y);
            Vector2 newAnchorMax =
                anchorMax + new Vector2(offsetMax.x / parentSize.x, offsetMax.y / parentSize.y);

            // 应用新锚点
            rt.anchorMin = newAnchorMin;
            rt.anchorMax = newAnchorMax;

            // 归零 Offset (因为锚点已经移动到了边缘)
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
