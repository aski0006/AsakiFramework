using Asaki.Core.Logging;
using System;

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
		protected AsakiValue(Func<AsakiValue<T>> factory = null) : base(
			factory != null ? new Func<AsakiValueBase>(() => factory()) : null) { }

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
				instance.Value = Value;
				return instance;
			}

			ALog.Warn("Failed to clone AsakiValue");
			return null;
		}
	}
}
