using System;
using System.Threading.Tasks;

namespace Bind
{
    public interface IBind<T> : IReadonlyBind<T>, IValueContainer<T>
    {
    }
    
    public interface IAsyncBind<T> : IReadonlyAsyncBind<T>, IAsyncValueContainer<T>
    {
    }

    public interface IReadonlyAsyncBind<out T>
    {
        public void AddListener(Func<T, Task> listener);
        public void RemoveListener(Func<T, Task> listener);
        
        public event Func<T, Task> OnValueChangeAsync;
        public T V { get; }
        public T GetValue();
    }
    
    public interface IReadonlyBind<out T>
    {
        public void AddListener(Action<T> listener);
        public void RemoveListener(Action<T> listener);
        public event Action<T> OnValueChange;

        public T V { get; }
        public T GetValue();
    }
    
    public interface IValueContainer<T>
    {
        public T V { get; set; }
        public void SetValue(T value);
    }
    
    public interface IAsyncValueContainer<T>
    {
        public T V { get; }
        public Task SetValueAsync(T value);
    }
    
}