using Asaki.Core.Broker;
using Asaki.Core.Context;
using UnityEngine;

namespace Game.Test
{
	public struct AsakiPlayerJumpExampleEvent : IAsakiEvent { }
	public class BrokerExample : MonoBehaviour, IAsakiHandler<AsakiPlayerJumpExampleEvent>
	{
		private void OnEnable()
		{
			this.AsakiRegister();
		}

		private void OnDisable()
		{
			this.AsakiUnregister();
		}
		public void OnEvent(AsakiPlayerJumpExampleEvent e)
		{
			Debug.LogError("OnEvent");
		}

		[ContextMenu("Test")]
		public void test()
		{
			AsakiContext.Get<IAsakiEventService>().Publish(new AsakiPlayerJumpExampleEvent());
		}
	}
}
