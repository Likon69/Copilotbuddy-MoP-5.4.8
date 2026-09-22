using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Styx.CommonBot.Coroutines
{
    public abstract class CoroutineTask
    {
        public abstract Task Run();
        internal Task Task { get; set; }
        internal TaskAwaiter TaskAwaiter { get; set; }

        public CoroutineTaskAwaiter GetAwaiter()
        {
            Task = Run();
            TaskAwaiter = Task.GetAwaiter();
            return new CoroutineTaskAwaiter(this);
        }
    }
}
