using Asaki.Core.Attributes;
using Asaki.Core.Audio;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples
{
    public class AsakiAudioExample : MonoBehaviour, IAsakiInit<IAsakiAudioService>, IAsakiAutoInject
    {
        // 添加一个公开的标志，用于在Inspector中查看是否被初始化
        public bool IsInitialized { get; private set; } = false;

        private IAsakiAudioService _asakiAudioService;

        [AsakiInject]
        public void Init(IAsakiAudioService args)
        {
            IsInitialized = true;
            ALog.Info("AsakiAudioExample initialized");

            _asakiAudioService = args;
            ALog.Info("Audio service obtained: " + (_asakiAudioService != null));
        }
    }
}
