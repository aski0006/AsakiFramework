using Asaki.Core.Logging;
using UnityEngine;

namespace Game.Scripts.Examples
{
    /// <summary>
    /// ALog Unity 控制台桥接测试脚本
    /// 用于验证改进后的日志系统
    /// </summary>
    public class ALogBridgeTest : MonoBehaviour
    {
        private int _counter;
        private float _timer;

        void Update()
        {
            _timer += Time.deltaTime;

            // 每秒输出一次日志
            if (_timer >= 1f)
            {
                _timer = 0f;
                _counter++;

                // 测试 1：普通日志
                ALog.Info($"测试计数: {_counter}", new { Time = Time.time });

                // 测试 2：警告日志
                if (_counter % 3 == 0)
                {
                    ALog.Warn("每3秒的周期性警告", new { Counter = _counter });
                }

                // 测试 3：错误日志（不带异常）
                if (_counter % 5 == 0)
                {
                    ALog.Error($"模拟错误 #{_counter}", new { Details = "这是一个测试错误" });
                }
            }
        }

        [ContextMenu("Test Error with Exception")]
        private void TestException()
        {
            try
            {
                // 故意抛出异常
                ThrowNestedException();
            }
            catch (System.Exception ex)
            {
                ALog.Error("捕获到嵌套异常", ex);
            }
        }

        private void ThrowNestedException()
        {
            MethodA();
        }

        private void MethodA()
        {
            MethodB();
        }

        private void MethodB()
        {
            throw new System.InvalidOperationException("这是一个测试异常，用于验证堆栈跟踪！");
        }

        [ContextMenu("Test Trace (High Frequency)")]
        private void TestHighFrequency()
        {
            // 测试高频日志（会被聚合）
            for (int i = 0; i < 100; i++)
            {
                ALog.Trace($"高频追踪消息 #{i}", new { Index = i, Time = Time.time });
            }

            ALog.Info("已发送 100 条高频追踪消息，检查 Unity 控制台和 Dashboard");
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.Label($"ALog Bridge Test - Counter: {_counter}");
            GUILayout.Label("请查看 Unity Console (Ctrl+Shift+C)");
            GUILayout.Label("双击日志行可跳转到代码位置");
            GUILayout.Space(10);

            if (GUILayout.Button("触发异常测试"))
            {
                TestException();
            }

            if (GUILayout.Button("触发高频日志测试"))
            {
                TestHighFrequency();
            }

            GUILayout.EndArea();
        }
    }
}
