using System;
using System.Collections;
using Asaki.Core.Architecture;
using Asaki.Core.Audio;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Generated;
using Cysharp.Threading.Tasks;
using Game.Examples.Architecture.Counter.Events;
using UnityEngine;

namespace Game.Examples.Architecture.Counter
{
    public class AchievementSystem : IAsakiSystem
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
                Unlock("Click Master").Forget();
            }
        }

        private async UniTaskVoid Unlock(string name)
        {
            _isUnlocked = true;
            ALog.Info($"[AchievementSystem] UNLOCKED: {name}");

            // 1. 播放音效 (验证全局服务调用)
            var handle = _audioService.Play(AudioAssetID.Sihan_三Z_STUDIO_HOYO_MiX___DAMIDAMI);
            ALog.Info("[AchievementSystem] *Play Sound Effect*");
            // 2. 发送全局事件 (验证 Broker)
            AsakiBroker.Publish(new AchievementUnlockedEvent(name));
            await StopAudio(handle);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }

        private async UniTask StopAudio(AsakiAudioHandle handle)
        {
            await UniTask.WaitForSeconds(3);
            _audioService.Stop(handle);
        }

    }
}
