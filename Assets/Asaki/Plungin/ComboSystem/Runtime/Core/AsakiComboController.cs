using System;
using System.Linq;
using Asaki.Core.FSM;
using Asaki.Plungin.ComboSystem.States;
using UnityEngine;

namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招系统核心控制器 - 仅负责连招表现，不处理战斗逻辑
    /// </summary>
    public class AsakiComboController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ComboTree comboTree;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Settings")]
        [SerializeField] private float inputBufferDuration = 0.3f;

        // 核心组件
        private AsakiStateMachine<AsakiComboController> _stateMachine;
        private ComboAnimatorBridge _animatorBridge;
        private HitBoxManager _hitBoxManager;
        private InputBuffer _inputBuffer;
        private ComboAnimationEventReceiver _eventReceiver;

        // 运行时状态
        private ComboMove _currentMove;
        private int _comboCount;
        private float _comboTimer;
        private bool _isInitialized;

        #region 公开属性

        /// <summary>
        /// 当前连击数
        /// </summary>
        public int CurrentComboCount => _comboCount;

        /// <summary>
        /// 当前招式
        /// </summary>
        public ComboMove CurrentMove => _currentMove;

        /// <summary>
        /// 连招计时器
        /// </summary>
        public float ComboTimer => _comboTimer;

        /// <summary>
        /// 输入缓冲
        /// </summary>
        internal InputBuffer InputBuffer => _inputBuffer;

        /// <summary>
        /// 状态机
        /// </summary>
        internal AsakiStateMachine<AsakiComboController> StateMachine => _stateMachine;

        /// <summary>
        /// 当前状态类型
        /// </summary>
        public ComboStateType CurrentStateType
        {
            get
            {
                if (_stateMachine?.CurrentState == null) return ComboStateType.Idle;
                var state = _stateMachine.CurrentState;
                if (state is ComboIdleState) return ComboStateType.Idle;
                if (state is ComboStartupState) return ComboStateType.Startup;
                if (state is ComboActiveState) return ComboStateType.Active;
                if (state is ComboRecoveryState) return ComboStateType.Recovery;
                if (state is ComboWindowState) return ComboStateType.ComboWindow;
                if (state is ComboInterruptedState) return ComboStateType.Interrupted;
                return ComboStateType.Idle;
            }
        }

        #endregion

        #region 回调事件（供外部订阅）

        /// <summary>连招开始</summary>
        public event Action OnComboStarted;

        /// <summary>连招中断</summary>
        public event Action<InterruptReason> OnComboInterrupted;

        /// <summary>连招完成（自然结束）</summary>
        public event Action OnComboCompleted;

        /// <summary>招式开始（动画开始播放）</summary>
        public event Action<ComboMove> OnMoveStarted;

        /// <summary>招式判定开始（可以命中敌人）</summary>
        public event Action<HitBoxInfo[]> OnHitBoxesActivated;

        /// <summary>招式判定结束</summary>
        public event Action OnHitBoxesDeactivated;

        /// <summary>招式完成（动画播放完毕）</summary>
        public event Action<ComboMove> OnMoveCompleted;

        /// <summary>连招窗口开启（可以输入下一招）</summary>
        public event Action<float> OnComboWindowOpened;

        /// <summary>连招窗口关闭</summary>
        public event Action OnComboWindowClosed;

        /// <summary>状态变化</summary>
        public event Action<ComboStateType, ComboStateType> OnStateChanged;

        #endregion

        void Awake()
        {
            InitializeComponents();
        }

        void Update()
        {
            if (!_isInitialized) return;

            _stateMachine.Update(Time.deltaTime);
            _inputBuffer.Update(Time.deltaTime);
            _comboTimer += Time.deltaTime;
        }

        void FixedUpdate()
        {
            if (!_isInitialized) return;

            _stateMachine.FixedUpdate(Time.fixedDeltaTime);
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        void InitializeComponents()
        {
            // 使用Asaki标准状态机
            _stateMachine = new AsakiStateMachine<AsakiComboController>(this);

            _animatorBridge = gameObject.AddComponent<ComboAnimatorBridge>();
            _animatorBridge.Initialize(animator);

            _hitBoxManager = gameObject.AddComponent<HitBoxManager>();

            _inputBuffer = new InputBuffer(inputBufferDuration);

            // 添加或获取动画事件接收器
            _eventReceiver = gameObject.GetComponent<ComboAnimationEventReceiver>();
            if (_eventReceiver == null)
            {
                _eventReceiver = gameObject.AddComponent<ComboAnimationEventReceiver>();
            }
            _eventReceiver.Initialize(this);

            // 进入初始状态
            _stateMachine.ChangeState<ComboIdleState>();

            _isInitialized = true;
        }

        /// <summary>
        /// 初始化连招树（可在运行时设置）
        /// </summary>
        public void Initialize(ComboTree tree)
        {
            comboTree = tree;
            if (tree != null)
            {
                inputBufferDuration = tree.InputBufferWindow;
                _inputBuffer = new InputBuffer(inputBufferDuration);
            }
        }

        /// <summary>
        /// 设置连击数
        /// </summary>
        public void SetComboCount(int count)
        {
            _comboCount = count;
        }

        /// <summary>
        /// 触发攻击指令 - 由外部输入系统调用
        /// </summary>
        /// <param name="inputTypeId">输入类型ID</param>
        public void TriggerAttack(string inputTypeId)
        {
            if (!CanAcceptInput())
            {
                // 缓冲输入
                _inputBuffer.PushInput(inputTypeId);
                return;
            }

            ProcessAttackInput(inputTypeId);
        }

        /// <summary>
        /// 处理攻击输入 - 内部调用或从缓冲中消费
        /// </summary>
        internal void ProcessAttackInput(string inputTypeId)
        {
            if (comboTree == null) return;

            ComboMove nextMove = null;
            var previousState = CurrentStateType;

            if (CurrentStateType == ComboStateType.Idle)
            {
                // 从Idle开始新连招，找到第一个匹配的招式
                nextMove = comboTree.Moves?.FirstOrDefault(m => CanStartMove(m, inputTypeId));
            }
            else if (_currentMove != null)
            {
                // 继续连招
                nextMove = comboTree.FindNextMove(_currentMove.MoveId, inputTypeId);
            }

            if (nextMove != null && CanExecuteMove(nextMove))
            {
                var startupState = _stateMachine.GetState<ComboStartupState>();
                startupState.SetMove(nextMove);
                _stateMachine.ChangeState<ComboStartupState>();

                // 通知状态变化
                NotifyStateChanged(previousState, ComboStateType.Startup);
            }
        }

        /// <summary>
        /// 尝试继续连招
        /// </summary>
        internal bool TryContinueCombo(string inputTypeId)
        {
            if (comboTree == null || _currentMove == null) return false;

            var nextMove = comboTree.FindNextMove(_currentMove.MoveId, inputTypeId);
            if (nextMove != null && CanExecuteMove(nextMove))
            {
                var previousState = CurrentStateType;

                var startupState = _stateMachine.GetState<ComboStartupState>();
                startupState.SetMove(nextMove);
                _stateMachine.ChangeState<ComboStartupState>();

                NotifyStateChanged(previousState, ComboStateType.Startup);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试消费缓冲的输入
        /// </summary>
        internal bool TryConsumeBufferedInput(out string inputTypeId)
        {
            return _inputBuffer.TryGetInput(out inputTypeId);
        }

        /// <summary>
        /// 检查是否可以在Idle状态下开始某个招式
        /// </summary>
        bool CanStartMove(ComboMove move, string inputTypeId)
        {
            // 检查是否有从空状态到这个招式的转换
            // 这里简化处理：检查招式的最小连击数要求
            return move.MinComboCount <= 0;
        }

        /// <summary>
        /// 检查是否可以执行招式
        /// </summary>
        bool CanExecuteMove(ComboMove move)
        {
            // 检查冷却
            if (move.IsOnCooldown(Time.time))
                return false;

            // 检查连击数限制
            if (_comboCount < move.MinComboCount)
                return false;

            if (move.MaxComboCount > 0 && _comboCount >= move.MaxComboCount)
                return false;

            return true;
        }

        /// <summary>
        /// 中断当前连招 - 由外部系统调用（如受击时）
        /// </summary>
        public void InterruptCombo(InterruptReason reason)
        {
            if (CurrentStateType == ComboStateType.Idle)
                return;

            var previousState = CurrentStateType;

            _stateMachine.ChangeState<ComboInterruptedState>();
            OnComboInterrupted?.Invoke(reason);

            NotifyStateChanged(previousState, ComboStateType.Interrupted);

            // 清理
            _hitBoxManager.DeactivateAllHitBoxes();
            _comboCount = 0;
            _comboTimer = 0f;
        }

        /// <summary>
        /// 重置连招状态
        /// </summary>
        public void ResetCombo()
        {
            var previousState = CurrentStateType;

            _stateMachine.ChangeState<ComboIdleState>();
            _hitBoxManager.DeactivateAllHitBoxes();
            _inputBuffer.Clear();
            _comboCount = 0;
            _comboTimer = 0f;
            _currentMove = null;

            NotifyStateChanged(previousState, ComboStateType.Idle);
            OnComboCompleted?.Invoke();
        }

        /// <summary>
        /// 检查是否可以接受输入
        /// </summary>
        public bool CanAcceptInput()
        {
            var state = CurrentStateType;
            return state == ComboStateType.Idle ||
                   state == ComboStateType.ComboWindow;
        }

        /// <summary>
        /// 处理动画事件
        /// </summary>
        internal void OnAnimationEvent(string eventName)
        {
            // 动画事件处理可以在这里扩展
            // 当前由状态机自行管理时机
        }

        /// <summary>
        /// 通知状态变化
        /// </summary>
        internal void NotifyStateChanged(ComboStateType from, ComboStateType to)
        {
            OnStateChanged?.Invoke(from, to);
        }

        #region 内部方法 - 状态机回调

        /// <summary>
        /// 由状态机调用 - 开始新招式
        /// </summary>
        internal void StartMove(ComboMove move)
        {
            _currentMove = move;
            _comboCount++;

            // 播放动画
            _animatorBridge.PlayMoveAnimation(move);

            // 通知外部
            OnMoveStarted?.Invoke(move);

            if (_comboCount == 1)
                OnComboStarted?.Invoke();
        }

        /// <summary>
        /// 由状态机调用 - 激活判定框
        /// </summary>
        internal void ActivateHitBoxes()
        {
            if (_currentMove?.HitBoxes == null) return;

            // 激活Collider
            _hitBoxManager.ActivateHitBoxes(_currentMove.HitBoxes);

            // 通知外部"判定框已激活" - 外部CombatSystem处理命中检测
            var hitBoxInfos = _currentMove.HitBoxes.Select(h => new HitBoxInfo
            {
                HitBoxId = h.HitBoxId,
                Collider = _hitBoxManager.GetCollider(h.HitBoxId),
                Owner = gameObject,
                MoveData = _currentMove
            }).ToArray();

            OnHitBoxesActivated?.Invoke(hitBoxInfos);
        }

        /// <summary>
        /// 由状态机调用 - 禁用判定框
        /// </summary>
        internal void DeactivateHitBoxes()
        {
            _hitBoxManager.DeactivateAllHitBoxes();
            OnHitBoxesDeactivated?.Invoke();
        }

        /// <summary>
        /// 由状态机调用 - 招式完成
        /// </summary>
        internal void CompleteMove()
        {
            OnMoveCompleted?.Invoke(_currentMove);
        }

        /// <summary>
        /// 由状态机调用 - 开启连招窗口
        /// </summary>
        internal void OpenComboWindow(float duration)
        {
            OnComboWindowOpened?.Invoke(duration);
        }

        /// <summary>
        /// 由状态机调用 - 关闭连招窗口
        /// </summary>
        internal void CloseComboWindow()
        {
            OnComboWindowClosed?.Invoke();
        }

        #endregion

        void OnDestroy()
        {
            _stateMachine?.Stop();
        }
    }
}
