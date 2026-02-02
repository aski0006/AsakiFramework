using System;
using Asaki.Core.Architecture;
using Asaki.Core.Audio;

namespace Game.Scripts.Examples.Architecture.Counter
{
    [Serializable]
    public class CounterArchitecture : AsakiArchitecture
    {
        protected override void OnSetup()
        {
            Resolver.TryGet(out IAsakiAudioService audioService);
            CounterModel model = new CounterModel();
            RegisterModel(model);
            RegisterSystem(new CounterSystem(model));
            RegisterSystem(new AchievementSystem(model, audioService));
        }
    }
}
