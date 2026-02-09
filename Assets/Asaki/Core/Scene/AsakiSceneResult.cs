namespace Asaki.Core.Scene
{
    public readonly struct AsakiSceneResult
    {
        public readonly bool Success;
        public readonly string SceneName;
        public readonly string ErrorMessage;

        /// <summary>
        /// 是否加载成功
        /// </summary>
        public bool IsSuccess => Success;

        public AsakiSceneResult(bool success, string sceneName, string errorMessage = null)
        {
            Success = success;
            SceneName = sceneName;
            ErrorMessage = errorMessage;
        }

        public static AsakiSceneResult Ok(string sceneName)
        {
            return new AsakiSceneResult(true, sceneName);
        }

        public static AsakiSceneResult Failed(string sceneName, string errorMessage = null)
        {
            return new AsakiSceneResult(false, sceneName, errorMessage);
        }

        public static AsakiSceneResult OperationCanceled(
            string sceneName,
            string errorMessage = null
        )
        {
            return new AsakiSceneResult(false, sceneName, "Operation canceled.");
        }
    }
}
