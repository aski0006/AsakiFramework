using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Extensions
{
    public static class GUILayoutExtensions
    {
        private static readonly Color SplitterColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private const float SplitterWidth = 3f;
        private static bool _isDragging;

        /// <summary>
        /// 可拖拽的分隔条，支持精确的min/max限制
        /// </summary>
        /// <param name="value">被控制的宽度值</param>
        /// <param name="minWidth">最小宽度</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="reverseDirection">是否反转拖拽方向（用于右侧分割条）</param>
        /// <param name="onDrag">拖拽回调</param>
        public static void Splitter(
            ref float value,
            float minWidth,
            float maxWidth,
            bool reverseDirection = false,
            System.Action onDrag = null
        )
        {
            // 创建一个可视化的Box元素，正确参与自动布局流程
            GUILayout.Box(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.Width(SplitterWidth),
                GUILayout.ExpandHeight(true)
            );

            // 获取该元素实际占用的矩形区域（在绘制后立即获取，位置准确）
            Rect splitterRect = GUILayoutUtility.GetLastRect();

            // 绘制视觉分隔线
            EditorGUI.DrawRect(splitterRect, SplitterColor);

            // 扩展拖拽区域，使鼠标更容易捕捉（左右各扩展2像素）
            Rect dragRect = new Rect(
                splitterRect.x - 2f,
                splitterRect.y,
                splitterRect.width + 4f,
                splitterRect.height
            );

            // 设置鼠标悬停时的光标样式
            EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.ResizeHorizontal);

            // 获取控件ID，用于事件处理
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            Event currentEvent = Event.current;

            // 处理鼠标事件
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (dragRect.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        _isDragging = true;
                        GUIUtility.hotControl = controlID;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDragging && GUIUtility.hotControl == controlID)
                    {
                        _isDragging = false;
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isDragging && GUIUtility.hotControl == controlID)
                    {
                        float deltaX = currentEvent.delta.x;

                        // [修复] 对右侧分割条反转方向
                        if (reverseDirection)
                        {
                            deltaX = -deltaX;
                        }

                        float newWidth = value + deltaX;

                        // 限制在最小/最大宽度范围内
                        newWidth = Mathf.Clamp(newWidth, minWidth, maxWidth);

                        // 只有当变化足够大时才更新，避免抖动
                        if (Mathf.Abs(newWidth - value) > 0.1f)
                        {
                            value = newWidth;
                            onDrag?.Invoke();
                        }

                        currentEvent.Use();
                    }
                    break;
            }
        }
    }
}
