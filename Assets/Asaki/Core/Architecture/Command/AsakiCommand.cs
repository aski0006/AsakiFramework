using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Command
{
	public abstract class AsakiCommand : IAsakiCommand
	{
		protected IAsakiArchitecture Architecture { get; private set; }
		public void Create(IAsakiArchitecture architecture)
		{
			Architecture = architecture;
			OnCreate(); // 子类可重写此方法进行初始化
		}
		protected virtual void OnCreate() { }
		
		public abstract void Execute();
		
		protected T GetModel<T>() where T : class, IAsakiModel
			=> Architecture.GetModel<T>();

		protected T GetSystem<T>() where T : class, IAsakiSystem
			=> Architecture.GetSystem<T>();

		protected void Log(string message)
			=> ALog.Info($"[{GetType().Name}] {message}");

		protected void LogWarning(string message)
			=> ALog.Warn($"[{GetType().Name}] {message}");

		protected void LogError(string message)
			=> ALog.Error($"[{GetType().Name}] {message}");
	}
	
	public abstract class AsakiCommand<TResult> : IAsakiCommand<TResult>
	{
		protected IAsakiArchitecture Architecture { get; private set; }

		public void Create(IAsakiArchitecture architecture)
		{
			Architecture = architecture;
			OnCreate();
		}

		protected virtual void OnCreate() { }

		public abstract TResult Execute();

		protected T GetModel<T>() where T : class, IAsakiModel
			=> Architecture.GetModel<T>();

		protected T GetSystem<T>() where T : class, IAsakiSystem
			=> Architecture.GetSystem<T>();

		protected void Log(string message)
			=> ALog.Info($"[{GetType().Name}] {message}");

		protected void LogWarning(string message)
			=> ALog.Warn($"[{GetType().Name}] {message}");

		protected void LogError(string message)
			=> ALog.Error($"[{GetType().Name}] {message}");
	}
	
	public abstract class AsakiCommandAsync : IAsakiCommandAsync
	{
		protected IAsakiArchitecture Architecture { get; private set; }

		public void Create(IAsakiArchitecture architecture)
		{
			Architecture = architecture;
			OnCreate();
		}

		protected virtual void OnCreate() { }

		public abstract UniTask ExecuteAsync();

		protected T GetModel<T>() where T : class, IAsakiModel
			=> Architecture.GetModel<T>();

		protected T GetSystem<T>() where T : class, IAsakiSystem
			=> Architecture.GetSystem<T>();

		protected void Log(string message)
			=> ALog.Info($"[{GetType().Name}] {message}");

		protected void LogWarning(string message)
			=> ALog.Warn($"[{GetType().Name}] {message}");

		protected void LogError(string message)
			=> ALog.Error($"[{GetType().Name}] {message}");
	}
	
	public abstract class AsakiCommandAsync<TResult> : IAsakiCommandAsync<TResult>
	{
		protected IAsakiArchitecture Architecture { get; private set; }

		public void Create(IAsakiArchitecture architecture)
		{
			Architecture = architecture;
			OnCreate();
		}

		protected virtual void OnCreate() { }

		public abstract UniTask<TResult> ExecuteAsync();

		protected T GetModel<T>() where T : class, IAsakiModel
			=> Architecture.GetModel<T>();

		protected T GetSystem<T>() where T : class, IAsakiSystem
			=> Architecture.GetSystem<T>();

		protected void Log(string message)
			=> ALog.Info($"[{GetType().Name}] {message}");

		protected void LogWarning(string message)
			=> ALog.Warn($"[{GetType().Name}] {message}");

		protected void LogError(string message)
			=> ALog.Error($"[{GetType().Name}] {message}");
	}
	
	public abstract class AsakiUndoCommand : AsakiCommand, IAsakiUndoCommand
	{
		public virtual bool CanUndo => true;

		public abstract void Undo();

		public virtual void Redo()
		{
			// 默认 Redo = 重新执行
			Execute();
		}
	}
	
	public abstract class AsakiUndoCommand<TResult> : AsakiCommand<TResult>, IAsakiUndoCommand<TResult>
	{
		public virtual bool CanUndo => true;

		public abstract void Undo();

		public virtual void Redo()
		{
			Execute();
		}
	}
}
