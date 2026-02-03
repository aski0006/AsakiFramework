using System;
using System.Collections;
using System.Collections.Generic;

namespace Asaki.Core.Collections
{
    /// <summary>
    /// 魔法容器 - 空间换时间的高性能容器
    /// 特点：O(1)增删改查 + 内存连续 + 稳定句柄
    /// </summary>
    /// <typeparam name="T">存储的元素类型，必须是引用类型</typeparam>
    public class MagicContainer<T> : IEnumerable<T>
        where T : class
    {
        // 三个核心数组
        private readonly List<T> _data = new(); // 数据存储（连续内存）
        private readonly List<int> _handleToIndex = new(); // 句柄→索引映射
        private readonly List<int> _indexToHandle = new(); // 索引→句柄反向映射
        private readonly Stack<int> _freeHandles = new(); // 空闲句柄复用栈

        private int _nextHandle = 0; // 下一个新句柄
        private int _count = 0; // 有效元素数量

        /// <summary>
        /// 有效元素数量
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// 内部数据容量（包含未回收空间）
        /// </summary>
        public int Capacity => _data.Count;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// 添加元素，返回稳定句柄
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>稳定句柄，可用于后续访问</returns>
        public int Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            int index = _data.Count;
            int handle;

            // 优先复用空闲句柄
            if (_freeHandles.Count > 0)
            {
                handle = _freeHandles.Pop();
                _handleToIndex[handle] = index;
            }
            else
            {
                handle = _nextHandle++;
                _handleToIndex.Add(index);
            }

            _data.Add(item);

            // 更新反向映射
            if (index < _indexToHandle.Count)
                _indexToHandle[index] = handle;
            else
                _indexToHandle.Add(handle);

            _count++;
            return handle;
        }

        /// <summary>
        /// 删除元素 - Swap到末尾策略，保持内存连续
        /// </summary>
        /// <param name="handle">要删除元素的句柄</param>
        /// <returns>是否成功删除</returns>
        public bool Remove(int handle)
        {
            if (handle < 0 || handle >= _handleToIndex.Count)
                return false;

            int index = _handleToIndex[handle];
            if (index < 0)
                return false; // 已删除

            int lastIndex = _data.Count - 1;

            // Swap到末尾策略：将最后一个元素移到当前位置
            if (index != lastIndex)
            {
                // 移动数据
                _data[index] = _data[lastIndex];

                // 更新被移动元素的映射
                int movedHandle = _indexToHandle[lastIndex];
                _handleToIndex[movedHandle] = index;
                _indexToHandle[index] = movedHandle;
            }

            // 删除末尾
            _data.RemoveAt(lastIndex);
            _handleToIndex[handle] = -1; // 标记为已删除
            _freeHandles.Push(handle);
            _count--;

            return true;
        }

        /// <summary>
        /// 通过句柄获取元素
        /// </summary>
        /// <param name="handle">元素句柄</param>
        /// <returns>元素实例，如果句柄无效则返回null</returns>
        public T Get(int handle)
        {
            if (handle < 0 || handle >= _handleToIndex.Count)
                return null;

            int index = _handleToIndex[handle];
            if (index < 0)
                return null;

            return _data[index];
        }

        /// <summary>
        /// 尝试通过句柄获取元素
        /// </summary>
        /// <param name="handle">元素句柄</param>
        /// <param name="item">输出元素</param>
        /// <returns>是否成功获取</returns>
        public bool TryGet(int handle, out T item)
        {
            item = Get(handle);
            return item != null;
        }

        /// <summary>
        /// 通过索引获取元素（用于高性能遍历）
        /// </summary>
        /// <param name="index">数组索引</param>
        /// <returns>元素实例</returns>
        public T GetAt(int index)
        {
            if (index < 0 || index >= _data.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _data[index];
        }

        /// <summary>
        /// 通过索引获取句柄
        /// </summary>
        /// <param name="index">数组索引</param>
        /// <returns>句柄</returns>
        public int GetHandleAt(int index)
        {
            if (index < 0 || index >= _indexToHandle.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _indexToHandle[index];
        }

        /// <summary>
        /// 检查句柄是否有效
        /// </summary>
        /// <param name="handle">要检查的句柄</param>
        /// <returns>是否有效</returns>
        public bool IsValidHandle(int handle)
        {
            if (handle < 0 || handle >= _handleToIndex.Count)
                return false;
            return _handleToIndex[handle] >= 0;
        }

        /// <summary>
        /// 获取句柄对应的索引
        /// </summary>
        /// <param name="handle">元素句柄</param>
        /// <returns>数组索引，无效句柄返回-1</returns>
        public int GetIndex(int handle)
        {
            if (handle < 0 || handle >= _handleToIndex.Count)
                return -1;
            return _handleToIndex[handle];
        }

        /// <summary>
        /// 清空所有元素
        /// </summary>
        public void Clear()
        {
            _data.Clear();
            _handleToIndex.Clear();
            _indexToHandle.Clear();
            _freeHandles.Clear();
            _nextHandle = 0;
            _count = 0;
        }

        /// <summary>
        /// 批量处理所有元素 - 最高性能遍历方式
        /// </summary>
        /// <param name="action">处理动作</param>
        public void ForEach(Action<T> action)
        {
            if (action == null)
                return;

            // 直接遍历底层数组，无迭代器开销，缓存友好
            for (int i = 0; i < _data.Count; i++)
            {
                action(_data[i]);
            }
        }

        /// <summary>
        /// 批量处理所有元素（带索引）
        /// </summary>
        /// <param name="action">处理动作，参数为(索引, 元素)</param>
        public void ForEach(Action<int, T> action)
        {
            if (action == null)
                return;

            for (int i = 0; i < _data.Count; i++)
            {
                action(i, _data[i]);
            }
        }

        /// <summary>
        /// 查找满足条件的元素
        /// </summary>
        /// <param name="predicate">条件谓词</param>
        /// <returns>第一个满足条件的元素，找不到返回null</returns>
        public T Find(Predicate<T> predicate)
        {
            if (predicate == null)
                return null;

            for (int i = 0; i < _data.Count; i++)
            {
                T item = _data[i];
                if (predicate(item))
                    return item;
            }
            return null;
        }

        /// <summary>
        /// 查找满足条件的所有元素
        /// </summary>
        /// <param name="predicate">条件谓词</param>
        /// <returns>满足条件的元素列表</returns>
        public List<T> FindAll(Predicate<T> predicate)
        {
            List<T> result = new();
            if (predicate == null)
                return result;

            for (int i = 0; i < _data.Count; i++)
            {
                T item = _data[i];
                if (predicate(item))
                    result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// 检查是否存在满足条件的元素
        /// </summary>
        /// <param name="predicate">条件谓词</param>
        /// <returns>是否存在</returns>
        public bool Exists(Predicate<T> predicate)
        {
            if (predicate == null)
                return false;

            for (int i = 0; i < _data.Count; i++)
            {
                if (predicate(_data[i]))
                    return true;
            }
            return false;
        }

        #region IEnumerable Implementation

        /// <summary>
        /// 获取枚举器（用于foreach遍历）
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
