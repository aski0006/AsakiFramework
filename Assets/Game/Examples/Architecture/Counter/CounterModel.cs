using Asaki.Core.Architecture.Interfaces;
using Asaki.Core.MVVM;

namespace Game.Examples.Architecture.Counter
{
	public class CounterModel : IAsakiModel
	{
		public AsakiProperty<int> Count = new AsakiProperty<int>(0);
		public void Dispose() { Count = null; }
		public void Create() { }
	}
}
