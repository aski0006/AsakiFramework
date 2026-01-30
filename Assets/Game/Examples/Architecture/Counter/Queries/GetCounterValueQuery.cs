using Asaki.Core.Architecture.Queries;

namespace Game.Examples.Architecture.Counter.Queries
{
    /// <summary>
    /// 获取计数器当前值的查询
    /// 使用对象池管理，避免 GC 分配
    /// </summary>
    public class GetCounterValueQuery : AsakiQuery<int>
    {
        public override int Query()
        {
            var model = GetModel<CounterModel>();
            return model?.count.Value ?? 0;
        }
    }
}
