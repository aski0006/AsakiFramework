using System;
using Asaki.Core.Architecture;
using Asaki.Core.Audio;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples.Architecture.Counter.Events;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples.Architecture.Counter
{
    public class AchievementSystem : IAsakiSystem
    {
        private readonly CounterModel _model;
        private readonly IAsakiAudioService _audioService; // 引用全局音频服务
        private IDisposable _subscription;
        private bool _isUnlocked;
        private int _changedCount;

        public AchievementSystem(CounterModel model, IAsakiAudioService audioService)
        {
            _model = model;
            _audioService = audioService;
        }

        public void Create()
        {

        }

        private void OnCountChanged(int count)
        {
            if (!_isUnlocked && count >= 5)
            {
                Unlock("Click Master");
            }
            _changedCount++;
        }

        private void Unlock(string name)
        {
            _isUnlocked = true;
            ALog.Info($"[AchievementSystem] UNLOCKED: {name}");

            // 1. 播放音效 (验证全局服务调用)
            ALog.Info("[AchievementSystem] *Play Sound Effect*");
            // 2. 发送全局事件 (验证 Broker)
            AsakiBroker.Publish(new AchievementUnlockedEvent(name));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }

        public void Start()
        {
            _subscription = _model.count.Subscribe(OnCountChanged);
        }
    }
}
