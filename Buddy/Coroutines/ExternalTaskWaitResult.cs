namespace Buddy.Coroutines
{
    public struct ExternalTaskWaitResult<T>
    {
        internal static readonly ExternalTaskWaitResult<T> TimedOut = default(ExternalTaskWaitResult<T>);

        public bool Completed { get; private set; }
        public T Result { get; private set; }

        internal static ExternalTaskWaitResult<T> FromResult(T result)
        {
            return new ExternalTaskWaitResult<T> { Result = result, Completed = true };
        }
    }
}
