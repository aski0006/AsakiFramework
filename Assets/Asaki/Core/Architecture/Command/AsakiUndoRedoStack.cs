using System.Collections.Generic;
using Asaki.Core.Logging;

namespace Asaki.Core.Architecture.Command
{
    public class AsakiUndoRedoStack
    {
        private readonly Stack<IAsakiUndoCommand> _undoStack = new Stack<IAsakiUndoCommand>(64);
        private readonly Stack<IAsakiUndoCommand> _redoStack = new Stack<IAsakiUndoCommand>(64);

        // 预分配数组用于 TrimStack，避免每次分配 List
        private IAsakiUndoCommand[] _trimBuffer;
        private const int MAX_HISTORY = 100;

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
            TrimStack(_undoStack, MAX_HISTORY);
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

        private void TrimStack(Stack<IAsakiUndoCommand> stack, int maxSize)
        {
            if (stack.Count <= maxSize)
                return;

            // 使用预分配数组或 ArrayPool 避免 GC 分配
            int count = stack.Count;
            if (_trimBuffer == null || _trimBuffer.Length < count)
            {
                _trimBuffer = new IAsakiUndoCommand[count];
            }

            // 将栈内容复制到数组（从栈顶到栈底）
            stack.CopyTo(_trimBuffer, 0);
            stack.Clear();

            // 保留最新的 maxSize 个命令
            // _trimBuffer[0] 是栈顶（最新），_trimBuffer[count-1] 是栈底（最旧）
            int startIndex = count - maxSize;
            for (int i = startIndex; i < count; i++)
            {
                stack.Push(_trimBuffer[i]);
            }

            // 归还多余的命令到对象池（最旧的那些）
            for (int i = 0; i < startIndex; i++)
            {
                AsakiCommandPoolManager.Return(_trimBuffer[i]);
            }

            // 清理引用，避免内存泄漏
            for (int i = 0; i < count; i++)
            {
                _trimBuffer[i] = null;
            }
        }
    }
}
