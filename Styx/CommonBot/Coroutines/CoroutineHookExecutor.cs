using System;
using System.Threading.Tasks;
using CommonBehaviors.Actions;
using Styx.Common;
using Styx.TreeSharp;

namespace Styx.CommonBot.Coroutines
{
    public class CoroutineHookExecutor : CoroutineTask<bool>
    {
        public CoroutineHookExecutor(string location, string description = null, Func<Task<bool>> defaultCoroutine = null)
        {
            Composite composite = defaultCoroutine == null
                ? null
                : new ActionRunCoroutine(context => defaultCoroutine());

            _hookExecutor = new HookExecutor(location, description, composite);
        }

        public override Task<bool> Run()
        {
            return _hookExecutor.ExecuteCoroutine(null);
        }

        private readonly HookExecutor _hookExecutor;
    }
}
