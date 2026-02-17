﻿using System;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 标记一个方法，自动生成注入语句，需要配合 <see cref="global::Asaki.Core.Context.IAsakiInject"/>
    /// 进行依赖注入参数管理
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AsakiInjectAttribute : Attribute { }
}
