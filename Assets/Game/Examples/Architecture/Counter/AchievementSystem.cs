using Asaki.Core.Architecture;
using Asaki.Core.Audio;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Game.Examples.Architecture.Counter.Events;
using System;

namespace Game.Examples.Architecture.Counter
{
	public class AchievementSystem:IAsakiSystem
	{
		private readonly CounterModel _model;
		private readonly IAsakiAudioService _audioService; // 引用全局音频服务
		private IDisposable _subscription;
		private bool _isUnlocked = false;
		public AchievementSystem(CounterModel model, IAsakiAudioService audioService)
		{
			_model = model;
			_audioService = audioService;
		}

		public void Setup()
		{
			_subscription = _model.count.Subscribe(OnCountChanged);
		}
		private void OnCountChanged(int count)
		{
			if (!_isUnlocked && count >= 5)
			{
				Unlock("Click Master");
			}
		}
		
		private void Unlock(string name)
		{
			_isUnlocked = true;
			ALog.Info($"[AchievementSystem] UNLOCKED: {name}");

			// 1. 播放音效 (验证全局服务调用)
			// _audioService.PlayUiSound("unlock_sfx"); 
			ALog.Info("[AchievementSystem] *Play Sound Effect*");

			// 2. 发送全局事件 (验证 Broker)
			AsakiBroker.Publish(new AchievementUnlockedEvent(name));
		}

		public void Dispose()
		{
			_subscription?.Dispose();
		}
	}
}
