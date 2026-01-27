using System;
using Asaki.Core.Blackboard.Variables;
using UnityEngine;

namespace Asaki.Core.Blackboard
{
	[Serializable]
	public class AsakiVariableDef
	{
		public string Name;

		[SerializeReference]
		public AsakiValueBase ValueData;

		[SerializeReference]
		public AsakiValueBase DefaultValue;

		public bool IsExposed = false;

		[SerializeReference]
		public IVariableConstraint Constraint;

		public void ResetToDefault()
		{
			if (DefaultValue != null)
			{
				ValueData = DefaultValue.Clone();
			}
		}

		public bool Validate(object value)
		{
			return Constraint?.IsValid(value) ?? true;
		}

		public string TypeName
		{
			get
			{
				if (ValueData != null)
					return ValueData.TypeName;
				if (DefaultValue != null)
					return DefaultValue.TypeName;
				return "Unknown";
			}
		}
	}
}
