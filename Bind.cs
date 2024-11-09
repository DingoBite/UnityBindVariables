using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bind
{
    public class Bind<T> : IBind<T>
    {
        public event Action<T> OnValueChange;
        
        private T _value;

        public void AddListener(Action<T> listener) => OnValueChange += listener;
        public void RemoveListener(Action<T> listener) => OnValueChange -= listener;
        
        public Bind()
        {
        }

        public Bind(T value)
        {
            _value = value;
        }

        public T V
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChange?.Invoke(value);
            }
        }

        public void SetValue(T value) => V = value;
        public T GetValue() => V;
        
        public static implicit operator T(Bind<T> bind) => bind.V;
    }

    public class AsyncBind<T> : IAsyncBind<T>
    {
        public event Func<T, CancellationTokenSource, Task> OnValueChangeAsync;
        
        public void AddListener(Func<T, CancellationTokenSource, Task> listener) => OnValueChangeAsync += listener;
        public void RemoveListener(Func<T, CancellationTokenSource, Task> listener) => OnValueChangeAsync -= listener;
        
        public T V { get; private set; }

        public AsyncBind()
        {
        }

        public AsyncBind(T value)
        {
            V = value;
        }

        public async Task SetValueAsync(T value, CancellationTokenSource cancellationTokenSource = null)
        {
            V = value;
            if (OnValueChangeAsync == null)
                return;
            
            await OnValueChangeAsync.Invoke(value, cancellationTokenSource);
        }

        public T GetValue()
        {
            return V;
        }

        public static implicit operator T(AsyncBind<T> bind) => bind.V;
    }
}