using NUnit.Framework;
using Asaki.Core.Serialization;
using Asaki.Core.Serialization.Migration;
using System.IO;
using System.Collections.Generic;

namespace Tests.Serialization
{
    /// <summary>
    /// 数据版本控制与迁移系统的单元测试
    /// </summary>
    [TestFixture]
    public class AsakiMigrationTests
    {
        private AsakiMigrationRegistry _registry;

        [SetUp]
        public void Setup()
        {
            _registry = new AsakiMigrationRegistry();
        }

        /// <summary>
        /// 测试：注册迁移
        /// </summary>
        [Test]
        public void TestRegisterMigration()
        {
            var migration = new TestMigration_V1_to_V2();
            _registry.RegisterMigration(migration);

            var migrations = _registry.GetMigrations("Test.TestData");
            Assert.AreEqual(1, migrations.Count);
            Assert.AreEqual(migration, migrations[0]);
        }

        /// <summary>
        /// 测试：查找简单迁移路径（单步）
        /// </summary>
        [Test]
        public void TestFindMigrationPath_SingleStep()
        {
            var migration = new TestMigration_V1_to_V2();
            _registry.RegisterMigration(migration);

            var path = _registry.FindMigrationPath("Test.TestData", 1, 2);
            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(migration, path[0]);
        }

        /// <summary>
        /// 测试：查找链式迁移路径（多步）
        /// </summary>
        [Test]
        public void TestFindMigrationPath_MultiStep()
        {
            var migration1 = new TestMigration_V1_to_V2();
            var migration2 = new TestMigration_V2_to_V3();

            _registry.RegisterMigration(migration1);
            _registry.RegisterMigration(migration2);

            // 查找 V1 -> V3 的路径
            var path = _registry.FindMigrationPath("Test.TestData", 1, 3);
            Assert.IsNotNull(path);
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(migration1, path[0]);
            Assert.AreEqual(migration2, path[1]);
        }

        /// <summary>
        /// 测试：优先选择直接路径而非链式路径
        /// </summary>
        [Test]
        public void TestFindMigrationPath_DirectPathPreferred()
        {
            var migration1 = new TestMigration_V1_to_V2();
            var migration2 = new TestMigration_V2_to_V3();
            var directMigration = new TestMigration_V1_to_V3_Direct();

            _registry.RegisterMigration(migration1);
            _registry.RegisterMigration(migration2);
            _registry.RegisterMigration(directMigration);

            // 查找 V1 -> V3 的路径，应该选择直接路径
            var path = _registry.FindMigrationPath("Test.TestData", 1, 3);
            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(directMigration, path[0]);
        }

        /// <summary>
        /// 测试：版本相同时无需迁移
        /// </summary>
        [Test]
        public void TestFindMigrationPath_SameVersion()
        {
            var path = _registry.FindMigrationPath("Test.TestData", 2, 2);
            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }

        /// <summary>
        /// 测试：找不到迁移路径时返回null
        /// </summary>
        [Test]
        public void TestFindMigrationPath_NoPathFound()
        {
            var migration = new TestMigration_V1_to_V2();
            _registry.RegisterMigration(migration);

            // 尝试查找 V1 -> V3，但只有 V1 -> V2 的迁移
            var path = _registry.FindMigrationPath("Test.TestData", 1, 3);
            Assert.IsNull(path);
        }

        /// <summary>
        /// 测试：HasMigrationPath 方法
        /// </summary>
        [Test]
        public void TestHasMigrationPath()
        {
            var migration = new TestMigration_V1_to_V2();
            _registry.RegisterMigration(migration);

            Assert.IsTrue(_registry.HasMigrationPath("Test.TestData", 1, 2));
            Assert.IsFalse(_registry.HasMigrationPath("Test.TestData", 1, 3));
        }

        /// <summary>
        /// 测试：强类型迁移执行
        /// </summary>
        [Test]
        public void TestTypedMigration_Execution()
        {
            var data = new TestDataV2 { Name = "Test", Value = 100 };
            var migration = new TestMigration_V1_to_V2();

            migration.Migrate(data);

            // 验证迁移设置了新字段
            Assert.AreEqual(0, data.NewField);
        }

        /// <summary>
        /// 测试：版本元数据
        /// </summary>
        [Test]
        public void TestVersionMetadata()
        {
            var metadata = new AsakiVersionMetadata("Test.TestData", 1);

            Assert.AreEqual("Test.TestData", metadata.TypeName);
            Assert.AreEqual(1, metadata.Version);
            Assert.IsTrue(metadata.IsVersionMatch(1));
            Assert.IsFalse(metadata.IsVersionMatch(2));
            Assert.IsTrue(metadata.RequiresMigration(2));
            Assert.IsFalse(metadata.RequiresMigration(1));
        }
    }

    // ===== 测试用的数据类和迁移类 =====

    /// <summary>
    /// 测试数据 V1
    /// </summary>
    public class TestDataV1 : IAsakiSavable, IAsakiVersionedSavable
    {
        public string Name;
        public int Value;

        public void Serialize(IAsakiWriter writer)
        {
            writer.BeginObject("TestData");
            writer.WriteString("Name", Name);
            writer.WriteInt("Value", Value);
            writer.EndObject();
        }

        public void Deserialize(IAsakiReader reader)
        {
            Name = reader.ReadString("Name");
            Value = reader.ReadInt("Value");
        }

        public int GetDataVersion() => 1;
    }

    /// <summary>
    /// 测试数据 V2 - 添加了NewField
    /// </summary>
    public class TestDataV2 : IAsakiSavable, IAsakiVersionedSavable
    {
        public string Name;
        public int Value;
        public int NewField; // 新增字段

        public void Serialize(IAsakiWriter writer)
        {
            writer.BeginObject("TestData");
            writer.WriteString("Name", Name);
            writer.WriteInt("Value", Value);
            writer.WriteInt("NewField", NewField);
            writer.EndObject();
        }

        public void Deserialize(IAsakiReader reader)
        {
            Name = reader.ReadString("Name");
            Value = reader.ReadInt("Value");
            NewField = reader.ReadInt("NewField");
        }

        public int GetDataVersion() => 2;
    }

    /// <summary>
    /// 测试数据 V3 - 添加了Items列表
    /// </summary>
    public class TestDataV3 : IAsakiSavable, IAsakiVersionedSavable
    {
        public string Name;
        public int Value;
        public int NewField;
        public List<string> Items; // 新增字段

        public void Serialize(IAsakiWriter writer)
        {
            writer.BeginObject("TestData");
            writer.WriteString("Name", Name);
            writer.WriteInt("Value", Value);
            writer.WriteInt("NewField", NewField);

            writer.BeginList("Items", Items?.Count ?? 0);
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    writer.WriteString("Item", item);
                }
            }
            writer.EndList();

            writer.EndObject();
        }

        public void Deserialize(IAsakiReader reader)
        {
            Name = reader.ReadString("Name");
            Value = reader.ReadInt("Value");
            NewField = reader.ReadInt("NewField");

            int count = reader.BeginList("Items");
            Items = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                Items.Add(reader.ReadString("Item"));
            }
            reader.EndList();
        }

        public int GetDataVersion() => 3;
    }

    /// <summary>
    /// 迁移：V1 -> V2
    /// </summary>
    public class TestMigration_V1_to_V2 : AsakiMigrationBase<TestDataV2>
    {
        public override int FromVersion => 1;
        public override int ToVersion => 2;
        public override string TypeName => "Test.TestData";

        public override void Migrate(TestDataV2 data)
        {
            // 设置新字段的默认值
            data.NewField = 0;
        }
    }

    /// <summary>
    /// 迁移：V2 -> V3
    /// </summary>
    public class TestMigration_V2_to_V3 : AsakiMigrationBase<TestDataV3>
    {
        public override int FromVersion => 2;
        public override int ToVersion => 3;
        public override string TypeName => "Test.TestData";

        public override void Migrate(TestDataV3 data)
        {
            // 初始化新字段
            data.Items = new List<string>();
        }
    }

    /// <summary>
    /// 直接迁移：V1 -> V3
    /// </summary>
    public class TestMigration_V1_to_V3_Direct : AsakiMigrationBase<TestDataV3>
    {
        public override int FromVersion => 1;
        public override int ToVersion => 3;
        public override string TypeName => "Test.TestData";

        public override void Migrate(TestDataV3 data)
        {
            // 同时处理V2和V3的变更
            data.NewField = 0;
            data.Items = new List<string>();
        }
    }
}
