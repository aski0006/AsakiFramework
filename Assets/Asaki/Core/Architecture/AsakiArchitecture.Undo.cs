using System;
using Asaki.Core.Architecture.Command;
using Asaki.Core.Logging;

namespace Asaki.Core.Architecture
{
    public abstract partial class AsakiArchitecture
    {
        private AsakiUndoRedoStack _asakiUndoRedoStack;

        public void EnableUndoRedo()
        {
            _asakiUndoRedoStack ??= new AsakiUndoRedoStack();
        }

        public void SendUndoCommand<TCommand>()
            where TCommand : class, IAsakiUndoCommand, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            cmd.Create(this);

            try
            {
                if (_enableCommandLogging)
                    ALog.Info($"[UndoCommand] Executing {typeof(TCommand).Name}");

                cmd.Execute();

                // 记录到 Undo 栈（注意：这里不能 Return，因为栈持有引用）
                _asakiUndoRedoStack?.RecordCommand(cmd);
            }
            catch
            {
                // 执行失败则归还对象池
                AsakiCommandPoolManager.Return(cmd);
                throw;
            }
        }

        public void SendUndoCommand<TCommand>(Action<TCommand> configure)
            where TCommand : class, IAsakiUndoCommand, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            configure?.Invoke(cmd);
            cmd.Create(this);

            try
            {
                if (_enableCommandLogging)
                    ALog.Info($"[UndoCommand] Executing {typeof(TCommand).Name}");

                cmd.Execute();
                _asakiUndoRedoStack?.RecordCommand(cmd);
            }
            catch
            {
                AsakiCommandPoolManager.Return(cmd);
                throw;
            }
        }

        public void Undo()
        {
            _asakiUndoRedoStack?.Undo();
        }

        public void Redo()
        {
            _asakiUndoRedoStack?.Redo();
        }

        public bool CanUndo => _asakiUndoRedoStack?.CanUndo ?? false;
        public bool CanRedo => _asakiUndoRedoStack?.CanRedo ?? false;
        public int UndoCount => _asakiUndoRedoStack?.UndoCount ?? 0;
        public int RedoCount => _asakiUndoRedoStack?.RedoCount ?? 0;

        public void ClearUndoHistory()
        {
            _asakiUndoRedoStack?.ClearHistory();
        }
    }
}
