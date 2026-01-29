using Asaki.Core.Architecture;
using Asaki.Core.Attributes;
using Asaki.Core.MVVM;

namespace Game.Examples.Architecture.Counter
{
	[AsakiBind]
	public partial class CounterModel : IAsakiModel
	{
		public AsakiProperty<int> count = new AsakiProperty<int>(0);
		public void Dispose() { count = null; }
		public void Create() { }
	}
}
