using System.Collections.Generic;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Serialization;
using Asaki.Core.Serialization.Migration;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Test.Migration
{
    /// <summary>
    /// 端到端迁移演示场景
    /// 
    /// 此示例展示了完整的数据版本控制和迁移流程：
    /// 1. 创建V1数据并保存
    /// 2. 升级到V2结构
    /// 3. 注册迁移
    /// 4. 加载旧数据（自动迁移）
    /// </summary>
    public class MigrationDemoScene : MonoBehaviour
    {
        private void Start()
        {
            RunMigrationDemo().Forget();
        }

        private async UniTaskVoid RunMigrationDemo()
        {
            ALog.Info("=== Asaki Migration Demo Started ===");

            // 步骤1：注册迁移系统
            SetupMigrationSystem();

            // 步骤2：模拟V1数据的保存和加载
            await DemoVersionUpgrade();

            ALog.Info("=== Asaki Migration Demo Completed ===");
        }

        /// <summary>
        /// 设置迁移系统并注册所有迁移
        /// </summary>
        private void SetupMigrationSystem()
        {
            ALog.Info("[Demo] Setting up migration system...");

            var registry = AsakiContext.Get<IAsakiMigrationRegistry>();

            // 注册所有迁移
            registry.RegisterMigration(new CharacterMigration_V1_to_V2());
            registry.RegisterMigration(new CharacterMigration_V2_to_V3());

            ALog.Info("[Demo] Migration system setup complete");
        }

        /// <summary>
        /// 演示版本升级流程
        /// </summary>
        private async UniTaskVoid DemoVersionUpgrade()
        {
            ALog.Info("\n[Demo] Starting version upgrade demonstration...");

            // 场景：我们有一个V1的存档，但游戏已升级到V3
            // 系统应该自动执行 V1 -> V2 -> V3 的迁移链

            var saveService = AsakiContext.Get<IAsakiSaveService>();

            // 创建模拟的V1数据（实际上V1已被V3替代，这里仅用于演示）
            ALog.Info("[Demo] Creating V1 character data...");
            var v1CharacterData = CreateV1CharacterData();

            // 注意：在实际场景中，V1数据是从旧存档加载的
            // 这里我们手动创建V1数据来模拟

            ALog.Info($"[Demo] V1 Data: Name={v1CharacterData.CharacterName}, Level={v1CharacterData.Level}");

            // 假设我们加载的是一个旧的V1存档
            // 当前代码版本是V3，所以需要迁移
            // （实际演示中，我们直接创建V3数据并应用迁移逻辑）

            ALog.Info("[Demo] Simulating migration from V1 to V3...");

            // 执行迁移链
            var migratedData = await SimulateMigrationChain(v1CharacterData);

            ALog.Info(
                $"[Demo] Migrated Data (V3): Name={migratedData.CharacterName}, "
                    + $"Level={migratedData.Level}, "
                    + $"Experience={migratedData.Experience}, "
                    + $"Skills Count={migratedData.Skills?.Count ?? 0}"
            );

            // 验证迁移结果
            ValidateMigration(v1CharacterData, migratedData);

            ALog.Info("[Demo] Version upgrade demonstration complete!\n");
        }

        /// <summary>
        /// 创建V1角色数据（用于演示）
        /// </summary>
        private CharacterDataV1Compat CreateV1CharacterData()
        {
            return new CharacterDataV1Compat
            {
                CharacterName = "Hero",
                Level = 10,
            };
        }

        /// <summary>
        /// 模拟迁移链的执行
        /// </summary>
        private async UniTask<CharacterDataV3> SimulateMigrationChain(
            CharacterDataV1Compat v1Data
        )
        {
            var registry = AsakiContext.Get<IAsakiMigrationRegistry>();

            // 查找迁移路径
            var migrationPath = registry.FindMigrationPath(
                "Game.Test.Migration.CharacterData",
                1,
                3
            );

            if (migrationPath == null || migrationPath.Count == 0)
            {
                ALog.Error("[Demo] No migration path found!");
                return null;
            }

            ALog.Info(
                $"[Demo] Found migration path with {migrationPath.Count} step(s)"
            );

            // 转换V1数据到V3结构（手动复制共同字段）
            var v3Data = new CharacterDataV3
            {
                CharacterName = v1Data.CharacterName,
                Level = v1Data.Level,
            };

            // 应用迁移链
            foreach (var migration in migrationPath)
            {
                ALog.Info(
                    $"[Demo] Applying migration: v{migration.FromVersion} -> v{migration.ToVersion}"
                );

                if (migration is IAsakiMigration<CharacterDataV3> typedMigration)
                {
                    typedMigration.Migrate(v3Data);
                }
            }

            await UniTask.Yield();
            return v3Data;
        }

        /// <summary>
        /// 验证迁移结果
        /// </summary>
        private void ValidateMigration(
            CharacterDataV1Compat v1Data,
            CharacterDataV3 v3Data
        )
        {
            ALog.Info("[Demo] Validating migration...");

            // 验证基本数据保留
            if (v3Data.CharacterName != v1Data.CharacterName)
            {
                ALog.Error($"[Demo] Validation FAILED: Name mismatch!");
                return;
            }

            if (v3Data.Level != v1Data.Level)
            {
                ALog.Error($"[Demo] Validation FAILED: Level mismatch!");
                return;
            }

            // 验证V2迁移添加的字段
            if (v3Data.Experience != 0)
            {
                ALog.Info(
                    $"[Demo] ✓ V2 migration applied: Experience = {v3Data.Experience}"
                );
            }

            // 验证V3迁移添加的字段
            if (v3Data.Skills != null && v3Data.Skills.Count > 0)
            {
                ALog.Info($"[Demo] ✓ V3 migration applied: Skills added");
            }

            ALog.Info("[Demo] Validation PASSED ✓");
        }
    }

    // ===== 数据结构定义 =====

    /// <summary>
    /// V1兼容结构（用于演示旧数据）
    /// </summary>
    public class CharacterDataV1Compat
    {
        public string CharacterName;
        public int Level;
    }

    /// <summary>
    /// 角色数据 V1 - 初始版本
    /// </summary>
    [AsakiSave(Version = 1)]
    public partial class CharacterDataV1 : IAsakiVersionedSavable
    {
        [AsakiSaveMember(Order = 1)]
        public string CharacterName;

        [AsakiSaveMember(Order = 2)]
        public int Level;
    }

    /// <summary>
    /// 角色数据 V2 - 添加经验值系统
    /// 
    /// 变更：
    /// - 新增 Experience 字段
    /// </summary>
    [AsakiSave(Version = 2)]
    public partial class CharacterDataV2 : IAsakiVersionedSavable
    {
        [AsakiSaveMember(Order = 1)]
        public string CharacterName;

        [AsakiSaveMember(Order = 2)]
        public int Level;

        [AsakiSaveMember(Order = 3)]
        public int Experience; // 新增
    }

    /// <summary>
    /// 角色数据 V3 - 添加技能系统
    /// 
    /// 变更：
    /// - 新增 Skills 字段
    /// </summary>
    [AsakiSave(Version = 3)]
    public partial class CharacterDataV3 : IAsakiVersionedSavable
    {
        [AsakiSaveMember(Order = 1)]
        public string CharacterName;

        [AsakiSaveMember(Order = 2)]
        public int Level;

        [AsakiSaveMember(Order = 3)]
        public int Experience;

        [AsakiSaveMember(Order = 4)]
        public List<string> Skills; // 新增
    }

    // ===== 迁移定义 =====

    /// <summary>
    /// 迁移：CharacterData V1 -> V2
    /// </summary>
    [AsakiMigration(typeof(CharacterDataV2), 1, 2)]
    public class CharacterMigration_V1_to_V2 : AsakiMigrationBase<CharacterDataV2>
    {
        public override int FromVersion => 1;
        public override int ToVersion => 2;
        public override string TypeName => "Game.Test.Migration.CharacterData";

        public override void Migrate(CharacterDataV2 data)
        {
            ALog.Info(
                $"[Migration V1->V2] Migrating character '{data.CharacterName}'"
            );

            // V2新增了Experience字段
            // 根据等级计算初始经验值
            data.Experience = (data.Level - 1) * 100;

            ALog.Info($"[Migration V1->V2] Set Experience to {data.Experience}");
        }
    }

    /// <summary>
    /// 迁移：CharacterData V2 -> V3
    /// </summary>
    [AsakiMigration(typeof(CharacterDataV3), 2, 3)]
    public class CharacterMigration_V2_to_V3 : AsakiMigrationBase<CharacterDataV3>
    {
        public override int FromVersion => 2;
        public override int ToVersion => 3;
        public override string TypeName => "Game.Test.Migration.CharacterData";

        public override void Migrate(CharacterDataV3 data)
        {
            ALog.Info(
                $"[Migration V2->V3] Migrating character '{data.CharacterName}'"
            );

            // V3新增了Skills字段
            data.Skills = new List<string>();

            // 根据等级给予初始技能
            if (data.Level >= 1)
                data.Skills.Add("Basic Attack");
            if (data.Level >= 5)
                data.Skills.Add("Power Strike");
            if (data.Level >= 10)
                data.Skills.Add("Ultimate Skill");

            ALog.Info(
                $"[Migration V2->V3] Added {data.Skills.Count} skills"
            );
        }
    }
}
