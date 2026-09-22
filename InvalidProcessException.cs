using System;
using System.Runtime.Serialization;

namespace CopilotBuddy
{
    [Serializable]
    public class InvalidProcessException : Exception
    {
        public InvalidProcessException()
        {
        }

        public InvalidProcessException(string message)
            : base(message)
        {
        }

        public InvalidProcessException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidProcessException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
