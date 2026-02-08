// File: Assets/Asaki/Tests/Resources/Mocks/MockProgressTracker.cs
// 进度追踪器，用于测试进度回调

using System.Collections.Generic;

namespace Asaki.Tests.Resources.Mocks
{
    /// <summary>
    /// 进度追踪器，用于验证进度回调
    /// </summary>
    public class MockProgressTracker
    {
        private readonly List<float> _progressValues = new();

        /// <summary>
        /// 记录的所有进度值
        /// </summary>
        public IReadOnlyList<float> ProgressValues => _progressValues;

        /// <summary>
        /// 最后一次报告的进度
        /// </summary>
        public float LastProgress => _progressValues.Count > 0 ? _progressValues[^1] : 0f;

        /// <summary>
        /// 是否已完成（进度达到1.0）
        /// </summary>
        public bool IsComplete => LastProgress >= 1f;

        /// <summary>
        /// 进度更新次数
        /// </summary>
        public int UpdateCount => _progressValues.Count;

        /// <summary>
        /// 进度回调方法
        /// </summary>
        public void OnProgress(float progress)
        {
            _progressValues.Add(progress);
        }

        /// <summary>
        /// 重置追踪器
        /// </summary>
        public void Reset()
        {
            _progressValues.Clear();
        }

        /// <summary>
        /// 验证进度是否单调递增
        /// </summary>
        public bool IsMonotonicallyIncreasing()
        {
            for (int i = 1; i < _progressValues.Count; i++)
            {
                if (_progressValues[i] < _progressValues[i - 1])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取进度委托
        /// </summary>
        public System.Action<float> GetProgressAction() => OnProgress;
    }
}
