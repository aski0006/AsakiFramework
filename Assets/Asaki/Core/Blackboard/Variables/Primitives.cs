using System;
using Asaki.Core.Attributes;
using UnityEngine;

namespace Asaki.Core.Blackboard.Variables
{
    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiInt : AsakiValue<int>
    {
        private static readonly Func<AsakiValue<int>> _factory = () => new AsakiInt();

        public AsakiInt()
            : base(_factory) { }

        public AsakiInt(int value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiFloat : AsakiValue<float>
    {
        private static readonly Func<AsakiValue<float>> _factory = () => new AsakiFloat();

        public AsakiFloat()
            : base(_factory) { }

        public AsakiFloat(float value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiBool : AsakiValue<bool>
    {
        private static readonly Func<AsakiValue<bool>> _factory = () => new AsakiBool();

        public AsakiBool()
            : base(_factory) { }

        public AsakiBool(bool value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiString : AsakiValue<string>
    {
        private static readonly Func<AsakiValue<string>> _factory = () => new AsakiString();

        public AsakiString()
            : base(_factory) { }

        public AsakiString(string value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiVector3 : AsakiValue<Vector3>
    {
        private static readonly Func<AsakiValue<Vector3>> _factory = () => new AsakiVector3();

        public AsakiVector3()
            : base(_factory) { }

        public AsakiVector3(Vector3 value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiVector2 : AsakiValue<Vector2>
    {
        private static readonly Func<AsakiValue<Vector2>> _factory = () => new AsakiVector2();

        public AsakiVector2()
            : base(_factory) { }

        public AsakiVector2(Vector2 value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiVector2Int : AsakiValue<Vector2Int>
    {
        private static readonly Func<AsakiValue<Vector2Int>> _factory = () => new AsakiVector2Int();

        public AsakiVector2Int()
            : base(_factory) { }

        public AsakiVector2Int(Vector2Int value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiVector3Int : AsakiValue<Vector3Int>
    {
        private static readonly Func<AsakiValue<Vector3Int>> _factory = () => new AsakiVector3Int();

        public AsakiVector3Int()
            : base(_factory) { }

        public AsakiVector3Int(Vector3Int value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiColor : AsakiValue<Color>
    {
        private static readonly Func<AsakiValue<Color>> _factory = () => new AsakiColor();

        public AsakiColor()
            : base(_factory) { }

        public AsakiColor(Color value)
            : this()
        {
            Value = value;
        }
    }

    [Serializable]
    [AsakiBlackboardValueSchema]
    public class AsakiGameObject : AsakiValue<GameObject>
    {
        private static readonly Func<AsakiValue<GameObject>> _factory = () => new AsakiGameObject();

        public AsakiGameObject()
            : base(_factory) { }

        public AsakiGameObject(GameObject value)
            : this()
        {
            Value = value;
        }
    }
}
