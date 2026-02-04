using System;
using System.Collections.Generic;
using System.Reflection;
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
            string commandType = typeof(TCommand).Name;
            cmd.Create(this);

            AsakiCommandDebugger.NotifyExecuting(commandType, false, true);

            try
            {
                if (_enableCommandLogging)
                    ALog.Info($"[UndoCommand] Executing {commandType}");

                cmd.Execute();

                // 记录到 Undo 栈（注意：这里不能 Return，因为栈持有引用）
                _asakiUndoRedoStack?.RecordCommand(cmd);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    DateTime.Now.Ticks,
                    0,
                    false,
                    null,
                    null,
                    false,
                    true,
                    false,
                    null
                ));
            }
            catch (Exception ex)
            {
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    DateTime.Now.Ticks,
                    0,
                    false,
                    null,
                    null,
                    false,
                    true,
                    true,
                    ex.Message
                ));
                
                // 执行失败则归还对象池
                AsakiCommandPoolManager.Return(cmd);
                throw;
            }
        }

        public void SendUndoCommand<TCommand>(Action<TCommand> configure)
            where TCommand : class, IAsakiUndoCommand, new()
        {
            TCommand cmd = AsakiCommandPoolManager.Rent<TCommand>();
            string commandType = typeof(TCommand).Name;
            configure?.Invoke(cmd);
            cmd.Create(this);

            AsakiCommandDebugger.NotifyExecuting(commandType, false, true);

            try
            {
                if (_enableCommandLogging)
                    ALog.Info($"[UndoCommand] Executing {commandType}");

                cmd.Execute();
                _asakiUndoRedoStack?.RecordCommand(cmd);
                
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    DateTime.Now.Ticks,
                    0,
                    false,
                    null,
                    null,
                    false,
                    true,
                    false,
                    null
                ));
            }
            catch (Exception ex)
            {
                AsakiCommandDebugger.NotifyExecuted(new CommandExecutionInfo(
                    commandType,
                    DateTime.Now.Ticks,
                    0,
                    false,
                    null,
                    null,
                    false,
                    true,
                    true,
                    ex.Message
                ));
                
                AsakiCommandPoolManager.Return(cmd);
                throw;
            }
        }

        public void Undo()
        {
            if (_asakiUndoRedoStack?.CanUndo == true)
            {
                // 获取即将撤销的命令类型
                var undoStack = GetUndoStackSnapshot();
                string commandType = undoStack.Count > 0 ? undoStack[0].GetType().Name : "Unknown";
                
                _asakiUndoRedoStack?.Undo();
                AsakiCommandDebugger.NotifyUndo(commandType);
            }
        }

        public void Redo()
        {
            if (_asakiUndoRedoStack?.CanRedo == true)
            {
                // 获取即将重做的命令类型
                var redoStack = GetRedoStackSnapshot();
                string commandType = redoStack.Count > 0 ? redoStack[0].GetType().Name : "Unknown";
                
                _asakiUndoRedoStack?.Redo();
                AsakiCommandDebugger.NotifyRedo(commandType);
            }
        }

        private System.Collections.Generic.List<IAsakiUndoCommand> GetUndoStackSnapshot()
        {
            // 通过反射获取 Undo 栈的快照
            var stack = new System.Collections.Generic.List<IAsakiUndoCommand>();
            if (_asakiUndoRedoStack == null) return stack;
            
            var field = _asakiUndoRedoStack.GetType().GetField("_undoStack", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(_asakiUndoRedoStack) is System.Collections.Generic.Stack<IAsakiUndoCommand> undoStack)
            {
                stack.AddRange(undoStack);
            }
            return stack;
        }

        private System.Collections.Generic.List<IAsakiUndoCommand> GetRedoStackSnapshot()
        {
            // 通过反射获取 Redo 栈的快照
            var stack = new System.Collections.Generic.List<IAsakiUndoCommand>();
            if (_asakiUndoRedoStack == null) return stack;
            
            var field = _asakiUndoRedoStack.GetType().GetField("_redoStack", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(_asakiUndoRedoStack) is System.Collections.Generic.Stack<IAsakiUndoCommand> redoStack)
            {
                stack.AddRange(redoStack);
            }
            return stack;
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
