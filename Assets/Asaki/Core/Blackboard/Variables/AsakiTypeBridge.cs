using Asaki.Core.Logging;
using System;
using System.Collections.Generic;

namespace Asaki.Core.Blackboard.Variables
{
	public static class AsakiTypeBridge
	{
		private static readonly Dictionary<Type, Action<IAsakiBlackboard, AsakiBlackboardKey, object>> _typeLookup
			= new Dictionary<Type, Action<IAsakiBlackboard, AsakiBlackboardKey, object>>();

		private static readonly Dictionary<string, Action<IAsakiBlackboard, AsakiBlackboardKey, object>> _nameLookup
			= new Dictionary<string, Action<IAsakiBlackboard, AsakiBlackboardKey, object>>();

		public static void SetValue(IAsakiBlackboard bb, AsakiBlackboardKey key, object value)
		{
			switch (value)
			{
				case int v:
					bb.SetValue(key, v);
					return;
				case float v:
					bb.SetValue(key, v);
					return;
				case bool v:
					bb.SetValue(key, v);
					return;
				case string v:
					bb.SetValue(key, v);
					return;
			}

			Type type = value.GetType();
			if (_typeLookup.TryGetValue(type, out var setter))
			{
				setter(bb, key, value);
				return;
			}
			ALog.Warn($"[Asaki Blackboard] Unknown type:  {type.Name}");
		}

		public static bool TrySetValue(IAsakiBlackboard bb, AsakiBlackboardKey key, string typeName, object value)
		{
			if (!_nameLookup.TryGetValue(typeName, out var setter)) return false;
			setter(bb, key, value);
			return true;
		}

		public static void Register<T>()
		{
			Type t = typeof(T);

			Action<IAsakiBlackboard, AsakiBlackboardKey, object> action = (bb, key, value) =>
			{
				bb.SetValue(key, (T)value);
			};

			_typeLookup.TryAdd(t, action);

			string name = t.FullName;
			if (!string.IsNullOrEmpty(name))
			{
				_nameLookup.TryAdd(name, action);
			}
		}
	}
}
