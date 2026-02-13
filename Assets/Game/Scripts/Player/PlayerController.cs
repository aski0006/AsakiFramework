using Asaki.Core.Logging;
using Asaki.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : AsakiMono
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private PlayerSO playerSO;
        public PlayerInputSetting PlayerInputSetting { get; private set; }
        public Vector2 MoveInput { get; private set; }
        private Vector3 _moveDirection = new Vector3();
        protected override void OnAwake()
        {
            base.OnAwake();
            PlayerInputSetting = new();
            rb = GetCachedComponent<Rigidbody>();
            if (!playerSO)
            {
                ALog.Error("PlayerSO is not assigned in the inspector.");
            }
        }

        protected override void EnableComponent()
        {
            base.EnableComponent();
            PlayerInputSetting.Enable();
            PlayerInputSetting.Player.Move.performed += _getMoveInput;
            PlayerInputSetting.Player.Move.canceled += _clearMoveInput;
        }
        protected override void DisableComponent()
        {
            base.DisableComponent();
            PlayerInputSetting.Disable();
            PlayerInputSetting.Player.Move.performed -= _getMoveInput;
            PlayerInputSetting.Player.Move.canceled -= _clearMoveInput;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            _moveDirection.x = MoveInput.x;
            _moveDirection.z = MoveInput.y;
            if (_moveDirection == Vector3.zero) return;
            rb.MovePosition(
                _moveDirection.normalized * playerSO.MoveSpeed * Time.fixedDeltaTime + transform.position
            );
        }
        private void _getMoveInput(InputAction.CallbackContext ctx)
        {
            MoveInput = ctx.ReadValue<Vector2>();
        }
        private void _clearMoveInput(InputAction.CallbackContext ctx)
        {
            MoveInput = Vector2.zero;
        }
    }
}
