using Asaki.Core.Context;
using Asaki.Core.MVVM;
using System;

namespace Asaki.Core.Blackboard
{
	public interface IAsakiBlackboard : IAsakiService, IDisposable
	{
		T GetValue<T>(AsakiBlackboardKey key);
		void SetValue<T>(AsakiBlackboardKey key, T value);
		AsakiProperty<T> GetProperty<T>(AsakiBlackboardKey key);
		bool HasKey(AsakiBlackboardKey key);
		void Remove(AsakiBlackboardKey key);
		void Clear();
		IDisposable BeginBatch();
	}
}
