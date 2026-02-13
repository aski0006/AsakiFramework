namespace Asaki.CodeGen.Tests
{
    public static class TestSources
    {
        // --------------------------------------------------------------------
        // 1. 模拟框架环境 (Framework Environment)
        // --------------------------------------------------------------------
        public const string CoreFramework = @"
using System;
using System.Collections.Generic;

// --- A. 模拟 UnityEngine ---
namespace UnityEngine
{
    public class MonoBehaviour {}
    public struct Vector3 { public float x, y, z; }
    public struct Vector2 { public float x, y; }
    public struct Quaternion { public float x, y, z, w; }
    
    // 模拟 RuntimeInitializeOnLoadMethod，这对 TypeRegistry 至关重要
    public enum RuntimeInitializeLoadType { BeforeSceneLoad }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute 
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) {}
    }
}

// --- B. Asaki.Core (定义接口与核心契约) ---
namespace Asaki.Core
{
    // [TypeRegistry] Schema 标记属性 (根据你最新的更改，位于 Asaki.Core)
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AsakiBlackboardValueSchemaAttribute : Attribute {}
}

namespace Asaki.Core.Broker
{
    // ... (原有的 Broker 接口保持不变)
    public interface IAsakiEvent {}
    public interface IAsakiHandler<T> where T : struct, IAsakiEvent { void OnEvent(T e); }
    public static class AsakiBroker 
    {
        public static void Subscribe<T>(IAsakiHandler<T> handler) where T : struct, IAsakiEvent {}
        public static void Unsubscribe<T>(IAsakiHandler<T> handler) where T : struct, IAsakiEvent {}
    }
    public struct AsakiProperty<T> { public T Value; }
    
    // ... (原有的 Save 接口保持不变)
    public interface IAsakiWriter { void WriteInt(string k, int v); /*...*/ }
    public interface IAsakiReader { int ReadVersion(); /*...*/ }
    public interface IAsakiSavable { void Serialize(IAsakiWriter w); void Deserialize(IAsakiReader r); }
    public class AsakiSaveAttribute : Attribute { public AsakiSaveAttribute(int v=1){} }
    public class AsakiSaveMemberAttribute : Attribute { public int Order {get;set;} }
    public class AsakiBindAttribute : Attribute { public AsakiBindAttribute(string p=null){} }
}

// --- C. Asaki.Core.Blackboard (为 TypeRegistry 新增的环境) ---
namespace Asaki.Core.Blackboard
{
    // 黑板接口
    public interface IAsakiBlackboard
    {
        // 生成的代码会调用 bb.SetValue(key, v)
        void SetValue<T>(string key, T value);
    }
}

namespace Asaki.Core.Blackboard.Variables
{
    // 类型桥梁 (Generator 会注入 SetValueDispatch)
    public static class AsakiTypeBridge
    {
        public static Action<IAsakiBlackboard, string, object> SetValueDispatch;
    }

    // 值基类 (模拟用户自定义类型的父类)
    public abstract class AsakiValueBase {}

    public class AsakiValue<T> : AsakiValueBase
    {
        public T Value;
    }
}
";

        // --------------------------------------------------------------------
        // 2. 模拟用户代码 (User Game Code)
        // --------------------------------------------------------------------
        public const string UserGameCode = @"
using Asaki.Core; // 引用 Attribute
using Asaki.Core.Broker;
using Asaki.Core.Blackboard.Variables; // 引用 AsakiValue<T>
using UnityEngine;
using System;

namespace MyGame.Modules
{
    // ==================================================
    // Case 1: 事件系统
    // ==================================================
    public struct PlayerLoginEvent : IAsakiEvent { public int UserId; }
    public class PlayerManager : MonoBehaviour, IAsakiHandler<PlayerLoginEvent>
    {
        public void OnEvent(PlayerLoginEvent e) {}
    }

    // ==================================================
    // Case 2: 序列化系统
    // ==================================================
    [AsakiSave(version: 1)]
    public partial class PlayerData
    {
        [AsakiSaveMember(Order = 1)] public int Level;
        [AsakiSaveMember(Order = 2)] public Vector3 Position;
    }

    // ==================================================
    // Case 3: UI 数据绑定
    // ==================================================
    [AsakiBind(""HUD/PlayerInfo"")]
    public partial class PlayerHUD : MonoBehaviour
    {
        public AsakiProperty<int> HP;
    }

    // ==================================================
    // Case 4: 黑板类型注册 (TypeRegistry Generator)
    // ==================================================
    
    // [Case 4-A] 定义数据结构
namespace Game.Examples
{
    [Serializable]
    [AsakiBlackboardValueSchema] // <--- 生成器应该捕获这个标记
    public struct ProductData
    {
        public int Id;
        public int Quality;
    }
    
    // 这个类用于编辑器，生成器其实不关心它，但为了编译通过我们需要保留
    [Serializable]
    public class AsakiProduct : AsakiValue<ProductData> { }
}

// === 其他干扰项 (用于测试生成器是否只会处理标记了 Schema 的类型) ===
namespace Game.Ignored
{
    // 这个结构体没有标记 Schema，应该被忽略
    public struct UnregisteredData 
    { 
        public int IgnoreMe; 
    }
}
";
    }
}