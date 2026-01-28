using System;

namespace Asaki.Core.Blackboard
{
    public interface IVariableConstraint
    {
        bool IsValid(object value);
        string GetErrorMessage(object value);
    }

    [Serializable]
    public class RangeConstraint : IVariableConstraint
    {
        public float MinValue = float.MinValue;
        public float MaxValue = float.MaxValue;

        public bool IsValid(object value)
        {
            switch (value)
            {
                case int i:
                    return i >= MinValue && i <= MaxValue;
                case float f:
                    return f >= MinValue && f <= MaxValue;
                case double d:
                    return d >= MinValue && d <= MaxValue;
                default:
                    return true;
            }
        }

        public string GetErrorMessage(object value)
        {
            return $"Value {value} is outside range [{MinValue}, {MaxValue}]";
        }
    }

    [Serializable]
    public class NotNullConstraint : IVariableConstraint
    {
        public bool IsValid(object value)
        {
            return value != null;
        }

        public string GetErrorMessage(object value)
        {
            return "Value cannot be null";
        }
    }

    [Serializable]
    public class RegexConstraint : IVariableConstraint
    {
        public string Pattern;

        public bool IsValid(object value)
        {
            if (value is string str)
            {
                return System.Text.RegularExpressions.Regex.IsMatch(str, Pattern);
            }
            return true;
        }

        public string GetErrorMessage(object value)
        {
            return $"Value '{value}' does not match pattern '{Pattern}'";
        }
    }
}
