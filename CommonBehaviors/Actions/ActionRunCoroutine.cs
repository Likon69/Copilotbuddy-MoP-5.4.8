using System;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx.CommonBot.Coroutines;
using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class ActionRunCoroutine : Styx.TreeSharp.Action
    {
        private readonly Func<object, Coroutine> _coroutineProducer;
        internal readonly Func<object, Task<bool>> TaskProducer;
        private Coroutine _coroutine;

        public ActionRunCoroutine(Func<object, Coroutine> coroutineProducer)
        {
            _coroutineProducer = coroutineProducer ?? throw new ArgumentNullException("coroutineProducer");
        }

        public ActionRunCoroutine(Func<object, Task<bool>> taskProducer)
            : this(context => new Coroutine(async () => (object)await taskProducer(context)))
        {
            TaskProducer = taskProducer;
        }

        public ActionRunCoroutine(Func<object, CoroutineTask<bool>> taskProducer)
            : this(context => taskProducer(context).Run()) { }

        public ActionRunCoroutine(Func<object, Task> taskProducer)
            : this(context => new Coroutine(() => taskProducer(context))) { }

        public ActionRunCoroutine(Func<object, CoroutineTask> taskProducer)
            : this(context => taskProducer(context).Run()) { }

        public override void Start(object context)
        {
            base.Start(context);
            DisposeCoroutine();
            _coroutine = _coroutineProducer(context);
        }

        public override void Stop(object context)
        {
            DisposeCoroutine();
            base.Stop(context);
        }

        protected override RunStatus Run(object context)
        {
            _coroutine.Resume();
            switch (_coroutine.Status)
            {
                case CoroutineStatus.Runnable: return RunStatus.Running;
                case CoroutineStatus.RanToCompletion:
                    return _coroutine.Result is bool && !(bool)_coroutine.Result ? RunStatus.Failure : RunStatus.Success;
                case CoroutineStatus.Stopped:
                case CoroutineStatus.Faulted: return RunStatus.Failure;
                default: throw new Exception("Coroutine status was invalid");
            }
        }

        private void DisposeCoroutine()
        {
            if (_coroutine == null) return;
            _coroutine.Dispose();
            _coroutine = null;
        }
    }
}
