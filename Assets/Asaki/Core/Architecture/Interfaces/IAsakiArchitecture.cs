using Asaki.Core.Context;
using System;

namespace Asaki.Core.Architecture.Interfaces
{
	public interface IAsakiArchitecture : IAsakiSceneService, IDisposable
	{
		T GetSystem<T>() where T : class, IAsakiSystem;
		T GetModel<T>() where T : class, IAsakiModel;
	}
	
	
}
