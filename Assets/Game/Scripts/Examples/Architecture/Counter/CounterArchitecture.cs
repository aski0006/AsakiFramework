using System;
using System.Text;
using Asaki.Core.Architecture;
using Asaki.Core.Audio;
using Asaki.Core.Serialization;
using Asaki.Unity.Utils;
using UnityEngine;

namespace Game.Scripts.Examples.Architecture.Counter
{
    [Serializable]
    public class CounterArchitecture : AsakiArchitecture
    {
        protected override void OnSetup()
        {
            // 启用 Undo/Redo 功能
            EnableUndoRedo();

            Resolver.TryGet(out IAsakiAudioService audioService);
            Resolver.TryGet(out IAsakiSaveSlotManager saveSlotManager);
            CounterModel model = new CounterModel();
            RegisterModel(model);
            RegisterSystem(new CounterSystem(model));
            RegisterSystem(new AchievementSystem(model, audioService));
            Debug.Log(
                $"[CounterArchitecture] Initialized with new architecture!"
                    + $"Slot Count: {saveSlotManager?.GetAllSlots().Count ?? 0}"
            );
        }
    }
}
