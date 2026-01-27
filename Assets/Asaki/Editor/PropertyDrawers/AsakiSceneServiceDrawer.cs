using Asaki.Core.Context;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.PropertyDrawers
{
	/// <summary>
	/// 自定义 PropertyDrawer，限制只能拖入实现了 IAsakiSceneService 的 MonoBehaviour
	/// </summary>
	[CustomPropertyDrawer(typeof(MonoBehaviour), true)]
	public class AsakiSceneServiceDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// 只处理 Object 引用类型
			if (property.propertyType != SerializedPropertyType.ObjectReference)
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}

			// 检查是否在 AsakiSceneContext 的 _behaviourServices 列表中
			bool isInBehaviourServicesList = property.propertyPath.Contains("_behaviourServices");

			if (!isInBehaviourServicesList)
			{
				// 不是我们要处理的属性，使用默认绘制
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}

			// 自定义绘制，限制类型
			EditorGUI.BeginProperty(position, label, property);

			// 绘制对象字段
			Object newValue = EditorGUI.ObjectField(
				position,
				label,
				property.objectReferenceValue,
				typeof(MonoBehaviour),
				true);

			// 验证新值
			if (newValue != property.objectReferenceValue)
			{
				if (newValue == null)
				{
					// 允许设置为 null
					property.objectReferenceValue = null;
				}
				else if (newValue is IAsakiSceneService)
				{
					// 验证通过，允许设置
					property.objectReferenceValue = newValue;
				}
				else
				{
					// 验证失败，显示错误
					EditorUtility.DisplayDialog(
						"Invalid Service Type",
						$"{newValue.GetType().Name} does not implement IAsakiSceneService!\n\n" +
						"Only MonoBehaviour components that implement IAsakiSceneService can be added here.",
						"OK");
				}
			}

			EditorGUI.EndProperty();
		}
	}
}
