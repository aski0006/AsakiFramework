using Asaki.Core.Broker;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 当保存操作开始时触发的事件。
    /// </summary>
    /// <remarks>
    /// 此事件在任何保存数据写入之前通过 <see cref="IAsakiEventBroker"/> 系统发布。
    /// 它可用于准备游戏状态进行序列化或向用户显示保存指示器。
    /// </remarks>
    public struct AsakiSaveBeginEvent : IAsakiEvent
    {
        /// <summary>
        /// 保存数据将被写入的文件名或路径。
        /// </summary>
        public string Filename;
    }

    /// <summary>
    /// 当保存操作成功完成时触发的事件。
    /// </summary>
    /// <remarks>
    /// 此事件在所有保存数据成功写入存储后通过 <see cref="IAsakiEventBroker"/> 系统发布。
    /// 它可用于隐藏保存指示器或执行保存后的操作。
    /// </remarks>
    public struct AsakiSaveSuccessEvent : IAsakiEvent
    {
        /// <summary>
        /// 保存数据被写入的文件名或路径。
        /// </summary>
        public string Filename;
    }

    /// <summary>
    /// 当保存操作失败时触发的事件。
    /// </summary>
    /// <remarks>
    /// 此事件在保存操作遇到错误时通过 <see cref="IAsakiEventBroker"/> 系统发布。
    /// 它可用于向用户显示错误消息或处理保存失败的恢复。
    /// </remarks>
    public struct AsakiSaveFailedEvent : IAsakiEvent
    {
        /// <summary>
        /// 保存操作尝试写入数据的文件名或路径。
        /// </summary>
        public string Filename;

        /// <summary>
        /// 解释保存操作失败原因的描述性错误消息。
        /// </summary>
        public string ErrorMessage;
    }
}
