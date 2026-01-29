using System;

namespace Asaki.Core.Pooling
{
    [Serializable]
    public class AsakiPoolConfig
    {
        public int InitialSize = 10;
        public int MaxSize = 100;
        public bool EnableValidation = true;
        public bool EnableCollectionCheck = true;
        public bool AllowSyncCreation = false;
        public float ExpireTime = 5f;

        public static AsakiPoolConfig Default =>
            new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = 0,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = false,
                ExpireTime = 0f,
            };

        public static AsakiPoolConfig ForGameObject(int initialSize = 10, int maxSize = 100)
        {
            return new AsakiPoolConfig
            {
                InitialSize = initialSize,
                MaxSize = maxSize,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = false,
            };
        }

        public static AsakiPoolConfig ForLightWeightObject(int maxSize = 1024)
        {
            return new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = maxSize,
                EnableValidation = false,
                EnableCollectionCheck = false,
                AllowSyncCreation = true,
            };
        }
    }
}
