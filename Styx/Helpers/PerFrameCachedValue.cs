using System;

namespace Styx.Helpers
{
    public class PerFrameCachedValue<T>
    {
        public PerFrameCachedValue(Func<T> producer)
        {
            if (producer == null)
            {
                throw new ArgumentNullException("producer");
            }

            _producer = producer;
            _lastFrameCount = uint.MaxValue;
        }

        public T Value
        {
            get
            {
                uint frameCount = StyxWoW.Memory.Executor.FrameCount;
                if (_lastFrameCount != frameCount)
                {
                    _value = _producer();
                    _lastFrameCount = frameCount;
                }

                return _value;
            }
        }

        public static implicit operator T(PerFrameCachedValue<T> cachedValue)
        {
            return cachedValue.Value;
        }

        private readonly Func<T> _producer;

        private T _value;

        private uint _lastFrameCount;
    }
}
