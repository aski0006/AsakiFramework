using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Graphs;
using Asaki.Editor.GraphEditors;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Plugin.ComboSystem.Editor
{
    /// <summary>
    /// 连招节点视图 - 继承自AsakiNodeView
    /// </summary>
    public class ComboNodeView : AsakiNodeView
    {
        public ComboNodeView(AsakiNodeBase data, SerializedObject graphSO)
            : base(data, graphSO)
        {
            // 添加连招系统特定的样式
            AddComboStyles();

            // 根据节点类型添加额外UI
            AddTypeSpecificUI();
        }

        /// <summary>
        /// 添加连招系统样式
        /// </summary>
        private void AddComboStyles()
        {
            switch (node)
            {
                case MoveNode:
                    style.backgroundColor = new StyleColor(new Color(0.2f, 0.35f, 0.45f));
                    break;
                case TransitionNode:
                    style.backgroundColor = new StyleColor(new Color(0.35f, 0.3f, 0.25f));
                    break;
                case EntryNode:
                    style.backgroundColor = new StyleColor(new Color(0.2f, 0.45f, 0.3f));
                    break;
                case EndNode:
                    style.backgroundColor = new StyleColor(new Color(0.45f, 0.2f, 0.2f));
                    break;
                case ConditionNode:
                    style.backgroundColor = new StyleColor(new Color(0.4f, 0.3f, 0.4f));
                    break;
            }
        }

        /// <summary>
        /// 添加类型特定的UI
        /// </summary>
        void AddTypeSpecificUI()
        {
            switch (node)
            {
                case MoveNode moveNode:
                    AddMoveNodeUI(moveNode);
                    break;
                case TransitionNode transitionNode:
                    AddTransitionNodeUI(transitionNode);
                    break;
            }
        }

        /// <summary>
        /// 添加招式节点的特殊UI
        /// </summary>
        void AddMoveNodeUI(MoveNode moveNode)
        {
            // 添加预览信息
            var previewContainer = new VisualElement();
            previewContainer.style.marginTop = 4;
            previewContainer.style.marginBottom = 4;

            // 动画名称
            if (!string.IsNullOrEmpty(moveNode.MoveData.AnimationStateName))
            {
                var animLabel = new Label($"🎬 {moveNode.MoveData.AnimationStateName}");
                animLabel.style.fontSize = 10;
                animLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
                previewContainer.Add(animLabel);
            }

            // 时间信息
            float totalTime =
                moveNode.MoveData.StartupTime
                + moveNode.MoveData.ActiveDuration
                + moveNode.MoveData.RecoveryTime;
            if (totalTime > 0)
            {
                var timeLabel = new Label(
                    $"⏱️ {totalTime:F2}s (前{moveNode.MoveData.StartupTime:F2}/判{moveNode.MoveData.ActiveDuration:F2}/后{moveNode.MoveData.RecoveryTime:F2})"
                );
                timeLabel.style.fontSize = 9;
                timeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                previewContainer.Add(timeLabel);
            }

            // 判定框数量
            if (moveNode.MoveData.HitBoxes != null && moveNode.MoveData.HitBoxes.Length > 0)
            {
                var hitboxLabel = new Label($"💥 {moveNode.MoveData.HitBoxes.Length} 判定框");
                hitboxLabel.style.fontSize = 9;
                hitboxLabel.style.color = new Color(0.9f, 0.6f, 0.4f);
                previewContainer.Add(hitboxLabel);
            }

            if (previewContainer.childCount > 0)
            {
                extensionContainer.Add(previewContainer);
            }
        }

        /// <summary>
        /// 添加转换节点的特殊UI - 输入类型选择器和条件列表
        /// </summary>
        void AddTransitionNodeUI(TransitionNode transitionNode)
        {
            // 在extensionContainer中添加输入类型选择器
            var selectorContainer = new VisualElement();
            selectorContainer.style.marginTop = 4;
            selectorContainer.style.marginBottom = 4;

            // 创建下拉菜单
            var dropdown = new PopupField<string>(
                "输入类型",
                ComboInputTypeRegistry.GetAllIds().ToList(),
                transitionNode.InputType,
                id => ComboInputTypeRegistry.GetDefinition(id)?.DisplayName ?? id
            );

            dropdown.RegisterValueChangedCallback(evt =>
            {
                transitionNode.InputType = evt.newValue;
                title = transitionNode.Title;

                // 更新标题颜色
                UpdateTitleColor(evt.newValue);
            });

            selectorContainer.Add(dropdown);

            // 添加颜色指示条
            var colorBar = new VisualElement();
            colorBar.style.height = 3;
            colorBar.style.marginTop = 2;
            colorBar.name = "input-type-color-bar";
            selectorContainer.Add(colorBar);

            // 添加到extensionContainer
            extensionContainer.Add(selectorContainer);

            // 初始化颜色
            UpdateTitleColor(transitionNode.InputType);

            // 添加条件列表UI
            AddConditionsUI(transitionNode);
        }

        /// <summary>
        /// 添加条件列表UI（自定义绘制避免SerializedProperty失效）
        /// </summary>
        void AddConditionsUI(TransitionNode transitionNode)
        {
            var conditionsContainer = new VisualElement();
            conditionsContainer.style.marginTop = 8;
            conditionsContainer.name = "conditions-container";

            // 标题
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;

            var titleLabel = new Label("条件");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);

            // 添加条件按钮
            var addButton = new Button(() =>
            {
                transitionNode.AddCondition();
                RefreshConditionsList(transitionNode);
            });
            addButton.text = "+";
            addButton.style.width = 24;
            addButton.style.height = 24;
            header.Add(addButton);

            conditionsContainer.Add(header);

            // 条件列表
            var listContainer = new VisualElement();
            listContainer.name = "conditions-list";
            conditionsContainer.Add(listContainer);

            extensionContainer.Add(conditionsContainer);

            // 初始绘制
            RefreshConditionsList(transitionNode);
        }

        /// <summary>
        /// 刷新条件列表
        /// </summary>
        void RefreshConditionsList(TransitionNode transitionNode)
        {
            var listContainer = this.Q<VisualElement>("conditions-list");
            if (listContainer == null)
                return;

            listContainer.Clear();

            if (transitionNode.Conditions == null || transitionNode.Conditions.Length == 0)
            {
                var emptyLabel = new Label("（无条件）");
                emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                emptyLabel.style.fontSize = 11;
                listContainer.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < transitionNode.Conditions.Length; i++)
            {
                int index = i;
                var condition = transitionNode.Conditions[i];

                var conditionRow = new VisualElement();
                conditionRow.style.flexDirection = FlexDirection.Row;
                conditionRow.style.alignItems = Align.Center;
                conditionRow.style.marginTop = 2;
                conditionRow.style.marginBottom = 2;

                // 条件类型下拉
                var typeDropdown = new EnumField(condition.Type);
                typeDropdown.style.width = 100;
                typeDropdown.style.marginRight = 4;
                typeDropdown.RegisterValueChangedCallback(evt =>
                {
                    condition.Type = (ConditionType)evt.newValue;
                });
                conditionRow.Add(typeDropdown);

                // 参数输入
                var paramField = new TextField();
                paramField.style.width = 60;
                paramField.style.marginRight = 4;
                paramField.value = condition.Parameter ?? "";
                paramField.RegisterValueChangedCallback(evt =>
                {
                    condition.Parameter = evt.newValue;
                });
                conditionRow.Add(paramField);

                // 值输入
                var valueField = new FloatField();
                valueField.style.width = 50;
                valueField.style.marginRight = 4;
                valueField.value = condition.Value;
                valueField.RegisterValueChangedCallback(evt =>
                {
                    condition.Value = evt.newValue;
                });
                conditionRow.Add(valueField);

                // 删除按钮
                var removeButton = new Button(() =>
                {
                    transitionNode.RemoveCondition(index);
                    RefreshConditionsList(transitionNode);
                });
                removeButton.text = "-";
                removeButton.style.width = 20;
                removeButton.style.height = 20;
                conditionRow.Add(removeButton);

                listContainer.Add(conditionRow);
            }
        }

        /// <summary>
        /// 根据输入类型更新标题颜色
        /// </summary>
        void UpdateTitleColor(string inputTypeId)
        {
            var def = ComboInputTypeRegistry.GetDefinition(inputTypeId);
            if (def != null)
            {
                titleContainer.style.backgroundColor = new StyleColor(def.Color * 0.7f);

                // 更新颜色条
                var colorBar = this.Q<VisualElement>("input-type-color-bar");
                if (colorBar != null)
                {
                    colorBar.style.backgroundColor = new StyleColor(def.Color);
                }
            }
        }
    }
}
