using Asaki.Core.Logging;
using UnityEngine;
using System.Text;

namespace Asaki.Tests
{
	public class AsakiLogTest : MonoBehaviour
	{
		private float _timeAccumulator;
		private int _frameCount;

		private void Update()
		{
			// 1. 纯净测试：传递值类型，避免 new object
			// 注意：即便如此，转 String 依然有 GC，但比 new object 小得多
			ALog.Trace("PlayerPos", transform.position);
            
			// 2. 模拟高频业务逻辑
			ALog.Trace("Frame", Time.frameCount);

			// FPS 计算 (不使用 OnGUI)
			_timeAccumulator += Time.unscaledDeltaTime;
			_frameCount++;
			if (_timeAccumulator >= 1.0f)
			{
				// 使用 Log 输出 FPS，而不是 OnGUI
				ALog.Info($"FPS Report", _frameCount / _timeAccumulator);
				_frameCount = 0;
				_timeAccumulator = 0;
			}
		}

	}
}
