using System;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace Bind
{
    public static class BindExtensions
    {
        public static void UnSubscribe<TValue>(this IReadonlyBind<TValue> bind, Action<TValue> action)
        {
            bind.OnValueChange -= action;
        }
        
        public static void SafeSubscribe<TValue>(this IReadonlyAsyncBind<TValue> bind, Func<TValue, Task> action)
        {
            bind.OnValueChangeAsync -= action;
            bind.OnValueChangeAsync += action;
        }
        
        public static void UnSubscribe<TValue>(this IReadonlyAsyncBind<TValue> bind, Func<TValue, Task> action)
        {
            bind.OnValueChangeAsync -= action;
        }
        
        public static void SafeSubscribe<TValue>(this IReadonlyBind<TValue> bind, Action<TValue> action)
        {
            bind.OnValueChange -= action;
            bind.OnValueChange += action;
        }
        
        public static void SafeSubscribe<TValue>(this UnityEvent<TValue> bind, UnityAction<TValue> action)
        {
            bind.RemoveListener(action);
            bind.AddListener(action);
        }

        public static void UnSubscribe(this UnityEvent bind, UnityAction action)
        {
            bind.RemoveListener(action);
        }

        public static void SafeSubscribe(this UnityEvent bind, UnityAction action)
        {
            bind.RemoveListener(action);
            bind.AddListener(action);
        }
        
        public static void SafeBindInvoke<TValue>(this IReadonlyBind<TValue> model, UnityEvent<TValue> e)
        {
            model.OnValueChange -= e.Invoke;
            model.OnValueChange += e.Invoke;
        }
        
        public static void SafeBindInvoke<TValue>(this IReadonlyBind<TValue> model, Action<TValue> e)
        {
            model.OnValueChange -= e.Invoke;
            model.OnValueChange += e.Invoke;
        }
        
        public static void SafeBindAsyncInvoke<TValue>(this IReadonlyAsyncBind<TValue> model, Func<TValue, Task> e)
        {
            model.OnValueChangeAsync -= e.Invoke;
            model.OnValueChangeAsync += e.Invoke;
        }
    }
}