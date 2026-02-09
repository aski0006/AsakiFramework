using System.Collections.Generic;
using System.Text;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Unity.Services.Serialization;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples
{
    [AsakiSave]
    public partial class TestConfig
    {
        [AsakiSaveMember]
        public string ServerName;

        [AsakiSaveMember]
        public int MaxPlayers;
        //
        // public void Serialize(IAsakiWriter writer)
        // {
        //     writer.BeginObject("TestConfig"); // 虽然 AsakiJsonWriter.WriteObject 内部已经 Begin 了，但为了模拟 Roslyn 生成的代码，通常内部不重复 Begin。
        //     // *修正*: AsakiJsonWriter 的 WriteObject<T> 实现是：BeginObject -> value.Serialize -> EndObject。
        //     // 所以在这里面我们只需要写字段。
        //
        //     writer.WriteString("ServerName", ServerName);
        //     writer.WriteInt("MaxPlayers", MaxPlayers);
        //
        //     // 注意：这里不需要再调用 EndObject，因为 WriteObject 负责闭合。
        //     // 但如果是 Root 对象（如上面的 TestPayload），通常外部没有 WriteObject 包裹，需要自己处理。
        //     // 这里为了测试简单，我们在 WriteObject 内部实现中已经处理了结构。
        // }
        //
        // public void Deserialize(IAsakiReader reader)
        // {
        //     ServerName = reader.ReadString("ServerName");
        //     MaxPlayers = reader.ReadInt("MaxPlayers");
        // }
    }

    [AsakiSave]
    public partial class TestPayload
    {
        [AsakiSaveMember]
        public string PlayerName;

        [AsakiSaveMember]
        public int Level;

        [AsakiSaveMember]
        public Vector3 Position;

        [AsakiSaveMember]
        public bool IsVip;

        [AsakiSaveMember]
        public List<int> InventoryIds;

        [AsakiSaveMember]
        public TestConfig Config;
        //
        // public void Serialize(IAsakiWriter writer)
        // {
        // 	writer.BeginObject("TestPayload");
        // 	writer.WriteString("PlayerName", PlayerName);
        // 	writer.WriteInt("Level", Level);
        // 	writer.WriteVector3("Position", Position);
        // 	writer.WriteBool("IsVip", IsVip);
        //
        // 	// List
        // 	writer.BeginList("InventoryIds", InventoryIds.Count);
        // 	foreach (var id in InventoryIds)
        // 	{
        // 		writer.WriteInt("Item", id); // JSON Writer 会忽略 "Item" key，直接写数组元素
        // 	}
        // 	writer.EndList();
        //
        // 	// Nested Object
        // 	writer.WriteObject("Config", Config);
        //
        // 	writer.EndObject();
        // }
        //
        // public void Deserialize(IAsakiReader reader)
        // {
        // 	PlayerName = reader.ReadString("PlayerName");
        // 	Level = reader.ReadInt("Level");
        // 	Position = reader.ReadVector3("Position");
        // 	IsVip = reader.ReadBool("IsVip");
        //
        // 	// List 读取逻辑 (关键测试点)
        // 	int count = reader.BeginList("InventoryIds");
        // 	InventoryIds = new List<int>();
        // 	for (int i = 0; i < count; i++)
        // 	{
        // 		// 在 List 模式下，Key "Item" 应该被忽略，自动读取下一个元素
        // 		InventoryIds.Add(reader.ReadInt("Item"));
        // 	}
        // 	reader.EndList();
        //
        // 	// Nested Object 读取逻辑
        // 	Config = reader.ReadObject<TestConfig>("Config");
        // }
    }

    public class AsakiJsonTest : MonoBehaviour
    {
        [ContextMenu("Run Json Test")]
        void Start()
        {
            Debug.Log("<b>[AsakiJsonTest]</b> Start Testing...");

            // 1. 构建测试数据
            var original = new TestPayload
            {
                PlayerName = "Asaki_Tester",
                Level = 99,
                Position = new Vector3(12.5f, 5.0f, -10.2f),
                IsVip = true,
                // 测试列表
                InventoryIds = new List<int> { 1001, 1002, 5005, 8888 },
                // 测试嵌套对象
                Config = new TestConfig { ServerName = "Shanghai_01", MaxPlayers = 200 },
            };

            // 2. 序列化 (Object -> Json String)
            var sb = new StringBuilder();
            var writer = new AsakiJsonWriter(sb);
            original.Serialize(writer); // 模拟生成的代码调用
            string jsonOutput = writer.GetResult();

            Debug.Log($"<b>[JSON Output]:</b>\n{jsonOutput}");

            // 3. 反序列化 (Json String -> Object)
            // 使用我们新写的 Reader
            var reader = AsakiJsonReader.FromJson(jsonOutput);
            var deserialized = new TestPayload();
            deserialized.Deserialize(reader); // 模拟生成的代码调用

            // 4. 验证对比
            bool isSuccess = true;
            isSuccess &= Assert("Name", original.PlayerName, deserialized.PlayerName);
            isSuccess &= Assert("Level", original.Level, deserialized.Level);
            isSuccess &= Assert("Position", original.Position, deserialized.Position);
            isSuccess &= Assert("IsVip", original.IsVip, deserialized.IsVip);

            // 验证列表
            isSuccess &= Assert(
                "List Count",
                original.InventoryIds.Count,
                deserialized.InventoryIds.Count
            );
            if (
                deserialized.InventoryIds != null
                && deserialized.InventoryIds.Count == original.InventoryIds.Count
            )
            {
                for (int i = 0; i < original.InventoryIds.Count; i++)
                {
                    isSuccess &= Assert(
                        $"List Item [{i}]",
                        original.InventoryIds[i],
                        deserialized.InventoryIds[i]
                    );
                }
            }

            // 验证嵌套对象
            if (deserialized.Config == null)
            {
                Debug.LogError("[AsakiJsonTest] X Nested Object is NULL!");
                isSuccess = false;
            }
            else
            {
                isSuccess &= Assert(
                    "Nested ServerName",
                    original.Config.ServerName,
                    deserialized.Config.ServerName
                );
                isSuccess &= Assert(
                    "Nested MaxPlayers",
                    original.Config.MaxPlayers,
                    deserialized.Config.MaxPlayers
                );
            }

            if (isSuccess)
            {
                Debug.Log(
                    "<color=green><b>[AsakiJsonTest] ALL PASSED! Asaki Network is ready.</b></color>"
                );
            }
            else
            {
                Debug.LogError("[AsakiJsonTest] Some tests failed.");
            }
        }

        private bool Assert<T>(string fieldName, T expected, T actual)
        {
            bool match = EqualityComparer<T>.Default.Equals(expected, actual);
            if (!match)
            {
                Debug.LogError($"[Fail] {fieldName}: Expected '{expected}', got '{actual}'");
            }
            return match;
        }
    }
}
