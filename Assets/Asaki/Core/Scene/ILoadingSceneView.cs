using System;

namespace Asaki.Core.Scene
{
    /// <summary>
    /// 加载场景视图接口
    /// 由游戏开发者在 Game 程序集中实现，用于解耦 LoadingSceneController 与具体 UI
    /// </summary>
    public interface ILoadingSceneView
    {
        /// <summary>
        /// 更新加载进度
        /// </summary>
        /// <param name="progress">进度值 0.0 ~ 1.0</param>
        void UpdateProgress(float progress);

        /// <summary>
        /// 更新提示文本
        /// </summary>
        /// <param name="message">提示信息</param>
        void UpdateTip(string message);

        /// <summary>
        /// 显示错误信息
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        void ShowError(string errorMessage);

        /// <summary>
        /// 显示加载视图
        /// </summary>
        void Show();

        /// <summary>
        /// 隐藏加载视图
        /// </summary>
        void Hide();
    }
}
