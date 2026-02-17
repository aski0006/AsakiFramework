﻿using Asaki.Core.Attributes;
using Asaki.Core.Audio;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples
{
    public class AsakiAudioExample
        : MonoBehaviour,
            IAsakiInject<IAsakiAudioService>,
            IAsakiAutoInject
    {
        public bool IsInitialized { get; private set; } = false;

        private IAsakiAudioService _asakiAudioService;

        [AsakiInject]
        public void Inject(IAsakiAudioService args)
        {
            IsInitialized = true;
            ALog.Info("AsakiAudioExample initialized");

            _asakiAudioService = args;
            ALog.Info("Audio service obtained: " + (_asakiAudioService != null));
        }
    }
}
