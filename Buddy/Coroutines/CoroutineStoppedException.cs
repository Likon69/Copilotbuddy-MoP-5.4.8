using System;

namespace Buddy.Coroutines
{
    public class CoroutineStoppedException : Exception
    {
        internal CoroutineStoppedException(string message) : base(message)
        {
        }
    }
}
