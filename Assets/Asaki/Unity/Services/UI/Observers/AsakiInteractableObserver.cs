using UnityEngine.UI;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// 将 bool 属性绑定到 Selectable 的 interactable 状态。
    /// </summary>
    public class AsakiInteractableObserver : AsakiObserverBase<bool, Selectable>
    {
        public AsakiInteractableObserver(Selectable selectable)
            : base(selectable) { }

        protected override void ApplyValue(bool value)
        {
            if (_target.interactable != value)
            {
                _target.interactable = value;
            }
        }
    }
}
