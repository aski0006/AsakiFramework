using System;
using System.Collections.Generic;
using Asaki.Core.Logging;

namespace Asaki.Core.Architecture.Command
{
    public class AsakiUndoRedoStack
    {
        private readonly Stack<IAsakiUndoCommand> _undoStack = new Stack<IAsakiUndoCommand>(
            AsakiArchitectureConstants.DefaultUndoRedoStackCapacity
        );
        private readonly Stack<IAsakiUndoCommand> _redoStack = new Stack<IAsakiUndoCommand>(
            AsakiArchitectureConstants.DefaultUndoRedoStackCapacity
        );

        // 预分配数组用于 TrimStack，避免每次分配 List
        // 延迟初始化，避免不必要的内存占用
        private IAsakiUndoCommand[] _trimBuffer;
        private readonly object _trimLock = new object();
        private readonly int _maxHistory = AsakiArchitectureConstants.DefaultUndoRedoMaxHistory;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public void RecordCommand(IAsakiUndoCommand command)
        {
            if (!command.CanUndo)
            {
                // 不可撤销的命令会清空历史
                ClearHistory();
                ALog.Warn("[UndoRedo] Non-undoable command executed, history cleared");
                return;
            }

            _undoStack.Push(command);
            _redoStack.Clear(); // 执行新命令后清空 Redo 栈

            // 限制栈大小（移除最旧的记录）
            TrimStack(_undoStack, _maxHistory);
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                ALog.Warn("[UndoRedo] No command to undo");
                return;
            }

            IAsakiUndoCommand cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
            ALog.Info($"[UndoRedo] Undid {cmd.GetType().Name}");
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                ALog.Warn("[UndoRedo] No command to redo");
                return;
            }

            IAsakiUndoCommand cmd = _redoStack.Pop();
            cmd.Redo();
            _undoStack.Push(cmd);
            ALog.Info($"[UndoRedo] Redid {cmd.GetType().Name}");
        }

        public void ClearHistory()
        {
            // 归还对象池
            while (_undoStack.Count > 0)
            {
                IAsakiUndoCommand cmd = _undoStack.Pop();
                AsakiCommandPoolManager.Return(cmd);
            }

            while (_redoStack.Count > 0)
            {
                IAsakiUndoCommand cmd = _redoStack.Pop();
                AsakiCommandPoolManager.Return(cmd);
            }

            ALog.Info("[UndoRedo] History cleared");
        }

        /// <summary>
        /// 修剪栈到指定大小，使用预分配 buffer 避免 GC
        /// </summary>
        private void TrimStack(Stack<IAsakiUndoCommand> stack, int maxSize)
        {
            if (stack.Count <= maxSize)
                return;

            int totalCount = stack.Count;
            int removeCount = totalCount - maxSize;

            // 确保 trim buffer 足够大
            EnsureTrimBufferCapacity(totalCount);

            // 使用预分配的 buffer 替代分配新 List
            // 将栈元素弹出到 buffer 中（逆序）
            for (int i = 0; i < totalCount; i++)
            {
                _trimBuffer[i] = stack.Pop();
            }

            // 归还需要移除的旧命令（buffer 中的后 removeCount 个元素）
            for (int i = maxSize; i < totalCount; i++)
            {
                AsakiCommandPoolManager.Return(_trimBuffer[i]);
                _trimBuffer[i] = null; // 帮助 GC
            }

            // 将保留的命令重新压入栈（从 maxSize-1 到 0，这样栈顶是最新的）
            for (int i = maxSize - 1; i >= 0; i--)
            {
                stack.Push(_trimBuffer[i]);
                _trimBuffer[i] = null; // 帮助 GC
            }
        }

        /// <summary>
        /// 确保 trim buffer 容量足够
        /// </summary>
        private void EnsureTrimBufferCapacity(int requiredCapacity)
        {
            if (_trimBuffer != null && _trimBuffer.Length >= requiredCapacity)
                return;

            // 扩容：至少是要求的容量，最多扩容到 _maxHistory * 2
            int newCapacity = Math.Max(requiredCapacity, _trimBuffer?.Length ?? 0);
            newCapacity = Math.Min(newCapacity * 2, _maxHistory * 2);
            newCapacity = Math.Max(newCapacity, _maxHistory);

            Array.Resize(ref _trimBuffer, newCapacity);
        }
    }
}
