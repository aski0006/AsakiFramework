using UnityEngine.UIElements;

namespace Asaki.Editor.GraphEditors
{
    public interface IAsakiGraphViewController
    {
        // 创建 GraphView 视觉元素
        VisualElement CreateGraphView();

        // 更新逻辑 (每帧调用，处理 Copy/Paste 等)
        void Update();

        // 保存逻辑 (虽然我们有 Undo，但有时需要手动触发编译)
        void Save();

        // 清理资源
        void Dispose();
    }
}
