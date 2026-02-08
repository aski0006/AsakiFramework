using System;
using UnityEngine;

namespace Asaki.Core.Resources
{
    /// <summary>
    /// 可序列化的资源类型基类。
    /// 用于在 Inspector 中配置资源加载类型。
    /// </summary>
    [Serializable]
    public abstract class SerializableResourceType
    {
        /// <summary>
        /// 获取类型的显示名称
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// 获取实际的 System.Type
        /// </summary>
        public abstract Type GetResourceType();
    }

    /// <summary>
    /// GameObject 资源类型
    /// </summary>
    [Serializable]
    public class GameObjectResourceType : SerializableResourceType
    {
        public override string TypeName => "GameObject";
        public override Type GetResourceType() => typeof(GameObject);
    }

    /// <summary>
    /// Texture2D 资源类型
    /// </summary>
    [Serializable]
    public class Texture2DResourceType : SerializableResourceType
    {
        public override string TypeName => "Texture2D";
        public override Type GetResourceType() => typeof(Texture2D);
    }

    /// <summary>
    /// Sprite 资源类型
    /// </summary>
    [Serializable]
    public class SpriteResourceType : SerializableResourceType
    {
        public override string TypeName => "Sprite";
        public override Type GetResourceType() => typeof(Sprite);
    }

    /// <summary>
    /// Material 资源类型
    /// </summary>
    [Serializable]
    public class MaterialResourceType : SerializableResourceType
    {
        public override string TypeName => "Material";
        public override Type GetResourceType() => typeof(Material);
    }

    /// <summary>
    /// AudioClip 资源类型
    /// </summary>
    [Serializable]
    public class AudioClipResourceType : SerializableResourceType
    {
        public override string TypeName => "AudioClip";
        public override Type GetResourceType() => typeof(AudioClip);
    }

    /// <summary>
    /// TextAsset 资源类型
    /// </summary>
    [Serializable]
    public class TextAssetResourceType : SerializableResourceType
    {
        public override string TypeName => "TextAsset";
        public override Type GetResourceType() => typeof(TextAsset);
    }

    /// <summary>
    /// AnimationClip 资源类型
    /// </summary>
    [Serializable]
    public class AnimationClipResourceType : SerializableResourceType
    {
        public override string TypeName => "AnimationClip";
        public override Type GetResourceType() => typeof(AnimationClip);
    }

    /// <summary>
    /// ScriptableObject 资源类型
    /// </summary>
    [Serializable]
    public class ScriptableObjectResourceType : SerializableResourceType
    {
        public override string TypeName => "ScriptableObject";
        public override Type GetResourceType() => typeof(ScriptableObject);
    }

    /// <summary>
    /// Shader 资源类型
    /// </summary>
    [Serializable]
    public class ShaderResourceType : SerializableResourceType
    {
        public override string TypeName => "Shader";
        public override Type GetResourceType() => typeof(Shader);
    }

    /// <summary>
    /// Mesh 资源类型
    /// </summary>
    [Serializable]
    public class MeshResourceType : SerializableResourceType
    {
        public override string TypeName => "Mesh";
        public override Type GetResourceType() => typeof(Mesh);
    }

    /// <summary>
    /// 自定义资源类型
    /// </summary>
    [Serializable]
    public class CustomResourceType : SerializableResourceType
    {
        [Tooltip("类型全名，格式: 命名空间.类型名, 程序集名\n例如: UnityEngine.Video.VideoClip, UnityEngine.VideoModule")]
        public string TypeFullName = "UnityEngine.Object, UnityEngine.CoreModule";

        public override string TypeName => $"Custom ({TypeFullName})";

        public override Type GetResourceType()
        {
            if (string.IsNullOrEmpty(TypeFullName))
                return typeof(UnityEngine.Object);

            var type = Type.GetType(TypeFullName);
            return type ?? typeof(UnityEngine.Object);
        }
    }
}
