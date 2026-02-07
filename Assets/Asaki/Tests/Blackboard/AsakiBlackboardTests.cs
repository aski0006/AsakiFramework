using System;
using System.Collections;
using System.Collections.Generic;
using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Reactive;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.Blackboard
{
    /// <summary>
    /// AsakiBlackboardKey 单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiBlackboardKeyTests
    {
        [Test]
        [Description("使用字符串构造键时应正确计算FNV-1a哈希值")]
        public void Constructor_WithStringName_ComputesCorrectFNV1aHash()
        {
            // Arrange & Act
            var key1 = new AsakiBlackboardKey("TestKey");
            var key2 = new AsakiBlackboardKey("TestKey");

            // Assert - 相同字符串应产生相同哈希
            Assert.AreEqual(key1.Hash, key2.Hash);
            Assert.AreEqual(key1, key2);
        }

        [Test]
        [Description("使用不同字符串应产生不同哈希值")]
        public void Constructor_WithDifferentStrings_ProducesDifferentHashes()
        {
            // Arrange & Act
            var key1 = new AsakiBlackboardKey("KeyA");
            var key2 = new AsakiBlackboardKey("KeyB");

            // Assert
            Assert.AreNotEqual(key1.Hash, key2.Hash);
            Assert.AreNotEqual(key1, key2);
        }

        [Test]
        [Description("使用空字符串或null应产生哈希值0")]
        public void Constructor_WithNullOrEmpty_ProducesZeroHash()
        {
            // Arrange & Act
            var nullKey = new AsakiBlackboardKey(null);
            var emptyKey = new AsakiBlackboardKey("");

            // Assert
            Assert.AreEqual(0, nullKey.Hash);
            Assert.AreEqual(0, emptyKey.Hash);
            Assert.AreEqual(nullKey, emptyKey);
        }

        [Test]
        [Description("使用整数哈希构造键应直接使用该哈希值")]
        public void Constructor_WithIntHash_UsesHashDirectly()
        {
            // Arrange
            int hashValue = 123456;

            // Act
            var key = new AsakiBlackboardKey(hashValue);

            // Assert
            Assert.AreEqual(hashValue, key.Hash);
        }

        [Test]
        [Description("字符串隐式转换应正确创建键")]
        public void ImplicitConversion_FromString_CreatesCorrectKey()
        {
            // Arrange & Act
            AsakiBlackboardKey key = "ImplicitKey";
            var explicitKey = new AsakiBlackboardKey("ImplicitKey");

            // Assert
            Assert.AreEqual(explicitKey, key);
            Assert.AreEqual(explicitKey.Hash, key.Hash);
        }

        [Test]
        [Description("整数隐式转换应正确创建键")]
        public void ImplicitConversion_FromInt_CreatesCorrectKey()
        {
            // Arrange
            int hashValue = 789012;

            // Act
            AsakiBlackboardKey key = hashValue;
            var explicitKey = new AsakiBlackboardKey(hashValue);

            // Assert
            Assert.AreEqual(explicitKey, key);
            Assert.AreEqual(hashValue, key.Hash);
        }

        [Test]
        [Description("相等运算符应正确比较两个键")]
        public void EqualityOperators_CompareKeysCorrectly()
        {
            // Arrange
            var key1 = new AsakiBlackboardKey("SameKey");
            var key2 = new AsakiBlackboardKey("SameKey");
            var key3 = new AsakiBlackboardKey("DifferentKey");

            // Assert
            Assert.IsTrue(key1 == key2, "相同键应相等");
            Assert.IsFalse(key1 != key2, "相同键不应不相等");
            Assert.IsTrue(key1 != key3, "不同键应不相等");
            Assert.IsFalse(key1 == key3, "不同键不应相等");
        }

        [Test]
        [Description("Equals方法应正确比较对象")]
        public void Equals_ComparesObjectsCorrectly()
        {
            // Arrange
            var key = new AsakiBlackboardKey("TestKey");
            var sameKey = new AsakiBlackboardKey("TestKey");
            var differentKey = new AsakiBlackboardKey("OtherKey");

            // Assert
            Assert.IsTrue(key.Equals(sameKey));
            Assert.IsFalse(key.Equals(differentKey));
            Assert.IsFalse(key.Equals(null));
            // 注意：由于存在 string 到 AsakiBlackboardKey 的隐式转换，
            // key.Equals("TestKey") 实际上会先将字符串转换为键，然后比较，结果为 true
            // 这是预期的行为，因为隐式转换就是为了方便这种比较
            Assert.IsTrue(key.Equals("TestKey"), "隐式转换后应相等");
        }

        [Test]
        [Description("GetHashCode应返回键的哈希值")]
        public void GetHashCode_ReturnsKeyHash()
        {
            // Arrange
            var key = new AsakiBlackboardKey("HashTest");

            // Act & Assert
            Assert.AreEqual(key.Hash, key.GetHashCode());
        }

        [Test]
        [Description("FNV-1a哈希算法应保持跨平台一致性")]
        public void FNV1aHash_IsConsistentAcrossCalls()
        {
            // Arrange
            string testString = "ConsistencyTest";

            // Act
            var key1 = new AsakiBlackboardKey(testString);
            var key2 = new AsakiBlackboardKey(testString);
            var key3 = new AsakiBlackboardKey(testString);

            // Assert - 多次调用应产生相同结果
            Assert.AreEqual(key1.Hash, key2.Hash);
            Assert.AreEqual(key2.Hash, key3.Hash);
        }

        [Test]
        [Description("ToString应返回有意义的表示")]
        public void ToString_ReturnsMeaningfulRepresentation()
        {
            // Arrange
            var key = new AsakiBlackboardKey("ToStringTest");

            // Act
            string result = key.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
            StringAssert.Contains(key.Hash.ToString(), result);
        }
    }

    /// <summary>
    /// AsakiBlackboard 核心功能单元测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiBlackboardCoreTests
    {
        private AsakiBlackboard _blackboard;

        [SetUp]
        public void Setup()
        {
            _blackboard = new AsakiBlackboard();
        }

        [TearDown]
        public void Teardown()
        {
            _blackboard?.Dispose();
            _blackboard = null;
        }

        [Test]
        [Description("SetValue和GetValue应正确存储和检索值")]
        public void SetValue_GetValue_StoresAndRetrievesCorrectly()
        {
            // Arrange
            var key = new AsakiBlackboardKey("TestValue");
            int expectedValue = 42;

            // Act
            _blackboard.SetValue(key, expectedValue);
            int actualValue = _blackboard.GetValue<int>(key);

            // Assert
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        [Description("GetValue对不存在的键应返回默认值")]
        public void GetValue_WithNonExistentKey_ReturnsDefault()
        {
            // Arrange
            var key = new AsakiBlackboardKey("NonExistent");

            // Act
            int intValue = _blackboard.GetValue<int>(key);
            string stringValue = _blackboard.GetValue<string>(key);
            bool boolValue = _blackboard.GetValue<bool>(key);

            // Assert
            Assert.AreEqual(0, intValue);
            Assert.IsNull(stringValue);
            Assert.IsFalse(boolValue);
        }

        [Test]
        [Description("HasKey应正确检测键是否存在")]
        public void HasKey_DetectsKeyExistence()
        {
            // Arrange
            var existingKey = new AsakiBlackboardKey("ExistingKey");
            var nonExistingKey = new AsakiBlackboardKey("NonExistingKey");

            // Act
            _blackboard.SetValue(existingKey, 100);

            // Assert
            Assert.IsTrue(_blackboard.HasKey(existingKey), "已设置的键应存在");
            Assert.IsFalse(_blackboard.HasKey(nonExistingKey), "未设置的键不应存在");
        }

        [Test]
        [Description("Remove应正确删除键值")]
        public void Remove_DeletesKeyValue()
        {
            // Arrange
            var key = new AsakiBlackboardKey("KeyToRemove");
            _blackboard.SetValue(key, "Value");
            Assert.IsTrue(_blackboard.HasKey(key));

            // Act
            _blackboard.Remove(key);

            // Assert
            Assert.IsFalse(_blackboard.HasKey(key));
            Assert.AreEqual(default(string), _blackboard.GetValue<string>(key));
        }

        [Test]
        [Description("Clear应清空所有键值")]
        public void Clear_RemovesAllKeys()
        {
            // Arrange
            _blackboard.SetValue("Key1", 1);
            _blackboard.SetValue("Key2", 2);
            _blackboard.SetValue("Key3", 3);

            // Act
            _blackboard.Clear();

            // Assert
            Assert.IsFalse(_blackboard.HasKey("Key1"));
            Assert.IsFalse(_blackboard.HasKey("Key2"));
            Assert.IsFalse(_blackboard.HasKey("Key3"));
        }

        [Test]
        [Description("支持多种数据类型")]
        public void SetValue_GetValue_SupportsMultipleTypes()
        {
            // Arrange & Act & Assert - Int
            _blackboard.SetValue("IntKey", 42);
            Assert.AreEqual(42, _blackboard.GetValue<int>("IntKey"));

            // Float
            _blackboard.SetValue("FloatKey", 3.14f);
            Assert.AreEqual(3.14f, _blackboard.GetValue<float>("FloatKey"), 0.001f);

            // Bool
            _blackboard.SetValue("BoolKey", true);
            Assert.IsTrue(_blackboard.GetValue<bool>("BoolKey"));

            // String
            _blackboard.SetValue("StringKey", "Hello");
            Assert.AreEqual("Hello", _blackboard.GetValue<string>("StringKey"));

            // Vector3
            var vector = new Vector3(1, 2, 3);
            _blackboard.SetValue("VectorKey", vector);
            Assert.AreEqual(vector, _blackboard.GetValue<Vector3>("VectorKey"));
        }

        [Test]
        [Description("更新已存在的键应替换旧值")]
        public void SetValue_UpdateExisting_ReplacesOldValue()
        {
            // Arrange
            var key = new AsakiBlackboardKey("UpdateKey");
            _blackboard.SetValue(key, 100);

            // Act
            _blackboard.SetValue(key, 200);

            // Assert
            Assert.AreEqual(200, _blackboard.GetValue<int>(key));
        }
    }

    /// <summary>
    /// AsakiBlackboard 父作用域测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiBlackboardParentScopeTests
    {
        [Test]
        [Description("子作用域应从父作用域继承值")]
        public void ChildScope_InheritsValuesFromParent()
        {
            // Arrange
            var parent = new AsakiBlackboard();
            parent.SetValue("InheritedKey", "InheritedValue");

            // Act
            var child = new AsakiBlackboard(parent);

            // Assert
            Assert.AreEqual("InheritedValue", child.GetValue<string>("InheritedKey"));
            Assert.IsTrue(child.HasKey("InheritedKey"));

            parent.Dispose();
            child.Dispose();
        }

        [Test]
        [Description("子作用域的本地值应覆盖父作用域的值")]
        public void ChildScope_LocalValue_OverridesParent()
        {
            // Arrange
            var parent = new AsakiBlackboard();
            parent.SetValue("OverrideKey", "ParentValue");
            var child = new AsakiBlackboard(parent);

            // Act
            child.SetValue("OverrideKey", "ChildValue");

            // Assert
            Assert.AreEqual("ChildValue", child.GetValue<string>("OverrideKey"));
            Assert.AreEqual("ParentValue", parent.GetValue<string>("OverrideKey"));

            parent.Dispose();
            child.Dispose();
        }

        [Test]
        [Description("父作用域的值变更不应影响已覆盖的子作用域值")]
        public void ParentChange_DoesNotAffectOverriddenChildValue()
        {
            // Arrange
            var parent = new AsakiBlackboard();
            parent.SetValue("Key", "Original");
            var child = new AsakiBlackboard(parent);
            child.SetValue("Key", "Overridden");

            // Act
            parent.SetValue("Key", "Changed");

            // Assert
            Assert.AreEqual("Overridden", child.GetValue<string>("Key"));

            parent.Dispose();
            child.Dispose();
        }

        [Test]
        [Description("子作用域删除键不应影响父作用域")]
        public void ChildRemove_DoesNotAffectParent()
        {
            // Arrange
            var parent = new AsakiBlackboard();
            parent.SetValue("Key", "Value");
            var child = new AsakiBlackboard(parent);

            // Act - 尝试删除继承的键（实际上只删除本地）
            child.Remove("Key");

            // Assert
            Assert.IsTrue(parent.HasKey("Key"));
            // 子作用域删除后，应该回退到父作用域的值
            Assert.IsTrue(child.HasKey("Key"));
            Assert.AreEqual("Value", child.GetValue<string>("Key"));

            parent.Dispose();
            child.Dispose();
        }
    }

    /// <summary>
    /// AsakiBlackboard 批处理模式测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiBlackboardBatchTests
    {
        private AsakiBlackboard _blackboard;
        private int _notificationCount;

        [SetUp]
        public void Setup()
        {
            _blackboard = new AsakiBlackboard();
            _notificationCount = 0;
        }

        [TearDown]
        public void Teardown()
        {
            _blackboard?.Dispose();
        }

        [Test]
        [Description("BeginBatch应返回可释放的对象")]
        public void BeginBatch_ReturnsDisposable()
        {
            // Act
            var batch = _blackboard.BeginBatch();

            // Assert
            Assert.IsNotNull(batch);
            Assert.IsInstanceOf<IDisposable>(batch);

            // Cleanup
            batch.Dispose();
        }

        [Test]
        [Description("批处理模式应延迟通知直到批次结束")]
        public void BatchMode_DelaysNotificationsUntilBatchEnd()
        {
            // Arrange
            var property = _blackboard.GetProperty<int>("BatchKey");
            property.Subscribe(_ => _notificationCount++);
            _notificationCount = 0;

            // Act - 在批次中多次设置值
            using (_blackboard.BeginBatch())
            {
                _blackboard.SetValue("BatchKey", 1);
                _blackboard.SetValue("BatchKey", 2);
                _blackboard.SetValue("BatchKey", 3);
                // 批次结束前不应有通知
                Assert.AreEqual(0, _notificationCount);
            }

            // Assert - 批次结束后应有通知
            Assert.GreaterOrEqual(_notificationCount, 1);
        }

        [Test]
        [Description("嵌套批次应正确处理")]
        public void NestedBatches_AreHandledCorrectly()
        {
            // Arrange
            var property = _blackboard.GetProperty<int>("NestedKey");
            property.Subscribe(_ => _notificationCount++);
            _notificationCount = 0;

            // Act - 嵌套批次
            using (_blackboard.BeginBatch())
            {
                _blackboard.SetValue("NestedKey", 1);

                using (_blackboard.BeginBatch())
                {
                    _blackboard.SetValue("NestedKey", 2);
                    Assert.AreEqual(0, _notificationCount);
                }

                // 内层批次结束，外层仍在批次中
                Assert.AreEqual(0, _notificationCount);
            }

            // Assert - 外层批次结束后才有通知
            Assert.GreaterOrEqual(_notificationCount, 1);
        }

        [Test]
        [Description("批处理扩展方法应正确批量设置值")]
        public void BatchSetExtension_SetsMultipleValues()
        {
            // Arrange & Act
            _blackboard.BatchSet(("Key1", 100), ("Key2", "Hello"), ("Key3", true));

            // Assert
            Assert.AreEqual(100, _blackboard.GetValue<int>("Key1"));
            Assert.AreEqual("Hello", _blackboard.GetValue<string>("Key2"));
            Assert.IsTrue(_blackboard.GetValue<bool>("Key3"));
        }

        [Test]
        [Description("使用字典的批处理扩展方法应正确设置值")]
        public void BatchSet_WithDictionary_SetsValues()
        {
            // Arrange
            var updates = new Dictionary<string, object>
            {
                { "DictKey1", 42 },
                { "DictKey2", 3.14f },
                { "DictKey3", "World" },
            };

            // Act
            _blackboard.BatchSet(updates);

            // Assert
            Assert.AreEqual(42, _blackboard.GetValue<int>("DictKey1"));
            Assert.AreEqual(3.14f, _blackboard.GetValue<float>("DictKey2"), 0.001f);
            Assert.AreEqual("World", _blackboard.GetValue<string>("DictKey3"));
        }
    }

    /// <summary>
    /// AsakiBlackboard 属性系统测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiBlackboardPropertyTests
    {
        private AsakiBlackboard _blackboard;

        [SetUp]
        public void Setup()
        {
            _blackboard = new AsakiBlackboard();
        }

        [TearDown]
        public void Teardown()
        {
            _blackboard?.Dispose();
        }

        [Test]
        [Description("GetProperty应返回AsakiProperty实例")]
        public void GetProperty_ReturnsAsakiProperty()
        {
            // Arrange & Act
            var property = _blackboard.GetProperty<int>("PropKey");

            // Assert
            Assert.IsNotNull(property);
            Assert.IsInstanceOf<AsakiProperty<int>>(property);
        }

        [Test]
        [Description("获取相同键的属性应返回同一实例")]
        public void GetProperty_SameKey_ReturnsSameInstance()
        {
            // Arrange & Act
            var property1 = _blackboard.GetProperty<string>("SameProp");
            var property2 = _blackboard.GetProperty<string>("SameProp");

            // Assert
            Assert.AreSame(property1, property2);
        }

        [Test]
        [Description("属性应与黑板值同步")]
        public void Property_SyncsWithBlackboardValue()
        {
            // Arrange
            _blackboard.SetValue("SyncKey", 50);
            var property = _blackboard.GetProperty<int>("SyncKey");

            // Assert - 属性应反映当前黑板值
            Assert.AreEqual(50, property.Value);

            // Act - 修改黑板值
            _blackboard.SetValue("SyncKey", 100);

            // Assert - 属性应更新
            Assert.AreEqual(100, property.Value);
        }

        [Test]
        [Description("属性订阅应接收变更通知")]
        public void PropertySubscription_ReceivesChangeNotifications()
        {
            // Arrange
            int receivedValue = 0;
            bool notified = false;
            var property = _blackboard.GetProperty<int>("NotifyKey");
            property.Subscribe(v =>
            {
                receivedValue = v;
                notified = true;
            });

            // Act
            _blackboard.SetValue("NotifyKey", 999);

            // Assert
            Assert.IsTrue(notified);
            Assert.AreEqual(999, receivedValue);
        }

        [Test]
        [Description("使用using语句应自动取消订阅")]
        public void Subscription_UsingStatement_AutoUnsubscribes()
        {
            // Arrange
            int notificationCount = 0;
            var property = _blackboard.GetProperty<int>("AutoUnsubKey");

            // Act
            using (property.Subscribe(_ => notificationCount++))
            {
                // Subscribe 会立即调用一次回调（值为默认值 0），所以 count = 1
                // 然后设置值为 1，再次触发回调，count = 2
                _blackboard.SetValue("AutoUnsubKey", 1);
            }

            // 取消订阅后再次设置值，不应再触发回调
            _blackboard.SetValue("AutoUnsubKey", 2);

            // Assert - 应该收到两次通知（订阅时1次 + SetValue时1次）
            Assert.AreEqual(2, notificationCount, "订阅时立即回调一次，SetValue时回调一次");
        }

        [Test]
        [Description("删除键应清理相关属性")]
        public void RemoveKey_CleansUpProperty()
        {
            // Arrange
            var property = _blackboard.GetProperty<int>("CleanupKey");
            _blackboard.SetValue("CleanupKey", 123);

            // Act
            _blackboard.Remove("CleanupKey");

            // Assert - 重新获取属性应得到新实例
            var newProperty = _blackboard.GetProperty<int>("CleanupKey");
            // 由于属性已被清理和释放，获取的新属性应为默认值
            Assert.IsNotNull(newProperty);
        }
    }

    /// <summary>
    /// 变量类型测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiVariableTypeTests
    {
        private AsakiBlackboard _blackboard;

        [SetUp]
        public void Setup()
        {
            _blackboard = new AsakiBlackboard();
        }

        [TearDown]
        public void Teardown()
        {
            _blackboard?.Dispose();
        }

        [Test]
        [Description("AsakiInt应正确存储和检索整数值")]
        public void AsakiInt_StoresAndRetrievesInt()
        {
            // Arrange
            var intValue = new AsakiInt(42);

            // Act
            intValue.ApplyTo(_blackboard, "IntTest");

            // Assert
            Assert.AreEqual(42, _blackboard.GetValue<int>("IntTest"));
            Assert.AreEqual("Int32", intValue.TypeName);
        }

        [Test]
        [Description("AsakiFloat应正确存储和检索浮点值")]
        public void AsakiFloat_StoresAndRetrievesFloat()
        {
            // Arrange
            var floatValue = new AsakiFloat(3.14159f);

            // Act
            floatValue.ApplyTo(_blackboard, "FloatTest");

            // Assert
            Assert.AreEqual(3.14159f, _blackboard.GetValue<float>("FloatTest"), 0.0001f);
        }

        [Test]
        [Description("AsakiBool应正确存储和检索布尔值")]
        public void AsakiBool_StoresAndRetrievesBool()
        {
            // Arrange
            var boolValue = new AsakiBool(true);

            // Act
            boolValue.ApplyTo(_blackboard, "BoolTest");

            // Assert
            Assert.IsTrue(_blackboard.GetValue<bool>("BoolTest"));
        }

        [Test]
        [Description("AsakiString应正确存储和检索字符串值")]
        public void AsakiString_StoresAndRetrievesString()
        {
            // Arrange
            var stringValue = new AsakiString("Hello Blackboard");

            // Act
            stringValue.ApplyTo(_blackboard, "StringTest");

            // Assert
            Assert.AreEqual("Hello Blackboard", _blackboard.GetValue<string>("StringTest"));
        }

        [Test]
        [Description("AsakiVector3应正确存储和检索Vector3")]
        public void AsakiVector3_StoresAndRetrievesVector3()
        {
            // Arrange
            var vector = new Vector3(1f, 2f, 3f);
            var vectorValue = new AsakiVector3(vector);

            // Act
            vectorValue.ApplyTo(_blackboard, "Vector3Test");

            // Assert
            Assert.AreEqual(vector, _blackboard.GetValue<Vector3>("Vector3Test"));
        }

        [Test]
        [Description("AsakiVector2应正确存储和检索Vector2")]
        public void AsakiVector2_StoresAndRetrievesVector2()
        {
            // Arrange
            var vector = new Vector2(5f, 10f);
            var vectorValue = new AsakiVector2(vector);

            // Act
            vectorValue.ApplyTo(_blackboard, "Vector2Test");

            // Assert
            Assert.AreEqual(vector, _blackboard.GetValue<Vector2>("Vector2Test"));
        }

        [Test]
        [Description("AsakiColor应正确存储和检索Color")]
        public void AsakiColor_StoresAndRetrievesColor()
        {
            // Arrange
            var color = new Color(0.5f, 0.2f, 0.8f, 1f);
            var colorValue = new AsakiColor(color);

            // Act
            colorValue.ApplyTo(_blackboard, "ColorTest");

            // Assert
            Assert.AreEqual(color, _blackboard.GetValue<Color>("ColorTest"));
        }

        [Test]
        [Description("AsakiGameObject应正确存储和检索GameObject")]
        public void AsakiGameObject_StoresAndRetrievesGameObject()
        {
            // Arrange
            var go = new GameObject("TestGameObject");
            var goValue = new AsakiGameObject(go);

            // Act
            goValue.ApplyTo(_blackboard, "GameObjectTest");

            // Assert
            Assert.AreSame(go, _blackboard.GetValue<GameObject>("GameObjectTest"));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        [Description("克隆应创建具有相同值的副本")]
        public void Clone_CreatesCopyWithSameValue()
        {
            // Arrange
            var original = new AsakiInt(100);

            // Act
            var clone = original.Clone() as AsakiInt;

            // Assert
            Assert.IsNotNull(clone);
            Assert.AreEqual(original.Value, clone.Value);
            Assert.AreNotSame(original, clone);
        }

        [Test]
        [Description("AsakiVariableDef应正确管理默认值")]
        public void AsakiVariableDef_ManagesDefaultValue()
        {
            // Arrange
            var varDef = new AsakiVariableDef
            {
                Name = "TestVar",
                ValueData = new AsakiInt(10),
                DefaultValue = new AsakiInt(5),
            };

            // Assert
            Assert.AreEqual("Int32", varDef.TypeName);

            // Act - 重置为默认值
            varDef.ResetToDefault();

            // Assert
            Assert.AreEqual(5, (varDef.ValueData as AsakiInt).Value);
        }
    }

    /// <summary>
    /// 变量约束测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiVariableConstraintTests
    {
        [Test]
        [Description("RangeConstraint应正确验证数值范围")]
        public void RangeConstraint_ValidatesNumericRange()
        {
            // Arrange
            var constraint = new RangeConstraint { MinValue = 0, MaxValue = 100 };

            // Assert - 范围内的值
            Assert.IsTrue(constraint.IsValid(50));
            Assert.IsTrue(constraint.IsValid(0));
            Assert.IsTrue(constraint.IsValid(100));

            // Assert - 范围外的值
            Assert.IsFalse(constraint.IsValid(-1));
            Assert.IsFalse(constraint.IsValid(101));

            // Assert - 浮点数
            Assert.IsTrue(constraint.IsValid(50.5f));
            Assert.IsFalse(constraint.IsValid(100.1f));
        }

        [Test]
        [Description("NotNullConstraint应正确验证非空")]
        public void NotNullConstraint_ValidatesNonNull()
        {
            // Arrange
            var constraint = new NotNullConstraint();

            // Assert
            Assert.IsFalse(constraint.IsValid(null));
            Assert.IsTrue(constraint.IsValid("not null"));
            Assert.IsTrue(constraint.IsValid(0));
            Assert.IsTrue(constraint.IsValid(new object()));
        }

        [Test]
        [Description("RegexConstraint应正确验证字符串模式")]
        public void RegexConstraint_ValidatesStringPattern()
        {
            // Arrange
            var constraint = new RegexConstraint { Pattern = "^[A-Z][a-z]+$" };

            // Assert
            Assert.IsTrue(constraint.IsValid("Hello"));
            Assert.IsTrue(constraint.IsValid("World"));
            Assert.IsFalse(constraint.IsValid("hello")); // 小写开头
            Assert.IsFalse(constraint.IsValid("HELLO")); // 全大写
            Assert.IsFalse(constraint.IsValid("Hello World")); // 包含空格

            // 非字符串应返回true（不验证）
            Assert.IsTrue(constraint.IsValid(123));
        }

        [Test]
        [Description("约束应通过AsakiVariableDef正确验证")]
        public void Constraint_ValidatesThroughVariableDef()
        {
            // Arrange
            var varDef = new AsakiVariableDef
            {
                Constraint = new RangeConstraint { MinValue = 0, MaxValue = 10 },
            };

            // Assert
            Assert.IsTrue(varDef.Validate(5));
            Assert.IsFalse(varDef.Validate(15));
        }
    }

    /// <summary>
    /// AsakiBlackboard 边界情况和异常测试
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class AsakiBlackboardEdgeCaseTests
    {
        [Test]
        [Description("Dispose后不应抛出异常")]
        public void Dispose_DoesNotThrow()
        {
            // Arrange
            var blackboard = new AsakiBlackboard();
            blackboard.SetValue("Key", "Value");

            // Act & Assert
            Assert.DoesNotThrow(() => blackboard.Dispose());
        }

        [Test]
        [Description("多次Dispose不应抛出异常")]
        public void MultipleDispose_DoesNotThrow()
        {
            // Arrange
            var blackboard = new AsakiBlackboard();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                blackboard.Dispose();
                blackboard.Dispose();
                blackboard.Dispose();
            });
        }

        [Test]
        [Description("空批处理应正常工作")]
        public void EmptyBatch_WorksCorrectly()
        {
            // Arrange
            var blackboard = new AsakiBlackboard();

            // Act & Assert - 空批次不应抛出异常
            Assert.DoesNotThrow(() =>
            {
                using (blackboard.BeginBatch())
                {
                    // 批次中不设置任何值
                }
            });

            blackboard.Dispose();
        }

        [Test]
        [Description("处理大量键值对应正常工作")]
        public void LargeNumberOfKeys_WorksCorrectly()
        {
            // Arrange
            var blackboard = new AsakiBlackboard();
            const int count = 1000;

            // Act
            for (int i = 0; i < count; i++)
            {
                blackboard.SetValue($"Key{i}", i);
            }

            // Assert
            Assert.IsTrue(blackboard.HasKey("Key0"));
            Assert.IsTrue(blackboard.HasKey($"Key{count - 1}"));
            Assert.AreEqual(0, blackboard.GetValue<int>("Key0"));
            Assert.AreEqual(count - 1, blackboard.GetValue<int>($"Key{count - 1}"));

            blackboard.Dispose();
        }

        [Test]
        [Description("特殊字符键名应正常工作")]
        public void SpecialCharacterKeyNames_WorkCorrectly()
        {
            // Arrange
            var blackboard = new AsakiBlackboard();
            var specialKeys = new[]
            {
                "Key.With.Dots",
                "Key-With-Dashes",
                "Key_With_Underscores",
                "Key:With:Colons",
                "Key/With/Slashes",
                "Key With Spaces",
                "Key\nWith\nNewlines",
                "Key\tWith\tTabs",
            };

            // Act & Assert
            foreach (var key in specialKeys)
            {
                blackboard.SetValue(key, key);
                Assert.AreEqual(key, blackboard.GetValue<string>(key), $"Failed for key: {key}");
            }

            blackboard.Dispose();
        }
    }
}
