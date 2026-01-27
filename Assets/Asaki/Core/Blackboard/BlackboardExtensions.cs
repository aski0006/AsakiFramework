using System.Collections.Generic;
using System.Reflection;

namespace Asaki.Core.Blackboard
{
	public static class BlackboardExtensions
	{
		public static void BatchSet(this IAsakiBlackboard blackboard, params (string key, object value)[] updates)
		{
			using (blackboard.BeginBatch())
			{
				foreach ((string key, object value) in updates)
				{
					AsakiBlackboardKey hashKey = new AsakiBlackboardKey(key);
					SetValueDynamic(blackboard, hashKey, value);
				}
			}
		}

		public static void BatchSet(this IAsakiBlackboard blackboard, Dictionary<string, object> updates)
		{
			using (blackboard.BeginBatch())
			{
				foreach (var kvp in updates)
				{
					AsakiBlackboardKey hashKey = new AsakiBlackboardKey(kvp.Key);
					SetValueDynamic(blackboard, hashKey, kvp.Value);
				}
			}
		}

		private static void SetValueDynamic(IAsakiBlackboard blackboard, AsakiBlackboardKey key, object value)
		{
			switch (value)
			{
				case int v:    blackboard.SetValue(key, v); break;
				case float v:  blackboard.SetValue(key, v); break;
				case bool v:   blackboard.SetValue(key, v); break;
				case string v: blackboard.SetValue(key, v); break;
				default:
					MethodInfo method = typeof(IAsakiBlackboard).GetMethod("SetValue");
					MethodInfo generic = method?.MakeGenericMethod(value.GetType());
					generic?.Invoke(blackboard, new object[] { key, value });
					break;
			}
		}
	}
}
