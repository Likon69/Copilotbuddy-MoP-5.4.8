using System;
using System.Diagnostics;

namespace Styx.Helpers
{
    public class TimeCachedValue<T>
    {
        public TimeCachedValue(TimeSpan timeSpan, Func<T> producer)
        {
            if (producer == null)
            {
                throw new ArgumentNullException("producer");
            }

            _producer = producer;
            _timeSpan = timeSpan;
            _stopwatch = new Stopwatch();
        }

        public T Value
        {
            get
            {
                if (!_stopwatch.IsRunning || _stopwatch.Elapsed > _timeSpan)
                {
                    _value = _producer();
                    _stopwatch.Restart();
                }

                return _value;
            }
        }

        public static implicit operator T(TimeCachedValue<T> cachedValue)
        {
            return cachedValue.Value;
        }

        private readonly TimeSpan _timeSpan;

        private readonly Stopwatch _stopwatch;

        private readonly Func<T> _producer;

        private T _value;
    }
}
