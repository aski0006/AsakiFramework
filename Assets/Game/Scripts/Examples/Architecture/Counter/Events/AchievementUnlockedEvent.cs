using Asaki.Core.Broker;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples.Architecture.Counter.Events
{
    public struct AchievementUnlockedEvent : IAsakiEvent
    {
        public string AchievementName;
        public int Timestamp;

        public AchievementUnlockedEvent(string name)
        {
            AchievementName = name;
            Timestamp = UnityEngine.Time.frameCount;
        }
    }
}
