using System;
using UnityEngine;

namespace Asaki.Core.Resources
{
    /// <summary>
    /// 可序列化的资源类型基类
    /// <para>用于在Inspector中配置资源加载类型，支持多态序列化。</para>
    /// <para>配合[SerializeReference]属性使用，实现类型选择器。</para>
    /// </summary>
    /// <remarks>
    /// <para>内置类型：</para>
    /// <list type="bullet">
    /// <item><description>GameObjectResourceType - GameObject预制体</description></item>
    /// <item><description>Texture2DResourceType - 2D纹理</description></item>
    /// <item><description>SpriteResourceType - 精灵图片</description></item>
    /// <item><description>MaterialResourceType - 材质</description></item>
    /// <item><description>AudioClipResourceType - 音频片段</description></item>
    /// <item><description>TextAssetResourceType - 文本资源</description></item>
    /// <item><description>AnimationClipResourceType - 动画片段</description></item>
    /// <item><description>ScriptableObjectResourceType - ScriptableObject</description></item>
    /// <item><description>ShaderResourceType - 着色器</description></item>
    /// <item><description>MeshResourceType - 网格</description></item>
    /// <item><description>CustomResourceType - 自定义类型</description></item>
    /// </list>
    /// </remarks>
    [Serializable]
    public abstract class SerializableResourceType
    {
        /// <summary>
        /// 获取类型的显示名称
        /// <para>用于Inspector中显示类型名称。</para>
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// 获取实际的System.Type
        /// <para>用于资源加载时指定正确的类型。</para>
        /// </summary>
        public abstract Type GetResourceType();
    }

    /// <summary>
    /// GameObject资源类型
    /// </summary>
    [Serializable]
    public class GameObjectResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "GameObject";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(GameObject);
    }

    /// <summary>
    /// Texture2D资源类型
    /// </summary>
    [Serializable]
    public class Texture2DResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "Texture2D";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(Texture2D);
    }

    /// <summary>
    /// Sprite资源类型
    /// </summary>
    [Serializable]
    public class SpriteResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "Sprite";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(Sprite);
    }

    /// <summary>
    /// Material资源类型
    /// </summary>
    [Serializable]
    public class MaterialResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "Material";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(Material);
    }

    /// <summary>
    /// AudioClip资源类型
    /// </summary>
    [Serializable]
    public class AudioClipResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "AudioClip";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(AudioClip);
    }

    /// <summary>
    /// TextAsset资源类型
    /// </summary>
    [Serializable]
    public class TextAssetResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "TextAsset";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(TextAsset);
    }

    /// <summary>
    /// AnimationClip资源类型
    /// </summary>
    [Serializable]
    public class AnimationClipResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "AnimationClip";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(AnimationClip);
    }

    /// <summary>
    /// ScriptableObject资源类型
    /// </summary>
    [Serializable]
    public class ScriptableObjectResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "ScriptableObject";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(ScriptableObject);
    }

    /// <summary>
    /// Shader资源类型
    /// </summary>
    [Serializable]
    public class ShaderResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "Shader";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(Shader);
    }

    /// <summary>
    /// Mesh资源类型
    /// </summary>
    [Serializable]
    public class MeshResourceType : SerializableResourceType
    {
        /// <inheritdoc/>
        public override string TypeName => "Mesh";

        /// <inheritdoc/>
        public override Type GetResourceType() => typeof(Mesh);
    }

    /// <summary>
    /// 自定义资源类型
    /// <para>支持通过类型全名指定任意Unity资源类型。</para>
    /// </summary>
    /// <remarks>
    /// <para>类型全名格式：命名空间.类型名, 程序集名</para>
    /// <para>示例：UnityEngine.Video.VideoClip, UnityEngine.VideoModule</para>
    /// </remarks>
    [Serializable]
    public class CustomResourceType : SerializableResourceType
    {
        [Tooltip(
            "类型全名，格式: 命名空间.类型名, 程序集名\n"
                + "例如: UnityEngine.Video.VideoClip, UnityEngine.VideoModule"
        )]
        public string TypeFullName = "UnityEngine.Object, UnityEngine.CoreModule";

        /// <inheritdoc/>
        public override string TypeName => $"Custom ({TypeFullName})";

        /// <inheritdoc/>
        public override Type GetResourceType()
        {
            if (string.IsNullOrEmpty(TypeFullName))
                return typeof(UnityEngine.Object);

            var type = Type.GetType(TypeFullName);
            return type ?? typeof(UnityEngine.Object);
        }
    }
}
