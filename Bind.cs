using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bind
{
    public class Bind<T> : IBind<T>
    {
        public event Action<T> OnValueChange;

        private readonly bool _equalityCheck;
        private readonly Func<T, T, bool> _equalityComparer;
        private T _value;

        public void AddListener(Action<T> listener) => OnValueChange += listener;
        public void RemoveListener(Action<T> listener) => OnValueChange -= listener;
        
        public Bind(bool equalityCheck = false, Func<T, T, bool> equalityComparer = null)
        {
            _equalityCheck = equalityCheck;
            _equalityComparer = equalityComparer;
        }

        public Bind(T value, bool equalityCheck = false, Func<T, T, bool> equalityComparer = null)
        {
            _equalityCheck = equalityCheck;
            _equalityComparer = equalityComparer;
            _value = value;
        }

        public T V
        {
            get => _value;
            set
            {
                var isEqual = Equal(value);
                _value = value;
                if (!isEqual)
                    OnValueChange?.Invoke(value);
            }
        }

        public bool Equal(T value)
        {
            if (!_equalityCheck)
                return false;
            
            if (_value == null && value != null || _value != null && value == null)
                return false;
            if (_equalityComparer != null && _equalityComparer(_value, value))
                return false;
            if (_value != null && value != null && !_value.Equals(value))
                return false;
            return true;
        }
        
        public void SetValue(T value) => V = value;
        public T GetValue() => V;
    }

    public class AsyncBind<T> : IAsyncBind<T>
    {
        public event Func<T, CancellationTokenSource, Task> OnValueChangeAsync;
        
        public void AddListener(Func<T, CancellationTokenSource, Task> listener) => OnValueChangeAsync += listener;
        public void RemoveListener(Func<T, CancellationTokenSource, Task> listener) => OnValueChangeAsync -= listener;
        
        public T V { get; private set; }
        
        private readonly bool _equalityCheck;
        private readonly Func<T, T, bool> _equalityComparer;

        public AsyncBind(bool equalityCheck = false, Func<T, T, bool> equalityComparer = null)
        {
            _equalityCheck = equalityCheck;
            _equalityComparer = equalityComparer;
        }

        public AsyncBind(T value, bool equalityCheck = false, Func<T, T, bool> equalityComparer = null)
        {
            _equalityCheck = equalityCheck;
            _equalityComparer = equalityComparer;
            V = value;
        }

        public async Task SetValueAsync(T value, CancellationTokenSource cancellationTokenSource = null)
        {
            V = value;
            var isEqual = Equal(value);
            if (!isEqual && OnValueChangeAsync != null)
                await OnValueChangeAsync.Invoke(value, cancellationTokenSource);
        }

        public bool Equal(T value)
        {
            if (!_equalityCheck)
                return false;
            
            if (V == null && value != null || V != null && value == null)
                return false;
            if (_equalityComparer != null && _equalityComparer(V, value))
                return false;
            if (V != null && value != null && !V.Equals(value))
                return false;
            return true;
        }
        
        public T GetValue()
        {
            return V;
        }

        public static implicit operator T(AsyncBind<T> bind) => bind.V;
    }
}