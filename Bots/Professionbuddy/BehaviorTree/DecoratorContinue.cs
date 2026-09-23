// UberBehaviorTree aka UberTree is a behavior tree with an implementation similar to
// that of TreeSharp https://code.google.com/p/treesharp/

using System;
using System.Threading.Tasks;

namespace Bots.Professionbuddy.BehaviorTree
{
	public class DecoratorContinue : Composite
	{
		private readonly Func<object, bool> _canRunCondition;

		public Component Child
		{
			get { return Children[0]; }
		}

		public DecoratorContinue() : this(null, null) { }

		public DecoratorContinue(Func<object, bool> canRunCondition, Component component)
			: base(component)
		{
			_canRunCondition = canRunCondition;
		}

		protected virtual bool CanRun(object context)
		{
			if (_canRunCondition != null)
			{
				return _canRunCondition(context);
			}
			return true;
		}

		public override async Task<bool> Run()
		{
			if (Children.Count != 1)
			{
				throw new ApplicationException("Decorators must have only one child.");
			}
			if (CanRun(Context))
			{
				return await Child;
			}
			return true;
		}
	}
}
