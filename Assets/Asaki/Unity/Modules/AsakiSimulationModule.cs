using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Simulation;
using Asaki.Unity.Bridge;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Modules
{
	[AsakiModule(priority: 10)]
	public class AsakiSimulationModule : IAsakiModule
	{
		private IAsakiSimulationService _service;
		private GameObject _driverGo;


		public void OnInit()
		{
			_service = new AsakiSimulationService();
			AsakiContext.Register(_service);
			_driverGo = new GameObject("[Asaki.Driver]");
			Object.DontDestroyOnLoad(_driverGo);
			AsakiMonoDriver driver = _driverGo.AddComponent<AsakiMonoDriver>();
			driver.Initialize(_service);
		}
		public UniTask OnInitAsync()
		{
			return UniTask.CompletedTask;
		}
		public void OnDispose()
		{
			if (_driverGo != null)
			{
				Object.Destroy(_driverGo);
				_driverGo = null;
			}

			_service = null;
		}
	}
}
