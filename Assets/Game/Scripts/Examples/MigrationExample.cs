using System.Collections.Generic;
using Asaki.Core.Attributes;
using Asaki.Core.Serialization;
using Asaki.Core.Serialization.Migration;

namespace Game.Test.Migration
{
    /// <summary>
    /// 示例：玩家数据 V1 - 原始版本
    /// </summary>
    /// <remarks>
    /// 这是初始版本，只包含基本的玩家信息。
    /// </remarks>
    [AsakiSave(Version = 1)]
    public partial class PlayerDataV1 : IAsakiVersionedSavable
    {
        [AsakiSaveMember(Order = 1)]
        public string PlayerName;

        [AsakiSaveMember(Order = 2)]
        public int Level;

        [AsakiSaveMember(Order = 3)]
        public int Experience;
    }

    /// <summary>
    /// 示例：玩家数据 V2 - 添加了金币系统
    /// </summary>
    /// <remarks>
    /// 第二版添加了金币字段。
    /// 需要从V1迁移：默认金币为0。
    /// </remarks>
    [AsakiSave(Version = 2)]
    public partial class PlayerDataV2 : IAsakiVersionedSavable
    {
        [AsakiSaveMember(Order = 1)]
        public string PlayerName;

        [AsakiSaveMember(Order = 2)]
        public int Level;

        [AsakiSaveMember(Order = 3)]
        public int Experience;

        [AsakiSaveMember(Order = 4)]
        public int Gold; // 新增字段
    }

    /// <summary>
    /// 示例：玩家数据 V3 - 添加了装备系统
    /// </summary>
    /// <remarks>
    /// 第三版添加了装备列表。
    /// 需要从V2迁移：装备列表初始化为空列表。
    /// </remarks>
    [AsakiSave(Version = 3)]
    public partial class PlayerDataV3 : IAsakiVersionedSavable
    {
        [AsakiSaveMember(Order = 1)]
        public string PlayerName;

        [AsakiSaveMember(Order = 2)]
        public int Level;

        [AsakiSaveMember(Order = 3)]
        public int Experience;

        [AsakiSaveMember(Order = 4)]
        public int Gold;

        [AsakiSaveMember(Order = 5)]
        public List<string> Equipment = new List<string>(); // 新增字段
    }

    /// <summary>
    /// 示例迁移：PlayerData V1 -> V2
    /// </summary>
    /// <remarks>
    /// 这个迁移负责将V1数据升级到V2。
    /// 主要变更：添加Gold字段，默认值为0。
    /// </remarks>
    [AsakiMigration(typeof(PlayerDataV2), 1, 2)]
    public class PlayerDataMigration_V1_to_V2 : AsakiMigrationBase<PlayerDataV2>
    {
        public override int FromVersion => 1;
        public override int ToVersion => 2;

        public override void Migrate(PlayerDataV2 data)
        {
            // V1没有Gold字段，使用默认值0
            data.Gold = 0;

            // 注意：PlayerName, Level, Experience已经通过反序列化自动填充
            // 我们只需要处理新增的字段
        }
    }

    /// <summary>
    /// 示例迁移：PlayerData V2 -> V3
    /// </summary>
    /// <remarks>
    /// 这个迁移负责将V2数据升级到V3。
    /// 主要变更：添加Equipment列表，初始化为空。
    /// </remarks>
    [AsakiMigration(typeof(PlayerDataV3), 2, 3)]
    public class PlayerDataMigration_V2_to_V3 : AsakiMigrationBase<PlayerDataV3>
    {
        public override int FromVersion => 2;
        public override int ToVersion => 3;

        public override void Migrate(PlayerDataV3 data)
        {
            // V2没有Equipment字段，初始化为空列表
            if (data.Equipment == null)
            {
                data.Equipment = new List<string>();
            }

            // 可选：根据等级给予初始装备
            if (data.Level >= 10)
            {
                data.Equipment.Add("Starter Sword");
                data.Equipment.Add("Leather Armor");
            }
        }
    }

    /// <summary>
    /// 示例：直接迁移 V1 -> V3（跳过V2）
    /// </summary>
    /// <remarks>
    /// 这是一个快捷迁移路径，允许直接从V1跳到V3。
    /// 虽然系统可以自动链式执行V1->V2->V3，但提供直接路径可以提高性能。
    /// </remarks>
    [AsakiMigration(typeof(PlayerDataV3), 1, 3)]
    public class PlayerDataMigration_V1_to_V3_Direct : AsakiMigrationBase<PlayerDataV3>
    {
        public override int FromVersion => 1;
        public override int ToVersion => 3;

        public override void Migrate(PlayerDataV3 data)
        {
            // 直接从V1迁移到V3，需要同时处理V2和V3的变更
            data.Gold = 0; // V2的变更
            data.Equipment = new List<string>(); // V3的变更

            // 根据等级给予装备
            if (data.Level >= 10)
            {
                data.Equipment.Add("Starter Sword");
                data.Equipment.Add("Leather Armor");
            }
        }
    }
}
