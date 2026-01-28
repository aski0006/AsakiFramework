using System.Collections.Generic;

namespace Asaki.Editor.Utilities.Tools.AssetsExplore
{
    /// <summary>
    /// 分类管理器 - 扩展性强的插件式架构
    /// </summary>
    public class CategoryManager
    {
        private static readonly Dictionary<string, AssetCategory> _extensionMap = new Dictionary<
            string,
            AssetCategory
        >
        {
            // 纹理
            { ".png", AssetCategory.Textures },
            { ".jpg", AssetCategory.Textures },
            { ".jpeg", AssetCategory.Textures },
            { ".tga", AssetCategory.Textures },
            { ".tif", AssetCategory.Textures },
            { ".bmp", AssetCategory.Textures },
            { ".psd", AssetCategory.Textures },
            { ".exr", AssetCategory.Textures },
            { ".hdr", AssetCategory.Textures },
            // 材质
            { ".mat", AssetCategory.Materials },
            // 模型
            { ".fbx", AssetCategory.Models },
            { ".obj", AssetCategory.Models },
            { ".blend", AssetCategory.Models },
            { ".max", AssetCategory.Models },
            { ".ma", AssetCategory.Models },
            { ".mb", AssetCategory.Models },
            // 预制件
            { ".prefab", AssetCategory.Prefabs },
            // 场景
            { ".unity", AssetCategory.Scenes },
            // 脚本
            { ".cs", AssetCategory.Scripts },
            { ".shader", AssetCategory.Shaders },
            { ".hlsl", AssetCategory.Shaders },
            { ".cginc", AssetCategory.Shaders },
            // 音频
            { ".mp3", AssetCategory.Audio },
            { ".wav", AssetCategory.Audio },
            { ".ogg", AssetCategory.Audio },
            { ".aif", AssetCategory.Audio },
            { ".aiff", AssetCategory.Audio },
            // 动画
            { ".anim", AssetCategory.Animations },
            { ".controller", AssetCategory.Animations },
            // 字体
            { ".ttf", AssetCategory.Fonts },
            { ".otf", AssetCategory.Fonts },
            // 视频
            { ".mp4", AssetCategory.Videos },
            { ".mov", AssetCategory.Videos },
            { ".avi", AssetCategory.Videos },
            { ".webm", AssetCategory.Videos },
            // 文档
            { ".txt", AssetCategory.Documents },
            { ".json", AssetCategory.Documents },
            { ".xml", AssetCategory.Documents },
            { ".md", AssetCategory.Documents },
            { ".pdf", AssetCategory.Documents },
        };

        public static AssetCategory GetCategory(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return AssetCategory.Others;

            extension = extension.ToLowerInvariant();
            return _extensionMap.TryGetValue(extension, out AssetCategory category)
                ? category
                : AssetCategory.Others;
        }

        // 支持动态添加分类规则
        public static void RegisterExtension(string extension, AssetCategory category)
        {
            _extensionMap[extension.ToLowerInvariant()] = category;
        }
    }
}
