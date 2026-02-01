using Asaki.Core.Attributes;
using Asaki.Core.Configuration;

namespace Game.Scripts.Data
{
    [AsakiSave]
    [AsakiConfig(LoadStrategy = AsakiConfigLoadStrategy.Preload, Priority = 100)]
    public partial class CardData : IAsakiConfig
    {
        [AsakiSaveMember(order: 0)]
        public int Id { get; private set; }

        [AsakiSaveMember(order: 1)]
        public int Cost { get; private set; }

        [AsakiSaveMember(order: 2)]
        public int Atk { get; private set; }

        [AsakiSaveMember(order: 3)]
        public int Def { get; private set; }

        [AsakiSaveMember(order: 4)]
        public string CardSpriteAssetKey { get; private set; }

        [AsakiSaveMember(order: 5)]
        public string CardDescription { get; private set; }
    }
}
