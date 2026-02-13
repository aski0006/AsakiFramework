using Asaki.Core.Logging;
using Asaki.Unity;
using UnityEngine;

namespace Game.Scripts.Player
{
    [RequireComponent(typeof(Camera))]
    public class PlayerCameraController : AsakiMono
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private float lerpSpeed = 5f;
        private Camera _camera;
        private Vector3 _offset = Vector3.zero;
        protected override void OnAwake()
        {
            base.OnAwake();
            _camera = GetCachedComponent<Camera>();
            if (!playerController)
            {
                ALog.Error("PlayerController is not assigned in the inspector.");
            }
        }

        protected override void OnStart()
        {
            _offset = transform.position - playerController.transform.position;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            transform.position =
                Vector3.Lerp(
                    playerController.transform.position + _offset,
                    transform.position,
                    lerpSpeed * Time.fixedDeltaTime
                );
        }


    }
}
