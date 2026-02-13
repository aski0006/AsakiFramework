using System;
using Asaki.Core.Logging;
using Asaki.Core.Serialization;

namespace Asaki.Core.Blackboard.Variables
{
    [Serializable]
    public abstract class AsakiValueBase
    {
        protected Func<AsakiValueBase> Factory { get; }

        public abstract string TypeName { get; }

        public abstract void ApplyTo(IAsakiBlackboard blackboard, string key);

        public virtual void ApplyTo(IAsakiBlackboard blackboard, AsakiBlackboardKey key)
        {
            ApplyTo(blackboard, key.Hash.ToString());
        }

        public abstract AsakiValueBase Clone();

        protected AsakiValueBase(Func<AsakiValueBase> factory = null)
        {
            Factory = factory;
        }
    }

    [Serializable]
    public abstract class AsakiValue<T> : AsakiValueBase
    {
        /// <summary>
        /// 深度克隆委托，由 Unity 层在初始化时设置
        /// 用于支持 IAsakiSavable 类型的深度克隆
        /// </summary>
        public static Func<IAsakiSavable, IAsakiSavable> DeepCloneSavableFunc { get; set; }

        protected AsakiValue(Func<AsakiValue<T>> factory = null)
            : base(factory != null ? new Func<AsakiValueBase>(() => factory()) : null) { }

        public T Value;

        public override string TypeName => typeof(T).Name;

        public override void ApplyTo(IAsakiBlackboard blackboard, string key)
        {
            blackboard?.SetValue(key, Value);
        }

        public override void ApplyTo(IAsakiBlackboard blackboard, AsakiBlackboardKey key)
        {
            blackboard?.SetValue(key, Value);
        }

        public override AsakiValueBase Clone()
        {
            AsakiValue<T> instance;
            if (Factory != null)
            {
                instance = Factory() as AsakiValue<T>;
            }
            else
            {
                instance = Activator.CreateInstance(GetType()) as AsakiValue<T>;
            }
            if (instance != null)
            {
                instance.Value = CloneValue(Value);
                return instance;
            }

            ALog.Warn("Failed to clone AsakiValue");
            return null;
        }

        /// <summary>
        /// 深度克隆值，正确处理引用类型
        /// </summary>
        private static T CloneValue(T value)
        {
            if (value == null)
                return default;

            var type = typeof(T);

            if (type.IsValueType || type == typeof(string))
                return value;

            if (value is ICloneable cloneable)
                return (T)cloneable.Clone();

            if (value is IAsakiSavable savable)
            {
                if (DeepCloneSavableFunc != null)
                {
                    try
                    {
                        return (T)DeepCloneSavableFunc(savable);
                    }
                    catch (Exception ex)
                    {
                        ALog.Warn(
                            $"Failed to deep clone IAsakiSavable value of type {type.Name}: {ex.Message}"
                        );
                        return value;
                    }
                }

                ALog.Warn(
                    $"Cannot deep clone IAsakiSavable value of type {type.Name}: DeepCloneSavableFunc not set. Returning original reference."
                );
                return value;
            }

            return value;
        }
    }
}
