using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Async;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;

// 引用 Pooling 的命名空间

namespace Asaki.Unity.Modules
{
	// 因为 Resources 加载资源时可能需要从池中生成对象
	[AsakiModule(150, typeof(AsakiResourcesModule))]
	public class AsakiPoolModule : IAsakiModule
	{
		private IAsakiPoolService _poolService;

		public void OnInit()
		{
			// 1. 获取依赖
			_poolService = new AsakiPoolService();

			AsakiContext.Register(_poolService);

			ALog.Info("[Asaki] Pooling Service initialized (Async-Native Mode).");
		}

		public UniTask OnInitAsync()
		{
			return UniTask.CompletedTask;
		}

		public void OnDispose()
		{
			_poolService?.Dispose();
		}
	}
}
