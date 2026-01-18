using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace Asaki.Unity.Modules
{
	[AsakiModule(175,
		typeof(AsakiEventBusModule))]
	public class AsakiSaveModule : IAsakiModule
	{
		private IAsakiSaveService _asakiSaveService;
		private IAsakiEventService _eventService;

		[AsakiInject]
		public void Init(IAsakiEventService eventService)
		{
			this._eventService = eventService;
		}

		public void OnInit()
		{
			_asakiSaveService = new AsakiSaveService(_eventService);
			_asakiSaveService.OnInit();
			AsakiContext.Register(_asakiSaveService);
		}
		public async UniTask OnInitAsync()
		{
			await _asakiSaveService.OnInitAsync();
		}
		public void OnDispose()
		{
			_asakiSaveService.OnDispose();
		}
	}
}
