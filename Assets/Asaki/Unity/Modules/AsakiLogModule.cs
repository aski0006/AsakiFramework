using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Configs;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
	[AsakiModule(priority: -100)] // 极高优先级，确保在业务模块之前运行
	public class AsakiLogModule : IAsakiModule
	{
		private IAsakiLoggingService _service;

		public void OnInit()
		{
			if (!AsakiContext.TryGet(out IAsakiLoggingService iService))
			{
				_service = new AsakiLoggingService();
				AsakiContext.Register<IAsakiLoggingService>(_service);
			}
			else
			{
				_service = iService as AsakiLoggingService;
			}
			if (_service != null && AsakiContext.TryGet(out AsakiConfig config))
			{
				// [关键] 激活配置
				if (config.LogConfig != null)
				{
					_service.ApplyConfig(config.LogConfig);
				}
			}
		}

		public UniTask OnInitAsync()
		{
			return UniTask.CompletedTask;
		}

		public void OnDispose()
		{
			_service = null;
		}
	}
}
