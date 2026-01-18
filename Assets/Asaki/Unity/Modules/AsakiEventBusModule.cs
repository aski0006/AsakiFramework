using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine; //用于打Log

namespace Asaki.Unity.Modules
{
	// 确保优先级较高，尽早接管总线
	[AsakiModule(75)]
	public class AsakiEventBusModule : IAsakiModule
	{
		private IAsakiEventService _eventService; // 改为接口引用更通用

		public void OnInit()
		{
			// 1. 检查是否已经被 Lazy Init 抢先注册了
			if (AsakiContext.TryGet<IAsakiEventService>(out IAsakiEventService existingBus))
			{
				// [收编逻辑]
				// 既然已经有了，模块就直接持有它，将其纳入模块化管理版图
				_eventService = existingBus;
				Debug.Log("[AsakiEventBusModule] Detected existing EventBus (Lazy Init). Adopting it.");
			}
			else
			{
				// 2. [正规军逻辑]
				// 还没有总线，由模块负责创建并注册
				AsakiEventService newBus = new AsakiEventService();
				AsakiContext.Register<IAsakiEventService>(newBus);
				_eventService = newBus;
			}
		}

		public UniTask OnInitAsync()
		{
			return UniTask.CompletedTask;
		}

		public void OnDispose()
		{
			// 在 ClearAll 时：
			// 1. AsakiContext 会遍历所有 Service 调用 IDisposable.Dispose()
			// 2. AsakiContext 会调用 Module.OnDispose()

			// 为了防止双重释放 (Double Dispose)，虽然 AsakiEventService 的 Dispose 是安全的，
			// 但最好在这里不再手动调用，或者确保 Dispose 是幂等的。
			// 由于 _eventService 已经注册在 Context 里，Context.ClearAll 会负责 Dispose 它。
			// 这里我们置空引用即可。

			_eventService = null;
		}
	}
}
