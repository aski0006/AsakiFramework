using UnityEngine;

namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 动画桥接 - 控制动画播放
    /// </summary>
    public class ComboAnimatorBridge : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        private int _lastPlayedHash;

        public void Initialize(Animator targetAnimator)
        {
            animator = targetAnimator;
        }

        /// <summary>
        /// 播放招式动画
        /// </summary>
        public void PlayMoveAnimation(ComboMove move)
        {
            if (animator == null || string.IsNullOrEmpty(move.AnimationStateName))
                return;

            int stateHash = Animator.StringToHash(move.AnimationStateName);
            _lastPlayedHash = stateHash;

            animator.speed = move.AnimationSpeed;
            animator.CrossFade(stateHash, 0.1f);
        }

        /// <summary>
        /// 重置动画速度
        /// </summary>
        public void ResetAnimationSpeed()
        {
            if (animator != null)
            {
                animator.speed = 1f;
            }
        }

        /// <summary>
        /// 中断动画
        /// </summary>
        public void InterruptAnimation()
        {
            ResetAnimationSpeed();
        }

        /// <summary>
        /// 获取Animator
        /// </summary>
        public Animator GetAnimator() => animator;
    }
}
