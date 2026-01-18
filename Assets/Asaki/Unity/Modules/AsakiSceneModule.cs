using Asaki.Core;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Async;
using Asaki.Core.Attributes;
using Asaki.Core.Resources;
using Asaki.Core.Scene;
using Asaki.Unity.Services.Scene;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace Asaki.Unity.Modules
{
	[AsakiModule(200,
		typeof(AsakiEventBusModule),
		typeof(AsakiAsyncModule),
		typeof(AsakiResourcesModule))]
	public class AsakiSceneModule : IAsakiModule
	{
		private IAsakiSceneService _asakiSceneService;
		private IAsakiEventService _eventService;
		private IAsakiResourceService _resService;
		private IAsakiAsyncService _asyncService;

		[AsakiInject]
		public void Init(IAsakiEventService eventService, IAsakiResourceService resService, IAsakiAsyncService asyncService)
		{
			this._eventService = eventService;
			this._resService = resService;
			this._asyncService = asyncService;
		}
		public void OnInit()
		{

			_asakiSceneService = new AsakiSceneService(
				_eventService,
				_asyncService,
				_resService);
			_asakiSceneService.PerBuildScene();
			AsakiContext.Register<IAsakiSceneService>(_asakiSceneService);
		}
		public UniTask OnInitAsync()
		{
			return UniTask.CompletedTask;
		}
		public void OnDispose()
		{
			_asakiSceneService.Dispose();
		}
	}
}
