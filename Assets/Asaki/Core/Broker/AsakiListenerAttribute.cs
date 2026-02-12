using System;

namespace Asaki.Core.Broker
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class AsakiListenerAttribute : Attribute { }
}
