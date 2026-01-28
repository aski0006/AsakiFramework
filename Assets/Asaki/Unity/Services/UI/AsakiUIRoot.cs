using System.Collections.Generic;
using Asaki.Core.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Asaki.Unity.Services.UI
{
    public class AsakiUIRoot : MonoBehaviour
    {
        // 存储层级节点
        private readonly Dictionary<AsakiUILayer, Transform> _layers =
            new Dictionary<AsakiUILayer, Transform>();

        // [新增] 存储各层的 Raycaster，用于物理屏蔽优化
        private readonly Dictionary<AsakiUILayer, GraphicRaycaster> _layerRaycasters =
            new Dictionary<AsakiUILayer, GraphicRaycaster>();

        private Vector2 _referenceResolution;
        private float _matchWidthOrHeight;

        /// <summary>
        /// 初始化 UI 根节点结构
        /// </summary>
        public void Initialize(Vector2 referenceResolution, float matchWidthOrHeight)
        {
            _referenceResolution = referenceResolution;
            _matchWidthOrHeight = matchWidthOrHeight;

            // 1. 确保 EventSystem (兼容新旧输入系统)
            EnsureEventSystem();

            // 2. 清理残留子节点 (防止热重载或重复初始化导致的重叠)
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _layers.Clear();
            _layerRaycasters.Clear();

            // 3. [核心优化] 创建分离的 Canvas 层级
            // Order 间隔 1000，确保层级绝对分离且中间有足够的空间插入动态层(如果有)
            CreateLayerNode("0_Scene", AsakiUILayer.Scene, 0); // 0-999
            CreateLayerNode("1_Normal", AsakiUILayer.Normal, 1000); // 1000-1999
            CreateLayerNode("2_Popup", AsakiUILayer.Popup, 2000); // 2000-2999
            CreateLayerNode("3_System", AsakiUILayer.System, 3000); // 3000+

            DontDestroyOnLoad(gameObject);
        }

        private void CreateLayerNode(string goName, AsakiUILayer layer, int sortOrder)
        {
            GameObject go = new GameObject(goName);
            go.transform.SetParent(transform, false);

            // [A] 独立的 Canvas
            // 优势：层级间互不干扰，Popup 层的 Rebuild 不会波及 Normal 层
            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.overrideSorting = true; // 开启独立排序
            c.sortingOrder = sortOrder;

            // [B] 独立的 Scaler
            // 必须每个 Canvas 都挂 Scaler，否则无法正确适配屏幕
            CanvasScaler s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = _referenceResolution;
            s.matchWidthOrHeight = _matchWidthOrHeight;

            // [C] 独立的 Raycaster
            GraphicRaycaster r = go.AddComponent<GraphicRaycaster>();
            _layerRaycasters[layer] = r;

            // [D] 统一的 CanvasGroup
            // 用于实现层级整体淡入淡出 (例如：打开 System 菜单时，Normal 层整体变暗)
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            // 默认设置
            group.alpha = 1f;
            group.ignoreParentGroups = false;

            _layers[layer] = go.transform;
        }

        /// <summary>
        /// 获取层级节点
        /// </summary>
        public Transform GetLayerNode(AsakiUILayer layer)
        {
            return _layers.TryGetValue(layer, out Transform node) ? node : transform;
        }

        /// <summary>
        /// [性能优化接口] 控制某一层是否接收输入
        /// <para>场景：打开全屏 Popup 时，调用 SetLayerRaycast(Normal, false) 物理屏蔽下层点击</para>
        /// </summary>
        public void SetLayerRaycast(AsakiUILayer layer, bool enable)
        {
            if (_layerRaycasters.TryGetValue(layer, out GraphicRaycaster raycaster))
            {
                if (raycaster.enabled != enable)
                    raycaster.enabled = enable;
            }
        }

        /// <summary>
        /// 确保场景中存在 EventSystem
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>())
                return;

            GameObject eventSystemGo = new GameObject("Asaki_EventSystem");
            DontDestroyOnLoad(eventSystemGo);
            eventSystemGo.AddComponent<EventSystem>();

            // [兼容性处理]
#if ENABLE_INPUT_SYSTEM
            // 如果你使用了 New Input System 且安装了 InputSystem 包，Unity 会自动处理或提示添加组件。
            // 这里为了最广泛的兼容性，默认添加 Standalone，New Input System 会提示 Replace。
            eventSystemGo.AddComponent<StandaloneInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
