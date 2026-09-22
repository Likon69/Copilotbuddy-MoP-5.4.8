using System;
using System.Threading.Tasks;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx.TreeSharp;

namespace Styx.CommonBot.Coroutines
{
    public static class CoroutineCompositeExtensions
    {
        private static async Task<bool> RunAsCoroutine(Composite composite, object context)
        {
            composite.Start(context);
            while (composite.Tick(context) == RunStatus.Running)
            {
                await Coroutine.Yield();
            }
            composite.Stop(context);
            return composite.LastStatus == RunStatus.Success;
        }

        public static Task<bool> ExecuteCoroutine(this Composite composite, object context = null)
        {
            if (composite == null)
                throw new ArgumentNullException("composite");

            var actionRunCoroutine = composite as ActionRunCoroutine;
            if (actionRunCoroutine != null && actionRunCoroutine.TaskProducer != null)
                return actionRunCoroutine.TaskProducer(context);

            return RunAsCoroutine(composite, context);
        }
    }
}
