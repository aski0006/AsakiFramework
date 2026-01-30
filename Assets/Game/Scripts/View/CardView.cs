using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Reactive;
using Asaki.Core.Pooling;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Async;
using Asaki.Unity.Services.UI.Observers;
using Cysharp.Threading.Tasks;
using Game.Scripts.Data;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Game.Scripts.View
{
	[Serializable]
	[AsakiBind]
	public partial class CardViewMvvm
	{
		[field: SerializeField] public AsakiProperty<int> CardCost { get; private set; } = new();
		[field: SerializeField] public AsakiProperty<int> CardAtk { get; private set; } = new();
		[field: SerializeField] public AsakiProperty<int> CardDef { get; private set; } = new();
	}

	public class CardView : MonoBehaviour, IAsakiPoolable, IAsakiInit<IAsakiResourceService>
	{
		[SerializeField] private TMP_Text CardCostText;
		[SerializeField] private TMP_Text CardAtkText;
		[SerializeField] private TMP_Text CardDefText;
		[SerializeField] private SpriteRenderer CardSprite;
		[SerializeField] private TMP_Text CardDescription;
		private CardViewMvvm ViewModel { get; } = new CardViewMvvm();
		private AsakiTMPTextIntObserver _cardCostObserver;
		private AsakiTMPTextIntObserver _cardAtkObserver;
		private AsakiTMPTextIntObserver _cardDefObserver;
		private IAsakiResourceService _asakiResourceService;
		private void Awake()
		{
			_cardCostObserver = new AsakiTMPTextIntObserver(CardCostText);
			_cardAtkObserver = new AsakiTMPTextIntObserver(CardAtkText);
			_cardDefObserver = new AsakiTMPTextIntObserver(CardDefText);
		}

		public void Init(IAsakiResourceService args)
		{
			_asakiResourceService = args;
		}

		public async UniTask LoadCardData(CardData cardData)
		{
			ViewModel.CardCost.Value = cardData.Cost;
			ViewModel.CardAtk.Value = cardData.Atk;
			ViewModel.CardDef.Value = cardData.Def;
			CardSprite.sprite = await _asakiResourceService.LoadAsync<Sprite>(cardData.CardSpriteAssetKey, CancellationToken.None);
			CardDescription.text = cardData.CardDescription;
		}

		private void OnEnable()
		{
			ViewModel.CardCost.Bind(_cardCostObserver);
			ViewModel.CardAtk.Bind(_cardAtkObserver);
			ViewModel.CardDef.Bind(_cardDefObserver);
		}

		private void OnDisable()
		{
			ViewModel.CardCost.Unbind(_cardCostObserver);
			ViewModel.CardAtk.Unbind(_cardAtkObserver);
			ViewModel.CardDef.Unbind(_cardDefObserver);
		}

		public void OnSpawn()
		{
			gameObject.SetActive(true);
		}
		public void OnDespawn()
		{
			gameObject.SetActive(false);
			ViewModel.CardCost.Value = 0;
			ViewModel.CardAtk.Value = 0;
			ViewModel.CardDef.Value = 0;
			CardSprite.sprite = null;
			CardDescription.text = "";
		}
	}
}
