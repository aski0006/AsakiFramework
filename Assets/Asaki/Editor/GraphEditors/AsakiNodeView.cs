using System.Collections.Generic;
using Asaki.Core.Graphs;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.GraphEditors
{
    /// <summary>
    /// 节点视图（性能优化：零反射、对象池复用）
    /// </summary>
    public class AsakiNodeView : Node
    {
        public AsakiNodeBase node;
        public List<Port> Inputs = new List<Port>();
        public List<Port> Outputs = new List<Port>();

        // Graph的SerializedObject引用
        private SerializedObject _graphSerializedObject;

        // ★ 新增：PropertyField对象池，避免重复创建（极致优化）
        private Dictionary<string, PropertyField> _fieldPool =
            new Dictionary<string, PropertyField>();

        public AsakiNodeView(AsakiNodeBase data, SerializedObject graphSO)
        {
            node = data;
            _graphSerializedObject = graphSO;
            viewDataKey = data.GUID;

            // 设置位置
            style.left = data.Position.x;
            style.top = data.Position.y;

            if (data is AsakiGetVariableNode getNode && getNode.IsGlobalVariable)
            {
                title = $"[Global] {data.Title}";
                // 全局节点特殊样式
                style.backgroundColor = new StyleColor(new Color(0.3f, 0.4f, 0.2f));
            }
            else
            {
                title = data.Title;
            }

            // 1. 使用缓存生成端口（O(1)）
            GeneratePortsFromCache();

            // 2. 绘制节点内容（零反射）
            CreateExtension();

            base.expanded = true;
            RefreshExpandedState();
        }

        /// <summary>
        /// 从缓存生成端口（O(1) 访问）
        /// </summary>
        private void GeneratePortsFromCache()
        {
            var ports = AsakiGraphTypeCache.GetPorts(node.GetType());

            foreach (PortInfo info in ports)
            {
                Direction direction = info.IsInput ? Direction.Input : Direction.Output;
                Port.Capacity capacity = info.AllowMultiple
                    ? Port.Capacity.Multi
                    : Port.Capacity.Single;

                // 端口类型用于UI Toolkit的类型检查
                Port port = InstantiatePort(
                    Orientation.Horizontal,
                    direction,
                    capacity,
                    info.DataType
                );

                port.portName = info.PortName;
                port.userData = info.PortName; // 存储端口名用于连线匹配

                if (info.IsInput)
                {
                    inputContainer.Add(port);
                    Inputs.Add(port);
                }
                else
                {
                    outputContainer.Add(port);
                    Outputs.Add(port);
                }
            }
        }

        /// <summary>
        /// 创建节点扩展内容（零反射、零GC Alloc）
        /// </summary>
        private void CreateExtension()
        {
            if (_graphSerializedObject == null)
                return;

            _graphSerializedObject.Update();

            // ★ 优化：一次性获取节点SerializedProperty
            SerializedProperty myProp = FindNodeProperty();
            if (myProp == null)
                return;

            // ★ 核心优化：使用缓存的可绘制字段信息（O(1)）
            var drawableFields = AsakiGraphTypeCache.GetDrawableFields(node.GetType());

            foreach (FieldDrawInfo fieldInfo in drawableFields)
            {
                // 直接通过字段名查找，Unity内部使用哈希查找，O(1)
                SerializedProperty prop = myProp.FindPropertyRelative(fieldInfo.FieldName);
                if (prop != null)
                {
                    // ★ 复用PropertyField（避免重复创建）
                    PropertyField field = GetOrCreatePropertyField(prop);
                    extensionContainer.Add(field);
                }
            }
        }

        /// <summary>
        /// 找到当前节点对应的SerializedProperty（只做一次）
        /// </summary>
        private SerializedProperty FindNodeProperty()
        {
            SerializedProperty nodesProp = _graphSerializedObject.FindProperty("Nodes");
            if (nodesProp == null || !nodesProp.isArray)
                return null;

            // ★ 使用GUID比对，避免managedReferenceValue的GC
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                SerializedProperty element = nodesProp.GetArrayElementAtIndex(i);
                SerializedProperty guidProp = element.FindPropertyRelative("GUID");
                if (guidProp != null && guidProp.stringValue == node.GUID)
                {
                    return element;
                }
            }
            return null;
        }

        /// <summary>
        /// 从对象池获取PropertyField（极致优化）
        /// </summary>
        private PropertyField GetOrCreatePropertyField(SerializedProperty prop)
        {
            // 尝试复用已存在的PropertyField
            if (_fieldPool.TryGetValue(prop.propertyPath, out PropertyField field))
            {
                field.BindProperty(prop); // 重新绑定到新属性
                return field;
            }

            // 创建新的并加入池
            field = new PropertyField(prop);
            field.Bind(_graphSerializedObject);
            _fieldPool[prop.propertyPath] = field;
            return field;
        }

        /// <summary>
        /// 同步节点位置到数据模型
        /// </summary>
        public void SyncPosition()
        {
            node.Position = GetPosition().position;
        }

        /// <summary>
        /// 清理资源（在节点销毁时调用）
        /// </summary>
        public void Cleanup()
        {
            _fieldPool.Clear(); // 清空对象池
        }

        // ================================================================
        // ★ Debug Visuals（保持不变）
        // ================================================================

        private const float HIGHLIGHT_DURATION = 0.5f;
        private float _highlightTime = 0f;
        private bool _isActive = false;

        public void Highlight()
        {
            _isActive = true;
            _highlightTime = (float)EditorApplication.timeSinceStartup;

            style.borderLeftColor = new StyleColor(Color.cyan);
            style.borderRightColor = new StyleColor(Color.cyan);
            style.borderTopColor = new StyleColor(Color.cyan);
            style.borderBottomColor = new StyleColor(Color.cyan);
            style.borderLeftWidth = 2f;
            style.borderRightWidth = 2f;
            style.borderTopWidth = 2f;
            style.borderBottomWidth = 2f;

            EditorApplication.update -= UpdateHighlight;
            EditorApplication.update += UpdateHighlight;
        }

        private void UpdateHighlight()
        {
            if (!_isActive)
                return;

            float elapsed = (float)EditorApplication.timeSinceStartup - _highlightTime;

            if (elapsed > HIGHLIGHT_DURATION)
            {
                ResetStyle();
                _isActive = false;
                EditorApplication.update -= UpdateHighlight;
            }
        }

        private void ResetStyle()
        {
            style.borderLeftColor = new StyleColor(Color.clear);
            style.borderRightColor = new StyleColor(Color.clear);
            style.borderTopColor = new StyleColor(Color.clear);
            style.borderBottomColor = new StyleColor(Color.clear);
            style.borderLeftWidth = 0f;
        }
    }
}
