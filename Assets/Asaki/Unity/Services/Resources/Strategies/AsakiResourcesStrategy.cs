using Asaki.Core.Async;
using Asaki.Core.Resources;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources.Strategies
{
	public class AsakiResourcesStrategy : IAsakiResStrategy
	{
		public string StrategyName => "Resources (Native)";
		private IAsakiAsyncService _async;
		public AsakiResourcesStrategy(IAsakiAsyncService async)
		{
			_async = async;
		}
		public UniTask InitializeAsync()
		{
			return UniTask.CompletedTask;
		}
		public async UniTask<Object> LoadAssetInternalAsync(string location, Type type, Action<float> onProgress, CancellationToken token)
		{
			ResourceRequest request = UnityEngine.Resources.LoadAsync(location, type);

			// 如果没有进度回调，直接使用之前的 Bridge
			if (onProgress == null)
			{
				return await request.ToTask(token);
			}

			// === 进度轮询模式 ===
			// 只要没完成，就每帧报告一次进度
			while (!request.isDone)
			{
				// 响应取消
				if (token.IsCancellationRequested)
				{
					// Resources.LoadAsync 无法真正取消底层 IO，但我们可以停止等待
					throw new OperationCanceledException(token);
				}

				// 报告进度
				onProgress.Invoke(request.progress);

				// 等待下一帧
				await _async.WaitFrame(token);
			}

			// 完成
			onProgress.Invoke(1f);
			return request.asset;
		}
		public void UnloadAssetInternal(string location, Object asset)
		{
			if (asset is not GameObject)
				UnityEngine.Resources.UnloadAsset(asset);
		}
		public async UniTask UnloadUnusedAssets(CancellationToken token)
		{
			AsyncOperation op = UnityEngine.Resources.UnloadUnusedAssets();
			if (_async != null)
			{
				while (!op.isDone)
				{
					if (token.IsCancellationRequested) return;
					await _async.WaitFrame(token);
				}
			}
			else
			{
				while (!op.isDone) await Task.Yield();
			}
		}
	}
}
