using System;
using System.Threading;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling.Factories
{
    /// <summary>
    /// 委托工厂 - 通过委托函数创建和管理池对象
    /// </summary>
    public class DelegateFactory<T> : IAsakiPoolObjectFactory<T>
        where T : class
    {
        private readonly Func<UniTask<T>> _createAsyncFunc;
        private readonly Func<T> _createSyncFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;
        private readonly Action<T> _onDestroy;
        private readonly Func<T, bool> _validate;

        /// <summary>
        /// 使用异步创建函数创建工厂
        /// </summary>
        public DelegateFactory(
            Func<UniTask<T>> createAsync,
            Action<T> onGet = null,
            Action<T> onReturn = null,
            Action<T> onDestroy = null,
            Func<T, bool> validate = null
        )
        {
            _createAsyncFunc = createAsync ?? throw new ArgumentNullException(nameof(createAsync));
            _onGet = onGet;
            _onReturn = onReturn;
            _onDestroy = onDestroy;
            _validate = validate;
        }

        /// <summary>
        /// 使用同步创建函数创建工厂
        /// </summary>
        public DelegateFactory(
            Func<T> createSync,
            Action<T> onGet = null,
            Action<T> onReturn = null,
            Action<T> onDestroy = null,
            Func<T, bool> validate = null
        )
        {
            _createSyncFunc = createSync ?? throw new ArgumentNullException(nameof(createSync));
            _onGet = onGet;
            _onReturn = onReturn;
            _onDestroy = onDestroy;
            _validate = validate;
        }

        public async UniTask<T> CreateAsync(CancellationToken token = default)
        {
            if (_createAsyncFunc != null)
                return await _createAsyncFunc();
            return _createSyncFunc?.Invoke();
        }

        public T CreateSync()
        {
            if (_createSyncFunc != null)
                return _createSyncFunc();

            // 如果没有同步函数但有异步函数，抛出异常
            if (_createAsyncFunc != null)
                throw new InvalidOperationException(
                    "No sync create function provided. Use CreateAsync instead."
                );

            throw new InvalidOperationException("No create function provided.");
        }

        public void OnGet(T obj) => _onGet?.Invoke(obj);

        public void OnReturn(T obj) => _onReturn?.Invoke(obj);

        public void OnDestroy(T obj) => _onDestroy?.Invoke(obj);

        public bool Validate(T obj)
        {
            if (_validate != null)
                return _validate(obj);

            return obj != null;
        }
    }
}
