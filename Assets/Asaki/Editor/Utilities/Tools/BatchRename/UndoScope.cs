using UnityEditor;

namespace Asaki.Editor.Utilities.Tools.BatchRename
{
    /// <summary>
    /// Undo作用域封装，确保批量操作的原子性
    /// 使用IDisposable模式，支持using语法糖
    /// 关键：与Selection和Inspector的Undo系统无缝集成
    /// </summary>
    public struct UndoScope : System.IDisposable
    {
        private string _groupName;
        private bool _disposed;
        private int _undoGroupIndex;

        /// <summary>
        /// 开启一个新的Undo组
        /// </summary>
        /// <param name="groupName">在Undo历史列表中显示的名称</param>
        public UndoScope(string groupName)
        {
            _groupName = groupName;
            _disposed = false;
            _undoGroupIndex = Undo.GetCurrentGroup();

            Undo.IncrementCurrentGroup(); // 创建新组，后续操作归入此组
        }

        /// <summary>
        /// 设置Undo组名称并完成作用域
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // 设置组名称，用户可通过Ctrl/Cmd+Z一次性撤销整个操作
                Undo.SetCurrentGroupName(_groupName);

                // 可选：合并操作，减少Undo历史条目
                Undo.CollapseUndoOperations(_undoGroupIndex);

                _disposed = true;
            }
        }
    }
}
