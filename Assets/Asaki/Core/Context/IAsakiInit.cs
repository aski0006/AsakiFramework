using UnityEngine;

namespace Asaki.Core.Context
{
    /// <summary>
    /// 无参数初始化接口。
    /// </summary>
    /// <remarks>
    /// 实现此接口的类可以在实例化后通过调用Init方法进行初始化，无需传递参数。
    /// 通常与<see cref="AsakiInitFactory"/>结合使用，实现统一的初始化模式。
    /// </remarks>
    public interface IAsakiInit
    {
        /// <summary>
        /// 执行无参数初始化操作。
        /// </summary>
        void Init(IAsakiResolver resolver = null); // 默认无参构造
    }

    /// <summary>
    /// 单参数初始化接口。
    /// </summary>
    /// <typeparam name="T1">初始化参数类型。</typeparam>
    /// <remarks>
    /// 实现此接口的类可以在实例化后通过调用Init方法进行初始化，并接收一个参数。
    /// 通常与<see cref="AsakiInitFactory"/>结合使用，实现统一的初始化模式。
    /// </remarks>
    public interface IAsakiInit<in T1>
    {
        /// <summary>
        /// 执行单参数初始化操作。
        /// </summary>
        /// <param name="args">初始化参数。</param>
        void Init(T1 args);
    }

    /// <summary>
    /// 双参数初始化接口。
    /// </summary>
    /// <typeparam name="T1">第一个初始化参数类型。</typeparam>
    /// <typeparam name="T2">第二个初始化参数类型。</typeparam>
    /// <remarks>
    /// 实现此接口的类可以在实例化后通过调用Init方法进行初始化，并接收两个参数。
    /// 通常与<see cref="AsakiInitFactory"/>结合使用，实现统一的初始化模式。
    /// </remarks>
    public interface IAsakiInit<in T1, in T2>
    {
        /// <summary>
        /// 执行双参数初始化操作。
        /// </summary>
        /// <param name="args1">第一个初始化参数。</param>
        /// <param name="args2">第二个初始化参数。</param>
        void Init(T1 args1, T2 args2);
    }

    /// <summary>
    /// 三参数初始化接口。
    /// </summary>
    /// <typeparam name="T1">第一个初始化参数类型。</typeparam>
    /// <typeparam name="T2">第二个初始化参数类型。</typeparam>
    /// <typeparam name="T3">第三个初始化参数类型。</typeparam>
    /// <remarks>
    /// 实现此接口的类可以在实例化后通过调用Init方法进行初始化，并接收三个参数。
    /// 通常与<see cref="AsakiInitFactory"/>结合使用，实现统一的初始化模式。
    /// </remarks>
    public interface IAsakiInit<in T1, in T2, in T3>
    {
        /// <summary>
        /// 执行三参数初始化操作。
        /// </summary>
        /// <param name="args1">第一个初始化参数。</param>
        /// <param name="args2">第二个初始化参数。</param>
        /// <param name="args3">第三个初始化参数。</param>
        void Init(T1 args1, T2 args2, T3 args3);
    }

    /// <summary>
    /// 四参数初始化接口。
    /// </summary>
    /// <typeparam name="T1">第一个初始化参数类型。</typeparam>
    /// <typeparam name="T2">第二个初始化参数类型。</typeparam>
    /// <typeparam name="T3">第三个初始化参数类型。</typeparam>
    /// <typeparam name="T4">第四个初始化参数类型。</typeparam>
    /// <remarks>
    /// 实现此接口的类可以在实例化后通过调用Init方法进行初始化，并接收四个参数。
    /// 通常与<see cref="AsakiInitFactory"/>结合使用，实现统一的初始化模式。
    /// </remarks>
    public interface IAsakiInit<in T1, in T2, in T3, in T4>
    {
        /// <summary>
        /// 执行四参数初始化操作。
        /// </summary>
        /// <param name="args1">第一个初始化参数。</param>
        /// <param name="args2">第二个初始化参数。</param>
        /// <param name="args3">第三个初始化参数。</param>
        /// <param name="args4">第四个初始化参数。</param>
        void Init(T1 args1, T2 args2, T3 args3, T4 args4);
    }

    /// <summary>
    /// 五参数初始化接口。
    /// </summary>
    /// <typeparam name="T1">第一个初始化参数类型。</typeparam>
    /// <typeparam name="T2">第二个初始化参数类型。</typeparam>
    /// <typeparam name="T3">第三个初始化参数类型。</typeparam>
    /// <typeparam name="T4">第四个初始化参数类型。</typeparam>
    /// <typeparam name="T5">第五个初始化参数类型。</typeparam>
    /// <remarks>
    /// 实现此接口的类可以在实例化后通过调用Init方法进行初始化，并接收五个参数。
    /// 通常与<see cref="AsakiInitFactory"/>结合使用，实现统一的初始化模式。
    /// </remarks>
    public interface IAsakiInit<in T1, in T2, in T3, in T4, in T5>
    {
        /// <summary>
        /// 执行五参数初始化操作。
        /// </summary>
        /// <param name="args1">第一个初始化参数。</param>
        /// <param name="args2">第二个初始化参数。</param>
        /// <param name="args3">第三个初始化参数。</param>
        /// <param name="args4">第四个初始化参数。</param>
        /// <param name="args5">第五个初始化参数。</param>
        void Init(T1 args1, T2 args2, T3 args3, T4 args4, T5 args5);
    }

    /// <summary>
    /// Unity MonoBehaviour实例化与初始化工厂类。
    /// </summary>
    /// <remarks>
    /// 此工厂类提供了一系列泛型方法，用于实例化MonoBehaviour对象并自动调用其Init方法进行初始化。
    /// 支持不同数量的初始化参数（0-5个），并提供位置和旋转参数的重载版本。
    /// 实现了统一的初始化模式，避免了手动调用Init方法的繁琐和可能的遗忘。
    /// </remarks>
    public static class AsakiInitFactory
    {
        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit"/>接口的MonoBehaviour预制体，并执行无参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit"/>接口。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T>(T prefab, Transform parent = null)
            where T : MonoBehaviour, IAsakiInit
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Init();
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1}"/>接口的MonoBehaviour预制体，并执行单参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1}"/>接口。</typeparam>
        /// <typeparam name="TArg1">初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="arg1">初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1>(T prefab, TArg1 arg1, Transform parent = null)
            where T : MonoBehaviour, IAsakiInit<TArg1>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Init(arg1);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1}"/>接口的MonoBehaviour预制体，指定位置和旋转，并执行单参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1}"/>接口。</typeparam>
        /// <typeparam name="TArg1">初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="position">实例化对象的世界位置。</param>
        /// <param name="rotation">实例化对象的世界旋转。</param>
        /// <param name="arg1">初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Init(arg1);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2}"/>接口的MonoBehaviour预制体，并执行双参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Init(arg1, arg2);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2}"/>接口的MonoBehaviour预制体，指定位置和旋转，并执行双参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="position">实例化对象的世界位置。</param>
        /// <param name="rotation">实例化对象的世界旋转。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Init(arg1, arg2);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2, TArg3}"/>接口的MonoBehaviour预制体，并执行三参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2, TArg3}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <typeparam name="TArg3">第三个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="arg3">第三个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2, TArg3>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2, TArg3>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Init(arg1, arg2, arg3);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2, TArg3}"/>接口的MonoBehaviour预制体，指定位置和旋转，并执行三参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2, TArg3}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <typeparam name="TArg3">第三个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="position">实例化对象的世界位置。</param>
        /// <param name="rotation">实例化对象的世界旋转。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="arg3">第三个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2, TArg3>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2, TArg3>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Init(arg1, arg2, arg3);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4}"/>接口的MonoBehaviour预制体，并执行四参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <typeparam name="TArg3">第三个初始化参数类型。</typeparam>
        /// <typeparam name="TArg4">第四个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="arg3">第三个初始化参数。</param>
        /// <param name="arg4">第四个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2, TArg3, TArg4>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Init(arg1, arg2, arg3, arg4);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4}"/>接口的MonoBehaviour预制体，指定位置和旋转，并执行四参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <typeparam name="TArg3">第三个初始化参数类型。</typeparam>
        /// <typeparam name="TArg4">第四个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="position">实例化对象的世界位置。</param>
        /// <param name="rotation">实例化对象的世界旋转。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="arg3">第三个初始化参数。</param>
        /// <param name="arg4">第四个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2, TArg3, TArg4>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Init(arg1, arg2, arg3, arg4);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4, TArg5}"/>接口的MonoBehaviour预制体，并执行五参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4, TArg5}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <typeparam name="TArg3">第三个初始化参数类型。</typeparam>
        /// <typeparam name="TArg4">第四个初始化参数类型。</typeparam>
        /// <typeparam name="TArg5">第五个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="arg3">第三个初始化参数。</param>
        /// <param name="arg4">第四个初始化参数。</param>
        /// <param name="arg5">第五个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2, TArg3, TArg4, TArg5>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Init(arg1, arg2, arg3, arg4, arg5);
            return instance;
        }

        /// <summary>
        /// 实例化实现了<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4, TArg5}"/>接口的MonoBehaviour预制体，指定位置和旋转，并执行五参数初始化。
        /// </summary>
        /// <typeparam name="T">要实例化的MonoBehaviour类型，必须实现<see cref="IAsakiInit{TArg1, TArg2, TArg3, TArg4, TArg5}"/>接口。</typeparam>
        /// <typeparam name="TArg1">第一个初始化参数类型。</typeparam>
        /// <typeparam name="TArg2">第二个初始化参数类型。</typeparam>
        /// <typeparam name="TArg3">第三个初始化参数类型。</typeparam>
        /// <typeparam name="TArg4">第四个初始化参数类型。</typeparam>
        /// <typeparam name="TArg5">第五个初始化参数类型。</typeparam>
        /// <param name="prefab">要实例化的预制体对象。</param>
        /// <param name="position">实例化对象的世界位置。</param>
        /// <param name="rotation">实例化对象的世界旋转。</param>
        /// <param name="arg1">第一个初始化参数。</param>
        /// <param name="arg2">第二个初始化参数。</param>
        /// <param name="arg3">第三个初始化参数。</param>
        /// <param name="arg4">第四个初始化参数。</param>
        /// <param name="arg5">第五个初始化参数。</param>
        /// <param name="parent">实例化对象的父变换，可选参数。</param>
        /// <returns>实例化并初始化完成的MonoBehaviour对象。</returns>
        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInit<TArg1, TArg2, TArg3, TArg4, TArg5>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Init(arg1, arg2, arg3, arg4, arg5);
            return instance;
        }
    }
}
