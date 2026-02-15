using Asaki.Core.UI;

namespace Asaki.Unity.Services.UI
{
    /// <summary>
    /// UI输入屏蔽管理器，负责控制各层级的输入状态。
    /// </summary>
    public class UIInputBlocker
    {
        private readonly AsakiUIRoot _uiRoot;
        private int _activePopupCount;

        public UIInputBlocker(AsakiUIRoot uiRoot)
        {
            _uiRoot = uiRoot;
            _activePopupCount = 0;
        }

        /// <summary>
        /// 当窗口打开时调用，更新输入屏蔽状态。
        /// </summary>
        public void OnWindowOpened(AsakiUILayer layer)
        {
            if (layer == AsakiUILayer.Popup)
            {
                _activePopupCount++;
                if (_activePopupCount == 1)
                {
                    _uiRoot.SetLayerRaycast(AsakiUILayer.Normal, false);
                }
            }
        }

        /// <summary>
        /// 当窗口关闭时调用，更新输入屏蔽状态。
        /// </summary>
        public void OnWindowClosed(AsakiUILayer layer)
        {
            if (layer == AsakiUILayer.Popup)
            {
                _activePopupCount--;
                if (_activePopupCount <= 0)
                {
                    _activePopupCount = 0;
                    _uiRoot.SetLayerRaycast(AsakiUILayer.Normal, true);
                }
            }
        }

        /// <summary>
        /// 检查是否存在活动的 Popup 窗口。
        /// </summary>
        public bool HasActivePopup => _activePopupCount > 0;

        /// <summary>
        /// 重置状态。
        /// </summary>
        public void Reset()
        {
            _activePopupCount = 0;
            if (_uiRoot != null && !_uiRoot.Equals(null))
            {
                _uiRoot.SetLayerRaycast(AsakiUILayer.Normal, true);
            }
        }
    }
}
