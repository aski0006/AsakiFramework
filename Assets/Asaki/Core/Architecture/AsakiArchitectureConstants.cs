namespace Asaki.Core.Architecture
{
    public static class AsakiArchitectureConstants
    {
        public const int DefaultUndoRedoMaxHistory = 100;
        public const int DefaultUndoRedoStackCapacity = 64;
        public const int DefaultComponentGroupsCapacity = 64;
        public const int DefaultEntityComponentArraySize = 8;

        /// <summary>
        /// 实体组件数组索引的最大 TypeId 阈值
        /// TypeId &lt;= 此值使用数组直接索引 (O(1) 性能)
        /// TypeId &gt; 此值使用 Dictionary 存储 (避免内存浪费)
        /// </summary>
        public const int EntityComponentArrayIndexThreshold = 127;

        /// <summary>
        /// 组件类型注册表的最大 TypeId 上限
        /// 防止过度注册导致溢出
        /// </summary>
        public const int MaxComponentTypeId = 10000;
    }
}
