using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.PropertyGridUtilities;
using Bots.Professionbuddy.Localization;

namespace Bots.Professionbuddy.Components
{
	[PBXmlElement("CallSubRoutine")]
    public sealed class CallSubRoutineAction : PBAction
    {
        private SubRoutineComposite _sub;
        private bool _subReset;

	    public CallSubRoutineAction()
	    {
		    Properties["SubRoutineName"] = new MetaProp(
			    "SubRoutineName",
				    typeof (string),
				    new DisplayNameAttribute(
						Strings.Action_Common_SubRoutineName));
		    SubRoutineName = "";
	    }

        [PBXmlAttribute]
        public string SubRoutineName
        {
            get { return Properties.GetValue<string>("SubRoutineName"); }
            set { Properties["SubRoutineName"].Value = value; }
        }

        public override string Name
        {
            get { return Strings.Action_CallSubRoutine_Name; }
        }

        public override string Title
        {
            get { return string.Format("{0}: {1}()", Name, SubRoutineName); }
        }

        public override string Help
        {
            get { return Strings.Action_CallSubRoutine_Help; }
        }

		protected async override Task Run()
		{
			if (_sub == null && !SubRoutineComposite.GetSubRoutineMyName(SubRoutineName, out _sub))
			{
				PBLog.Warn("{0}: {1}.", Strings.Error_SubroutineNotFound, SubRoutineName);
				IsDone = true;
				return;
			}

			using (SubRoutineComposite.Activate(_sub))
			{
				try
				{
					if (!_subReset)
					{
						_sub.Reset();
						_subReset = true;
					}
					await _sub;
				}
				finally
				{
					IsDone = _sub.IsDone;
				}
			}
		}

		public override void Reset()
		{
			_subReset = false;
			base.Reset();
		}

	    public override IPBComponent DeepCopy()
	    {
			return new CallSubRoutineAction { SubRoutineName = SubRoutineName };
	    }

	}
}