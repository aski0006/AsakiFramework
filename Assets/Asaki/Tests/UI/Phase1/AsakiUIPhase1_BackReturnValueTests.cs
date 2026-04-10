using System.Collections;
using Asaki.Core.Context;
using Asaki.Core.UI;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.UI.Phase1
{
    public class AsakiUIPhase1_BackReturnValueTests
    {
        private MockSimulationService _sim;
        private MockResourceService _res;
        private MockPoolService _pool;
        private MockEventService _evt;
        private AsakiUIManageService _svc;

        private const int MainId = 1001;
        private const int ChildId = 1002;
        private const string MainPath = "UI/Main";
        private const string ChildPath = "UI/Child";

        [SetUp]
        public void SetUp()
        {
            AsakiContext.ClearAll();

            _sim = new MockSimulationService();
            _res = new MockResourceService();
            _pool = new MockPoolService();
            _evt = new MockEventService();

            AsakiContext.Register<Asaki.Core.Simulation.IAsakiSimulationService>(_sim);

            _res.RegisterPrefab(MainPath, Phase1TestUtils.CreateWindowPrefab("MainPrefab"));
            _res.RegisterPrefab(ChildPath, Phase1TestUtils.CreateWindowPrefab("ChildPrefab"));

            var cfg = Phase1TestUtils.BuildConfig(
                new Asaki.Core.FrameworkSettings.UIInfo
                {
                    ID = MainId,
                    Name = "Main",
                    AssetPath = MainPath,
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                },
                new Asaki.Core.FrameworkSettings.UIInfo
                {
                    ID = ChildId,
                    Name = "Child",
                    AssetPath = ChildPath,
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                }
            );

            _svc = new AsakiUIManageService(
                cfg,
                new UnityEngine.Vector2(1920, 1080),
                0.5f,
                _evt,
                _res,
                _pool
            );
            _svc.OnInit();
            _svc.OnInitAsync().Forget();
        }

        [TearDown]
        public void TearDown()
        {
            _svc?.OnDispose();
            AsakiContext.ClearAll();
        }

        [UnityTest]
        public IEnumerator Back_ReturnValue_ShouldCloseTopAndDeliverToPrevious()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var main = await _svc.OpenAsync<TestWindow>(MainId);
                var child = await _svc.OpenAsync<TestWindow>(ChildId);

                Assert.NotNull(main);
                Assert.NotNull(child);

                await _svc.Back("OK_FROM_CHILD");
                _sim.SimulateTicks(0.05f);

                Assert.IsTrue(_svc.IsOpened(MainId));
                Assert.IsFalse(_svc.IsOpened(ChildId));
                Assert.AreEqual("OK_FROM_CHILD", main.LastReturnValue);
            });
        }
    }
}
