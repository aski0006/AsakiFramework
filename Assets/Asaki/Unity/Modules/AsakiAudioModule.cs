using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Audio;
using Asaki.Core.Configs;
using Asaki.Core.Context;
using Asaki.Core.Pooling;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Audio;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace Asaki.Unity.Modules
{
	// 优先级 400，通常依赖 Resources 加载音频
	[AsakiModule(400,
		typeof(AsakiResourcesModule),
		typeof(AsakiPoolModule),
		typeof(AsakiEventBusModule))]
	public class AsakiAudioModule : IAsakiModule
	{
		private IAsakiAudioService _audioService;
		private IAsakiResourceService _resource;
		private IAsakiPoolService _poolService;

		[AsakiInject]
		public void Init(IAsakiResourceService resource, IAsakiPoolService poolService)
		{
			this._resource = resource;
			this._poolService = poolService;
		}

		public void OnInit()
		{
			AsakiConfig config = AsakiContext.Get<AsakiConfig>();
			if (!config) return;

			_audioService = new AsakiAudioService(
				_resource,
				_poolService,
				config.AudioConfig,
				config.AudioConfig.SoundAgentPrefabAssetKey,
				config.AudioConfig.InitialPoolSize
			);

			_audioService.OnInit();

			AsakiContext.Register(_audioService);
		}

		public async UniTask OnInitAsync()
		{
			await _audioService.OnInitAsync();
		}

		public void OnDispose()
		{
			_audioService.OnDispose();
		}
	}
}
