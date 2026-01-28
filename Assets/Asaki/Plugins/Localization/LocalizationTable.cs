using Asaki.Core.Attributes;
using Asaki.Core.Configuration;

namespace Asaki.Plugins.Localization
{
    [AsakiSave]
    [AsakiConfig(LoadStrategy = AsakiConfigLoadStrategy.OnDemand)]
    public partial class LocalizationTable : IAsakiConfig
    {
        [AsakiSaveMember]
        public int Id { get; private set; }

        [AsakiSaveMember]
        public string Key { get; private set; }

        [AsakiSaveMember]
        public string LanguageCode { get; private set; }

        [AsakiSaveMember]
        public string Content { get; private set; }

        [AsakiSaveMember]
        public string Remark { get; private set; } // 备注
    }
}
