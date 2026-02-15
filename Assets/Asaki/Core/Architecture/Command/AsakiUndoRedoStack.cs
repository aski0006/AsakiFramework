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
        private IAsakiUndoCommand[] _trimBuffer;
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

        private void TrimStack(Stack<IAsakiUndoCommand> stack, int maxSize)
        {
            if (stack.Count <= maxSize)
                return;

            int totalCount = stack.Count;
            var tempList = new List<IAsakiUndoCommand>(totalCount);

            while (stack.Count > 0)
            {
                tempList.Add(stack.Pop());
            }

            for (int i = maxSize; i < tempList.Count; i++)
            {
                AsakiCommandPoolManager.Return(tempList[i]);
            }

            for (int i = maxSize - 1; i >= 0; i--)
            {
                stack.Push(tempList[i]);
            }
        }
    }
}
