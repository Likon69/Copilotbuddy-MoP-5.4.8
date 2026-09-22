using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.TreeSharp;

namespace Bots.Quest.QuestOrder
{
    public class ForcedCodeBehavior : ForcedBehavior
    {
        private readonly CustomForcedBehavior _behavior;

        public ForcedCodeBehavior(CodeNode codeNode)
        {
			if (codeNode == null)
				throw new ArgumentNullException("codeNode");

			_behavior = GetBehavior(codeNode.BehaviorType, codeNode.Arguments);

            if (_behavior == null)
                throw new Exception("Unable to create instance of UserDefinedObjective");

            _behavior.Element = codeNode.Element;
        }

		private static CustomForcedBehavior GetBehavior(Type asm, Dictionary<string, string> args)
		{
            if(asm == null)
                throw new Exception("Assembly is null!");

			if (args == null)
				args = new Dictionary<string, string>();

		    var behavior = (CustomForcedBehavior) Activator.CreateInstance(asm, args);
		    object obsoleteAttribute = behavior.GetType().GetCustomAttributes(typeof(ObsoleteAttribute), true).FirstOrDefault();
		    if (obsoleteAttribute != null)
		    {
		        Logging.Write(Colors.Red, "{0} behavior is marked as obsolete and will be removed in a future release. {1}", behavior.GetType().Name, ((ObsoleteAttribute)obsoleteAttribute).Message);
		    }
		    return behavior;
		}

        #region Overrides of ForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _behavior.Branch;
        }

        public override bool IsDone
        {
            get { return _behavior.IsDone; }
        }

        public override void OnStart()
        {
            _behavior.OnStart();
        }

        public override void OnTick()
        {
            _behavior.OnTick();
        }

        [Obsolete("Use OnFinished instead.")]
        public override void Dispose()
        {
            _behavior.Dispose();
        }

        public override void OnFinished()
        {
            _behavior.OnFinished();
        }

        #endregion
    }
}
