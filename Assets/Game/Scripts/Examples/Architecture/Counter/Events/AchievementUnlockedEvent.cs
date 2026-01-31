using Asaki.Core.Broker;

namespace Game.Scripts.Examples.Architecture.Counter.Events
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
