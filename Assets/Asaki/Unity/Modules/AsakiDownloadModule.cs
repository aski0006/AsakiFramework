using Asaki.Core;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Async;
using Asaki.Core.Attributes;
using Asaki.Core.Network;
using Asaki.Unity.Services.Network;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace Asaki.Unity.Modules
{
	[AsakiModule(125,
		typeof(AsakiEventBusModule),
		typeof(AsakiAsyncModule))]
	public class AsakiDownloadModule : IAsakiModule
	{
		private IAsakiDownloadService _asakiDownloadService;
		private IAsakiEventService _eventService;
		private IAsakiAsyncService _asyncService;
		[AsakiInject]
		public void Init(IAsakiEventService eventService, IAsakiAsyncService asyncService)
		{
			_eventService = eventService;
			_asyncService = asyncService;
		}
		public void OnInit()
		{

			_asakiDownloadService = new AsakiDownloadService(
				_asyncService,
				_eventService
			);
			AsakiContext.Register(_asakiDownloadService);
		}
		public UniTask OnInitAsync()
		{
			return UniTask.CompletedTask;
		}
		public void OnDispose() { }
	}
}
