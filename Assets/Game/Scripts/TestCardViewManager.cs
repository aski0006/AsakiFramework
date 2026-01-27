using Asaki.Core.Attributes;
using Asaki.Core.Configuration;
using Asaki.Core.Context;
using Asaki.Core.Pooling;
using Asaki.Core.Resources;
using Asaki.Unity.Extensions;
using Asaki.Unity.Services.Async;
using Game.Scripts.Data;
using Game.Scripts.View;
using UnityEngine;

namespace Game.Scripts
{
	public class TestCardViewManager : MonoBehaviour, IAsakiAutoInject, IAsakiSceneService,
		IAsakiInit<IAsakiPoolService, IAsakiConfigService, IAsakiResourceService>
	{
		public string CardViewPrefabPath = "CardView";
		private IAsakiPoolService _asakiPoolService;
		private IAsakiConfigService _asakiConfigService;
		private IAsakiResourceService _asakiResourceService;

		[AsakiInject]
		public void Init(IAsakiPoolService args1, IAsakiConfigService args2, IAsakiResourceService args3)
		{
			_asakiPoolService = args1;
			_asakiConfigService = args2;
			_asakiResourceService = args3;
			Prewarn().Forget();
		}

		private async AsakiTaskVoid Prewarn()
		{
			await _asakiPoolService.PrewarmAsync(CardViewPrefabPath, 8);
		}

		[ContextMenu("SpawnCardView")]
		public void SpawnCardView()
		{
			CardView cv = _asakiPoolService.Spawn<CardView, IAsakiResourceService>(CardViewPrefabPath, _asakiResourceService);
			cv.LoadCardData(_asakiConfigService.Get<CardData>(0)).Forget();
		}

	}
}
