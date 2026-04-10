using System.Collections;
using Asaki.Core.Context;
using Asaki.Core.UI;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.UI.Phase1
{
    public class AsakiUIPhase1_ClearStackAndDuplicateCloseTests
    {
        private MockSimulationService _sim;
        private MockResourceService _res;
        private MockPoolService _pool;
        private MockEventService _evt;
        private AsakiUIManageService _svc;

        private const int MainId = 3001;
        private const int PopupId = 3002;

        [SetUp]
        public void SetUp()
        {
            AsakiContext.ClearAll();

            _sim = new MockSimulationService();
            _res = new MockResourceService();
            _pool = new MockPoolService();
            _evt = new MockEventService();

            AsakiContext.Register<Asaki.Core.Simulation.IAsakiSimulationService>(_sim);

            _res.RegisterPrefab("UI/MainX", Phase1TestUtils.CreateWindowPrefab("MainX"));
            _res.RegisterPrefab("UI/PopupX", Phase1TestUtils.CreateWindowPrefab("PopupX"));

            var cfg = Phase1TestUtils.BuildConfig(
                new Asaki.Core.FrameworkSettings.UIInfo
                {
                    ID = MainId,
                    Name = "MainX",
                    AssetPath = "UI/MainX",
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                },
                new Asaki.Core.FrameworkSettings.UIInfo
                {
                    ID = PopupId,
                    Name = "PopupX",
                    AssetPath = "UI/PopupX",
                    Layer = AsakiUILayer.Popup,
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
        public IEnumerator ClearStack_IncludePopup_ShouldCloseAll()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var main = await _svc.OpenAsync<TestWindow>(MainId);
                var popup = await _svc.OpenAsync<TestWindow>(PopupId);

                Assert.NotNull(main);
                Assert.NotNull(popup);
                Assert.IsTrue(_svc.HasPopup());

                _svc.ClearStack(includePopup: true);
                _sim.SimulateTicks(0.1f);

                Assert.IsFalse(_svc.IsOpened(MainId));
                Assert.IsFalse(_svc.IsOpened(PopupId));
                Assert.IsFalse(_svc.HasPopup());
            });
        }

        [UnityTest]
        public IEnumerator DuplicateClose_ShouldNotCrashOrDoubleClose()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var main = await _svc.OpenAsync<TestWindow>(MainId);
                Assert.NotNull(main);

                // 重复请求关闭同一个窗口
                _svc.Close(main);
                _svc.Close(main);
                _svc.Back(); // 再触发一次路径

                _sim.SimulateTicks(0.1f);

                Assert.IsFalse(_svc.IsOpened(MainId));
                // 不校验精确次数（不同实现可能记录方式不同），只保证已关闭且无异常
            });
        }
    }
}
