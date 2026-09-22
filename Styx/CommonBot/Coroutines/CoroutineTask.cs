using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Styx.CommonBot.Coroutines
{
    public abstract class CoroutineTask<T>
    {
        public abstract Task<T> Run();
        internal Task<T> Task { get; set; }
        internal TaskAwaiter<T> TaskAwaiter { get; set; }

        public CoroutineTaskAwaiter<T> GetAwaiter()
        {
            Task = Run();
            TaskAwaiter = Task.GetAwaiter();
            return new CoroutineTaskAwaiter<T>(this);
        }
    }
}
