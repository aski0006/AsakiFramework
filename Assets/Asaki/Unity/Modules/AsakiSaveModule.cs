using System;
using System.IO;
using Asaki.Core.Attributes;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.DataTable;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(100, typeof(AsakiEventBusModule), typeof(AsakiConfigModule))]
    public class AsakiSaveModule : IAsakiModule
    {
        private IAsakiSaveService _asakiSaveService;
        private IAsakiEventService _eventService;
        private AsakiSaveConfig _saveConfig;

        [AsakiInject]
        public void Init(IAsakiEventService eventService)
        {
            _eventService = eventService;

            // 从配置服务获取存档配置，如果不存在则使用默认配置
            if (AsakiContext.TryGet(out AsakiFrameworkSetting asakiConfig))
            {
                _saveConfig = asakiConfig.SaveConfig;
            }
            _saveConfig ??= new AsakiSaveConfig();
        }

        public void OnInit()
        {
            _asakiSaveService = new AsakiSaveService(_eventService, _saveConfig);
            _asakiSaveService.OnInit();
            AsakiContext.Register(_asakiSaveService);

            // 设置深度克隆委托，支持 IAsakiSavable 类型的深度克隆
            AsakiValue<IAsakiSavable>.DeepCloneSavableFunc = DeepCloneSavable;
        }

        public async UniTask OnInitAsync()
        {
            await _asakiSaveService.OnInitAsync();
        }

        public void OnDispose()
        {
            _asakiSaveService.OnDispose();
            AsakiValue<IAsakiSavable>.DeepCloneSavableFunc = null;
        }

        /// <summary>
        /// 深度克隆 IAsakiSavable 对象
        /// </summary>
        private static IAsakiSavable DeepCloneSavable(IAsakiSavable source)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            var cloned = (IAsakiSavable)Activator.CreateInstance(type);

            using (var stream = new MemoryStream())
            {
                var writer = new AsakiBinaryWriter(stream, true);
                source.Serialize(writer);
                writer.Dispose();

                stream.Position = 0;
                var reader = new AsakiBinaryReader(stream, true);
                cloned.Deserialize(reader);
                reader.Dispose();
            }

            return cloned;
        }
    }
}
