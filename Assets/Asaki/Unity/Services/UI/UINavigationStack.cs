using System.Collections.Generic;
using Asaki.Core.Logging;
using Asaki.Core.UI;

namespace Asaki.Unity.Services.UI
{
    /// <summary>
    /// UI导航栈管理器，负责窗口栈的维护和导航操作。
    /// </summary>
    public class UINavigationStack
    {
        private readonly Stack<IAsakiWindow> _normalStack = new Stack<IAsakiWindow>();
        private readonly Stack<object> _returnValueStack = new Stack<object>();

        public int Count => _normalStack.Count;

        /// <summary>
        /// 将窗口压入导航栈。
        /// </summary>
        public void Push(IAsakiWindow window)
        {
            if (_normalStack.Count > 0)
            {
                _normalStack.Peek().OnCover();
            }
            _normalStack.Push(window);
        }

        /// <summary>
        /// 从导航栈弹出窗口。
        /// </summary>
        public IAsakiWindow Pop()
        {
            if (_normalStack.Count == 0)
                return null;

            var window = _normalStack.Pop();
            if (_normalStack.Count > 0)
            {
                _normalStack.Peek().OnReveal();
            }
            return window;
        }

        /// <summary>
        /// 查看栈顶窗口。
        /// </summary>
        public IAsakiWindow Peek()
        {
            return _normalStack.Count > 0 ? _normalStack.Peek() : null;
        }

        /// <summary>
        /// 检查窗口是否在栈中。
        /// </summary>
        public bool Contains(IAsakiWindow window)
        {
            return _normalStack.Contains(window);
        }

        /// <summary>
        /// 从栈中间移除窗口。
        /// </summary>
        public void RemoveFromMiddle(IAsakiWindow target)
        {
            var temp = new Stack<IAsakiWindow>();
            while (_normalStack.Count > 0)
            {
                var cur = _normalStack.Pop();
                if (cur == target)
                    break;
                temp.Push(cur);
            }
            while (temp.Count > 0)
            {
                _normalStack.Push(temp.Pop());
            }
        }

        /// <summary>
        /// 获取栈中指定类型的窗口。
        /// </summary>
        public T FindWindow<T>()
            where T : class, IAsakiWindow
        {
            foreach (var window in _normalStack)
            {
                if (window is T target)
                    return target;
            }
            return null;
        }

        /// <summary>
        /// 压入返回值。
        /// </summary>
        public void PushReturnValue(object value)
        {
            _returnValueStack.Push(value);
        }

        /// <summary>
        /// 弹出返回值。
        /// </summary>
        public object PopReturnValue()
        {
            return _returnValueStack.Count > 0 ? _returnValueStack.Pop() : null;
        }

        /// <summary>
        /// 清空导航栈。
        /// </summary>
        public void Clear()
        {
            _normalStack.Clear();
            _returnValueStack.Clear();
        }

        /// <summary>
        /// 获取所有栈内窗口。
        /// </summary>
        public IEnumerable<IAsakiWindow> GetAllWindows()
        {
            return _normalStack;
        }
    }
}
