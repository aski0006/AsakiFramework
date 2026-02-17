using System.Threading;
using Asaki.Core.Architecture;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Command
{
    public interface IAsakiCommand
    {
        void Create(IAsakiServiceProvider serviceProvider);
        void Execute();
    }

    public interface IAsakiCommand<out TResult>
    {
        void Create(IAsakiServiceProvider serviceProvider);
        TResult Execute();
    }

    public interface IAsakiCommandAsync
    {
        void Create(IAsakiServiceProvider serviceProvider);
        UniTask ExecuteAsync();
    }

    public interface IAsakiCommandAsync<TResult>
    {
        void Create(IAsakiServiceProvider serviceProvider);
        UniTask<TResult> ExecuteAsync(CancellationToken token = default(CancellationToken));
    }

    public interface IAsakiUndoCommand : IAsakiCommand
    {
        void Undo();
        void Redo();
        bool CanUndo { get; }
    }

    public interface IAsakiUndoCommand<TResult> : IAsakiCommand<TResult>
    {
        void Undo();
        void Redo();
        bool CanUndo { get; }
    }
}
