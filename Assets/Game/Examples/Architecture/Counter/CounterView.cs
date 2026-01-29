using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Game.Examples.Architecture.Counter.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Examples.Architecture.Counter
{
	public class CounterView : MonoBehaviour, IAsakiAutoInject, IAsakiInit<CounterArchitecture>
	,IAsakiHandler<AchievementUnlockedEvent>
	{
		[SerializeField] public Button BtnAdd;
		[SerializeField] public TMP_Text TxtCount;
		private CounterArchitecture _arch;
		private CounterModel _model;
		private CounterSystem _system;
		[AsakiInject]
		public void Init(CounterArchitecture args)
		{
			_arch = args;
			_model = _arch.GetModel<CounterModel>();
			_system = _arch.GetSystem<CounterSystem>();

			BindEvents();
			UpdateView(_model.count.Value);
			ALog.Info($"CounterView Initialized via Asaki Injection!");
		}

		private void OnEnable()
		{
			this.AsakiRegister();
		}
		
		private void OnDisable()
		{
			this.AsakiUnregister();
		}

		private void BindEvents()
		{
			// View -> System (输入)
			BtnAdd.onClick.AddListener(() =>
			{
				_system.Increment();
			});

			// Model -> View (输出/响应式)
			// 假设 AsakiProperty 有 Subscribe 方法
			_model.count.Subscribe(UpdateView);
		}

		private void UpdateView(int count)
		{
			if (TxtCount != null)
				TxtCount.text = $"Count: {count}";
		}

		private void OnDestroy()
		{
			// 清理 UI 监听，Model 的监听通常随 View 销毁或使用 Unsubscribe
			BtnAdd?.onClick.RemoveAllListeners();
		}
		public void OnEvent(AchievementUnlockedEvent e)
		{
			Debug.Log($"<color=yellow>[View] Received Achievement Event: {e.AchievementName}</color>");
			if (TxtCount != null)
			{
				TxtCount.text += $"\n🏆 {e.AchievementName}!";
			}
		}
	}
}
