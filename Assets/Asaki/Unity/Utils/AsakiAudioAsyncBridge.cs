using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

#if ASAKI_USE_UNITASK
#else
using System.Threading.Tasks;
using System.Collections;
#endif

namespace Asaki.Unity.Utils
{
	/// <summary>
	/// [Internal] 音频模块专用的异步桥接器。
	/// 屏蔽 UniTask / Task / Coroutine 的差异。
	/// </summary>
	internal static class AsakiAudioAsyncBridge
	{
		// ==========================================================
		// 1. 统一的返回类型 (ReturnType)
		// ==========================================================
		#if ASAKI_USE_UNITASK
		public struct Awaitable : System.Runtime.CompilerServices.ICriticalNotifyCompletion
		{
			private UniTask _task;
			public Awaitable(UniTask task)
			{
				_task = task;
			}
			public Awaitable GetAwaiter()
			{
				return this;
			}
			public bool IsCompleted => _task.GetAwaiter().IsCompleted;
			public void GetResult()
			{
				_task.GetAwaiter().GetResult();
			}
			public void OnCompleted(Action continuation)
			{
				_task.GetAwaiter().OnCompleted(continuation);
			}
			public void UnsafeOnCompleted(Action continuation)
			{
				_task.GetAwaiter().UnsafeOnCompleted(continuation);
			}

			#if ASAKI_USE_UNITASK
			public void ForgetInternal(Action<Exception> onException)
			{
				_task.Forget();
			}
			#endif
		}

		public struct AwaitableVoid
		{
			private UniTaskVoid _task;
			public AwaitableVoid(UniTaskVoid task)
			{
				_task = task;
			}
			public void Forget()
			{
				_task.Forget();
			}
		}
		#else
		public struct Awaitable : System.Runtime.CompilerServices.ICriticalNotifyCompletion
		{
			private Task _task;
			public Awaitable(Task task)
			{
				_task = task;
			}
			public Awaitable GetAwaiter()
			{
				return this;
			}
			public bool IsCompleted => _task.GetAwaiter().IsCompleted;
			public void GetResult()
			{
				_task.GetAwaiter().GetResult();
			}
			public void OnCompleted(Action continuation)
			{
				_task.GetAwaiter().OnCompleted(continuation);
			}
			public void UnsafeOnCompleted(Action continuation)
			{
				_task.GetAwaiter().UnsafeOnCompleted(continuation);
			}
		}

		public struct AwaitableVoid
		{
			private Task _task;
			public AwaitableVoid(Task task)
			{
				_task = task;
			}
			public void Forget()
			{ /* Task 本身如果不 await 就是 Fire&Forget */
			}
		}
		#endif

		// ==========================================================
		// 2. 统一的延迟方法 (Delay)
		// ==========================================================
		public static Awaitable Delay(int millisecondsDelay, bool ignoreTimeScale, CancellationToken token)
		{
			#if ASAKI_USE_UNITASK
			// Delay 返回的是 UniTask，可以直接构造
			return new Awaitable(UniTask.Delay(millisecondsDelay, ignoreTimeScale, PlayerLoopTiming.Update, token));
			#else
			return new Awaitable(Task.Delay(millisecondsDelay, token));
			#endif
		}

		// ==========================================================
		// 3. 统一的帧等待 (Yield) - [修复点]
		// ==========================================================
		public static Awaitable Yield()
		{
			#if ASAKI_USE_UNITASK
			// [修复] YieldAwaitable -> UniTask
			// 使用辅助方法将 Yield 包装为标准 UniTask
			return new Awaitable(YieldToUniTask());
			#else
			// [修复] YieldAwaitable -> Task
			return new Awaitable(YieldToTask());
			#endif
		}

		#if ASAKI_USE_UNITASK
		// 辅助：将 YieldAwaitable 转换为 UniTask
		private static async UniTask YieldToUniTask()
		{
			await UniTask.Yield(PlayerLoopTiming.Update);
		}
		#else
		// 辅助：将 YieldAwaitable 转换为 Task
		private static async Task YieldToTask()
		{
			await Task.Yield();
		}
		#endif

		// ==========================================================
		// 4. 统一的取消等待 (WaitUntilCanceled)
		// ==========================================================
		public static Awaitable WaitUntilCanceled(CancellationToken token)
		{
			#if ASAKI_USE_UNITASK
			return new Awaitable(UniTask.WaitUntilCanceled(token));
			#else
			// 原生 Task 模拟 WaitUntilCanceled
			return new Awaitable(Task.Delay(-1, token).ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnCanceled));
			#endif
		}


		// ==========================================================
		// 5. Fire and Forget (核心规避方案)
		// ==========================================================

		// ==========================================================
		// 5. Fire and Forget (核心规避方案)
		// ==========================================================

		#if ASAKI_USE_UNITASK
		// [修复点] 添加 Action<Exception> 参数
		public static void FireAndForget(this UniTask task, Action<Exception> onException = null)
		{
			// UniTask.Forget() 默认只打印日志到 UnityConsole
			// 如果用户提供了自定义异常处理，我们需要自己 wrap 一下
			if (onException == null)
			{
				task.Forget();
			}
			else
			{
				ForgetWithCustomHandler(task, onException).Forget();
			}
		}

		private static async UniTaskVoid ForgetWithCustomHandler(UniTask task, Action<Exception> onException)
		{
			try { await task; }
			catch (Exception ex) { onException(ex); }
		}
		#else
		// [修复点] 添加 Action<Exception> 参数
		public static async void FireAndForget(this Task task, Action<Exception> onException = null)
		{
			try { await task; }
			catch (Exception e)
			{
				if (onException != null) onException(e);
				else Debug.LogException(e);
			}
		}
		#endif

		/// <summary>
		/// 安全地“遗忘”一个异步任务。
		/// 替代 async void，防止异常导致的崩溃或静默失败。
		/// </summary>
		public static void FireAndForget(this Awaitable awaitable, Action<Exception> onException = null)
		{
			#if ASAKI_USE_UNITASK
			// UniTask 的 Forget 已经处理了异常日志
			// 但我们需要把我们的 Wrapper 拆箱出来
			// 由于 Awaitable 是结构体，我们需要在 Awaitable 里暴露内部 Task，或者直接在这里操作
			awaitable.ForgetInternal(onException);
			#else
			// 原生 Task 的 FireAndForget 实现
			HandleTask(awaitable, onException);
			#endif
		}

		#if ASAKI_USE_UNITASK
		// 扩展 Awaitable 结构体以支持 Forget
		// 请在 Awaitable 结构体中添加这个方法:
		/*
		public void ForgetInternal(Action<Exception> onException)
		{
		    _task.Forget(); // UniTask 自带异常处理
		}
		*/
		#else
		private static async void HandleTask(Awaitable awaitable, Action<Exception> onException)
		{
			try
			{
				// 等待 Task 完成，如果有异常会在此时抛出
				await awaitable;
			}
			catch (Exception e)
			{
				if (onException != null) onException(e);
				else Debug.LogException(e); // 默认兜底日志
			}
		}
		#endif

	}
}
