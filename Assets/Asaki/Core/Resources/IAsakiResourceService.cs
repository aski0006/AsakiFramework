using Asaki.Core.Context;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Resources
{
	public class ResHandle<T> : IDisposable where T : class
	{
		private readonly IAsakiResourceService _service;
		public readonly string Location;
		public readonly T Asset;

		public bool IsValid => Asset != null;

		public ResHandle(string location, T asset, IAsakiResourceService service)
		{
			Location = location;
			Asset = asset;
			_service = service;
		}

		public void Dispose()
		{
			if (IsValid)
			{
				_service?.Release(Location, typeof(T));
			}
		}

		public static implicit operator T(ResHandle<T> handle)
		{
			return handle.Asset;
		}
	}
	public interface IAsakiResourceService : IAsakiModule
	{
		UniTask<ResHandle<T>> LoadAsync<T>(string location, Action<float> onProgress, CancellationToken token) where T : class;
		UniTask<ResHandle<T>> LoadAsync<T>(string location, CancellationToken token) where T : class;
		void Release(string location, Type type);

		UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(IEnumerable<string> locations, Action<float> onProgress, CancellationToken token) where T : class;
		UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(IEnumerable<string> locations, CancellationToken token) where T : class;
		public void ReleaseBatch(IEnumerable<string> locations);

		UniTask UnloadUnusedAssets(CancellationToken token = default);
		public void SetTimeoutSeconds(int timeoutSeconds);


	}
}
