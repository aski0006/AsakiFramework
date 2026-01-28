using Asaki.Core.Broker;

namespace Asaki.Core.Scene
{
    public struct AsakiSceneProgressEvent : IAsakiEvent
    {
        public readonly string SceneName;
        public readonly float Progress; // 0.0 ~ 1.0

        public AsakiSceneProgressEvent(string sceneName, float progress)
        {
            SceneName = sceneName;
            Progress = progress;
        }
    }

    public readonly struct AsakiSceneStateEvent : IAsakiEvent
    {
        public enum State
        {
            Started,
            Completed,
            Failed,
            Cancelled,
        }

        public readonly string SceneName;
        public readonly State CurrentState;
        public readonly string ErrorMessage;

        public AsakiSceneStateEvent(string sceneName, State state, string error = null)
        {
            SceneName = sceneName;
            CurrentState = state;
            ErrorMessage = error;
        }
    }
}
