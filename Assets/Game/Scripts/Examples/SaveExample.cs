using System;
using System.Collections.Generic;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Reactive;
using Asaki.Core.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Test
{
    [AsakiSave]
    public partial class AsakiSaveExampleModel
    {
        [AsakiSaveMember(Order = 1)]
        public int version;

        [AsakiSaveMember(Order = 2)]
        public List<string> tests = new();

        [AsakiSaveMember(Order = 3)]
        public AsakiProperty<string> testName = new AsakiProperty<string>();
    }

    [AsakiSave]
    public partial class GameSlotMeta : IAsakiSlotMeta
    {
        [AsakiSaveMember]
        public int SlotId { get; set; }

        [AsakiSaveMember]
        public long LastSaveTime { get; set; }

        [AsakiSaveMember]
        public string SaveName { get; set; }
    }

    public class SaveExample : MonoBehaviour
    {
        private AsakiSaveExampleModel asakiSaveExampleModel;
        private GameSlotMeta meta;

        private void Start()
        {
            TestSave().Forget();
        }

        private async UniTaskVoid TestSave()
        {
            asakiSaveExampleModel = new AsakiSaveExampleModel();
            meta = new GameSlotMeta();
            meta.SlotId = 0;
            meta.SaveName = "TestSave";
            meta.LastSaveTime = DateTime.Now.Ticks;
            asakiSaveExampleModel.version = 1;
            asakiSaveExampleModel.tests.Add("Test1");
            asakiSaveExampleModel.tests.Add("Test2");
            await AsakiContext
                .Get<IAsakiSaveService>()
                .SaveSlotAsync(1, meta, asakiSaveExampleModel);

            var a = await AsakiContext
                .Get<IAsakiSaveService>()
                .LoadSlotAsync<GameSlotMeta, AsakiSaveExampleModel>(1);
            Debug.Log(a.Data.version.ToString());
            Debug.Log(a.Data.tests.ToString());
        }
    }
}
