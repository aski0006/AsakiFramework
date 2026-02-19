using UnityEngine;

namespace Asaki.Core.Context
{
    public interface IAsakiInject
    {
        void Inject(IAsakiResolver resolver = null);
    }

    public interface IAsakiInject<in T1>
    {
        void Inject(T1 args);
    }

    public interface IAsakiInject<in T1, in T2>
    {
        void Inject(T1 args1, T2 args2);
    }

    public interface IAsakiInject<in T1, in T2, in T3>
    {
        void Inject(T1 args1, T2 args2, T3 args3);
    }

    public interface IAsakiInject<in T1, in T2, in T3, in T4>
    {
        void Inject(T1 args1, T2 args2, T3 args3, T4 args4);
    }

    public interface IAsakiInject<in T1, in T2, in T3, in T4, in T5>
    {
        void Inject(T1 args1, T2 args2, T3 args3, T4 args4, T5 args5);
    }

    public interface IAsakiInject<in T1, in T2, in T3, in T4, in T5, in T6>
    {
        void Inject(T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6);
    }

    public interface IAsakiInject<in T1, in T2, in T3, in T4, in T5, in T6, in T7>
    {
        void Inject(T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7);
    }

    public interface IAsakiInject<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>
    {
        void Inject(T1 args1, T2 args2, T3 args3, T4 args4, T5 args5, T6 args6, T7 args7, T8 args8);
    }

    public interface IAsakiInject<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>
    {
        void Inject(
            T1 args1,
            T2 args2,
            T3 args3,
            T4 args4,
            T5 args5,
            T6 args6,
            T7 args7,
            T8 args8,
            T9 args9
        );
    }

    public interface IAsakiInject<
        in T1,
        in T2,
        in T3,
        in T4,
        in T5,
        in T6,
        in T7,
        in T8,
        in T9,
        in T10
    >
    {
        void Inject(
            T1 args1,
            T2 args2,
            T3 args3,
            T4 args4,
            T5 args5,
            T6 args6,
            T7 args7,
            T8 args8,
            T9 args9,
            T10 args10
        );
    }

    public static class AsakiInjectFactory
    {
        public static T Instantiate<T>(T prefab, Transform parent = null)
            where T : MonoBehaviour, IAsakiInject
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject();
            return instance;
        }

        public static T Instantiate<T, TArg1>(T prefab, TArg1 arg1, Transform parent = null)
            where T : MonoBehaviour, IAsakiInject<TArg1>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1);
            return instance;
        }

        public static T Instantiate<T, TArg1>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3, arg4);
            return instance;
        }

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
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3, arg4);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5);
            return instance;
        }

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
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            Transform parent = null
        )
            where T : MonoBehaviour, IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            TArg8 arg8,
            Transform parent = null
        )
            where T : MonoBehaviour,
                IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            return instance;
        }

        public static T Instantiate<T, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            TArg8 arg8,
            Transform parent = null
        )
            where T : MonoBehaviour,
                IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            return instance;
        }

        public static T Instantiate<
            T,
            TArg1,
            TArg2,
            TArg3,
            TArg4,
            TArg5,
            TArg6,
            TArg7,
            TArg8,
            TArg9
        >(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            TArg8 arg8,
            TArg9 arg9,
            Transform parent = null
        )
            where T : MonoBehaviour,
                IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            return instance;
        }

        public static T Instantiate<
            T,
            TArg1,
            TArg2,
            TArg3,
            TArg4,
            TArg5,
            TArg6,
            TArg7,
            TArg8,
            TArg9
        >(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            TArg8 arg8,
            TArg9 arg9,
            Transform parent = null
        )
            where T : MonoBehaviour,
                IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            return instance;
        }

        public static T Instantiate<
            T,
            TArg1,
            TArg2,
            TArg3,
            TArg4,
            TArg5,
            TArg6,
            TArg7,
            TArg8,
            TArg9,
            TArg10
        >(
            T prefab,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            TArg8 arg8,
            TArg9 arg9,
            TArg10 arg10,
            Transform parent = null
        )
            where T : MonoBehaviour,
                IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            return instance;
        }

        public static T Instantiate<
            T,
            TArg1,
            TArg2,
            TArg3,
            TArg4,
            TArg5,
            TArg6,
            TArg7,
            TArg8,
            TArg9,
            TArg10
        >(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            TArg6 arg6,
            TArg7 arg7,
            TArg8 arg8,
            TArg9 arg9,
            TArg10 arg10,
            Transform parent = null
        )
            where T : MonoBehaviour,
                IAsakiInject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>
        {
            T instance = Object.Instantiate(prefab, position, rotation, parent);
            instance.Inject(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            return instance;
        }
    }
}
