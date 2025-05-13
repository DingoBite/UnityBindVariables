using System;

namespace Bind
{
    public class BindProcessed<TIn, TOut> : IReadonlyBind<TOut>
    {
        public event Action<TOut> OnValueChange;

        private readonly Func<TIn, TOut> _processor;
        private readonly IReadonlyBind<TIn> _sourceBind;
        private readonly bool _equalityCheck;
        private readonly Func<TOut, TOut, bool> _equalityComparer;
        private TOut _value;

        public BindProcessed(
            IReadonlyBind<TIn> sourceBind, 
            Func<TIn, TOut> processor, 
            bool equalityCheck = false, 
            Func<TOut, TOut, bool> equalityComparer = null)
        {
            _sourceBind = sourceBind;
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _equalityCheck = equalityCheck;
            _equalityComparer = equalityComparer;

            _value = _processor(_sourceBind.GetValue());

            _sourceBind.AddListener(OnSourceValueChanged);
        }

        private void OnSourceValueChanged(TIn newValue)
        {
            var processedValue = _processor(newValue);
            var isEqual = Equal(processedValue);

            _value = processedValue;

            if (!isEqual)
                OnValueChange?.Invoke(processedValue);
        }

        public TOut V => _value;

        public void AddListener(Action<TOut> listener) => OnValueChange += listener;
        public void RemoveListener(Action<TOut> listener) => OnValueChange -= listener;

        public bool Equal(TOut value)
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

        public TOut GetValue() => V;

        public void Dispose() => _sourceBind.RemoveListener(OnSourceValueChanged);
    }
}