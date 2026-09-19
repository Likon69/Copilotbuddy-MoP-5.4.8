using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Styx
{
    [Serializable]
    public class InvalidExecutorException : Exception
    {
        public InvalidExecutorException()
        {
        }

        public InvalidExecutorException([Localizable(true)] string message)
            : base(message)
        {
        }

        public InvalidExecutorException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidExecutorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
