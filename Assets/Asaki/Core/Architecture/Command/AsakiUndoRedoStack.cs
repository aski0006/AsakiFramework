using Asaki.Core.Logging;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Command
{
	public class AsakiUndoRedoStack
	{
		private readonly Stack<IAsakiUndoCommand> _undoStack = new Stack<IAsakiUndoCommand>(64);
		private readonly Stack<IAsakiUndoCommand> _redoStack = new Stack<IAsakiUndoCommand>(64);
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

			var cmd = _undoStack.Pop();
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

			var cmd = _redoStack.Pop();
			cmd.Redo();
			_undoStack.Push(cmd);
			ALog.Info($"[UndoRedo] Redid {cmd.GetType().Name}");
		}

		public void ClearHistory()
		{
			// 归还对象池
			while (_undoStack.Count > 0)
			{
				var cmd = _undoStack.Pop();
				AsakiCommandPoolManager.Return(cmd);
			}

			while (_redoStack.Count > 0)
			{
				var cmd = _redoStack.Pop();
				AsakiCommandPoolManager.Return(cmd);
			}

			ALog.Info("[UndoRedo] History cleared");
		}

		private void TrimStack(Stack<IAsakiUndoCommand> stack, int maxSize)
		{
			if (stack.Count <= maxSize) return;

			var temp = new List<IAsakiUndoCommand>(stack);
			stack.Clear();

			// 保留最新的 maxSize 个命令
			for (int i = 0; i < maxSize; i++)
			{
				stack.Push(temp[i]);
			}

			// 归还多余的命令到对象池
			for (int i = maxSize; i < temp.Count; i++)
			{
				AsakiCommandPoolManager.Return(temp[i]);
			}
		}
	}
}
