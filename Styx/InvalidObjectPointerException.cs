using System;
using System.Runtime.Serialization;

namespace Styx
{
    [Serializable]
    public class InvalidObjectPointerException : Exception
    {
        public InvalidObjectPointerException()
        {
        }

        public InvalidObjectPointerException(string message)
            : base(message)
        {
        }

        public InvalidObjectPointerException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidObjectPointerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
