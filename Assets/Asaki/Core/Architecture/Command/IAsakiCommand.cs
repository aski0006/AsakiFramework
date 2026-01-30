using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Command
{
    public interface IAsakiCommand
    {
        void Create(IAsakiArchitecture architecture);
        void Execute();
    }

    public interface IAsakiCommand<out TResult>
    {
        void Create(IAsakiArchitecture architecture);
        TResult Execute();
    }

    public interface IAsakiCommandAsync
    {
        void Create(IAsakiArchitecture architecture);
        UniTask ExecuteAsync();
    }

    public interface IAsakiCommandAsync<TResult>
    {
        void Create(IAsakiArchitecture architecture);
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
