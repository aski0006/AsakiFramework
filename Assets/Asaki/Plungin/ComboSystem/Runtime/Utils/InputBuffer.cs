using System.Collections.Generic;

namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 输入缓冲 - 缓存输入以支持连招衔接
    /// </summary>
    public class InputBuffer
    {
        private readonly float _bufferDuration;
        private readonly Queue<BufferedInput> _inputs = new Queue<BufferedInput>();

        private struct BufferedInput
        {
            public string InputTypeId;
            public float Timestamp;
        }

        public InputBuffer(float bufferDuration)
        {
            _bufferDuration = bufferDuration;
        }

        /// <summary>
        /// 压入输入
        /// </summary>
        /// <param name="inputTypeId">输入类型ID</param>
        public void PushInput(string inputTypeId)
        {
            _inputs.Enqueue(new BufferedInput
            {
                InputTypeId = inputTypeId,
                Timestamp = UnityEngine.Time.time
            });
        }

        /// <summary>
        /// 尝试获取有效的缓冲输入
        /// </summary>
        public bool TryGetInput(out string inputTypeId)
        {
            inputTypeId = null;
            float currentTime = UnityEngine.Time.time;

            // 清理过期的输入
            while (_inputs.Count > 0)
            {
                var buffered = _inputs.Peek();
                if (currentTime - buffered.Timestamp > _bufferDuration)
                {
                    _inputs.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // 获取有效输入
            if (_inputs.Count > 0)
            {
                var buffered = _inputs.Dequeue();
                inputTypeId = buffered.InputTypeId;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清空缓冲
        /// </summary>
        public void Clear()
        {
            _inputs.Clear();
        }

        /// <summary>
        /// 更新缓冲（清理过期输入）
        /// </summary>
        public void Update(float deltaTime)
        {
            // 在TryGetInput中已经处理了清理
        }
    }
}
