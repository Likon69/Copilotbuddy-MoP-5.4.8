using System;

namespace Buddy.Coroutines
{
    public class CoroutineUnhandledException : CoroutineException
    {
        public CoroutineUnhandledException(string message) : base(message)
        {
        }

        public CoroutineUnhandledException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
