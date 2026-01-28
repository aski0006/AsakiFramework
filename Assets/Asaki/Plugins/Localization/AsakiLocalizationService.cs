using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Configs;
using Asaki.Core.Configuration;
using Asaki.Core.Context;
using Asaki.Core.Logging;

namespace Asaki.Plugins.Localization
{
    public struct OnAsakiLanguageChangedEvent : IAsakiEvent
    {
        public string LanguageCode { get; set; }
    }

    public class AsakiLocalizationService : IAsakiService, IDisposable
    {
        private readonly IAsakiEventService _asakiEventService;
        private readonly IAsakiConfigService _asakiConfigService;
        private Dictionary<string, string> _runtimeDict;
        private string _currentLanguage;

        public AsakiLocalizationService(
            IAsakiEventService asakiEventService,
            IAsakiConfigService asakiConfigService,
            AsakiLocalizationConfig cfg
        )
        {
            _runtimeDict = new Dictionary<string, string>();
            _asakiEventService = asakiEventService;
            _asakiConfigService = asakiConfigService;
            _currentLanguage = cfg.FallbackLanguage;
        }

        public void OnInit()
        {
            ALog.Info($"[Localization] Initializing language: {_currentLanguage}");
            LoadLanguage(_currentLanguage);
        }

        public void SwitchLanguage(string langCode)
        {
            if (_currentLanguage == langCode && _runtimeDict != null)
                return;

            _currentLanguage = langCode;
            LoadLanguage(langCode);
            _asakiEventService.Publish(new OnAsakiLanguageChangedEvent { LanguageCode = langCode });
        }

        private void LoadLanguage(string langCode)
        {
            var items = _asakiConfigService.Where<LocalizationTable>(x =>
                x.LanguageCode == langCode
            );
            if (items.Count == 0)
            {
                ALog.Warn($"[Localization] No entries found for language '{langCode}'");
            }
            _runtimeDict = new Dictionary<string, string>(items.Count);
            foreach (LocalizationTable item in items)
            {
                _runtimeDict.TryAdd(item.Key, item.Content);
            }
            ALog.Info($"[Localization] Loaded {items.Count} keys for '{langCode}'");
        }

        public string GetText(string key)
        {
            if (_runtimeDict != null && _runtimeDict.TryGetValue(key, out string text))
            {
                return text;
            }
            return null;
        }

        public void Dispose()
        {
            _runtimeDict.Clear();
            _runtimeDict = null;
            ALog.Trace($"[Localization] Disposed, Clear Runtime Dictionary");
        }
    }
}
