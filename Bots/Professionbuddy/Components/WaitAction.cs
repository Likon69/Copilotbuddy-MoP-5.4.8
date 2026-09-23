using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Threading;
using System.Threading.Tasks;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.Dynamic;
using Bots.Professionbuddy.PropertyGridUtilities;
using Bots.Professionbuddy.Localization;

namespace Bots.Professionbuddy.Components
{
	[PBXmlElement("Wait", new[] { "WaitAction" })]
    public sealed class WaitAction : DynamicallyCompiledCodeAction
    {
        private readonly Stopwatch _timeout = new Stopwatch();

        public WaitAction()
            : base(CsharpCodeType.BoolExpression)
        {
            Properties["Timeout"] = new MetaProp("Timeout", typeof (DynamicProperty<int>),
                                                 new TypeConverterAttribute(
                                                     typeof (DynamicProperty<int>.DynamivExpressionConverter)),
                                                 new DisplayNameAttribute(Strings.Action_Common_Timeout));

            Properties["Condition"] = new MetaProp("Condition", typeof (string),
                                                   new EditorAttribute(typeof (MultilineTextEditor),
                                                                       typeof (UITypeEditor)),
                                                   new DisplayNameAttribute(Strings.Action_WaitAction_Condition));

            Timeout = new DynamicProperty<int>("Timeout", this, "2000");

            Condition = "false";
            CanRunDelegate = u => false;
        }

        public Func<object,bool> CanRunDelegate { get; set; }

        [PBXmlAttribute]
        public string Condition
        {
            get { return Properties.GetValue<string>("Condition"); }
            set { Properties["Condition"].Value = value; }
        }

        [PBXmlAttribute]
        [TypeConverter(typeof (DynamicProperty<int>.DynamivExpressionConverter))]
        public DynamicProperty<int> Timeout
        {
            get { return Properties.GetValue<DynamicProperty<int>>("Timeout"); }
            set { Properties["Timeout"].Value = value; }
        }

        public override string Name
        {
            get { return Strings.Action_WaitAction_LongName; }
        }

        public override string Title
        {
            get
            {
                return string.Format("{0} ({1}) {2}:{3}",
                                     Strings.Action_WaitAction_Name, Condition,
                                     Strings.Action_Common_Timeout, Timeout);
            }
        }

	    public override string Help
        {
            get { return Strings.Action_WaitAction_Help; }
        }

        public override string Code
        {
            get { return Condition; }
        }

        public override Delegate CompiledMethod
        {
            get { return CanRunDelegate; }
            set { CanRunDelegate = (Func<object,bool>) value; }
        }

		protected async override Task Run()
		{
			if (!_timeout.IsRunning)
			{
				_timeout.Start();
			}
			try
			{
				if (_timeout.ElapsedMilliseconds >= Timeout || CanRunDelegate(this))
				{
					PBLog.Debug("Wait for {0} or until {1} Completed", TimeSpan.FromMilliseconds(Timeout), Condition);
					IsDone = true;
				}
			}
			catch (Exception ex)
			{
				if (ex.GetType() != typeof(ThreadAbortException))
					PBLog.Warn("{0}:({1})\n{2}", Strings.Action_WaitAction_Name, Condition, ex);
			}
		}

		public override void Reset()
		{
			_timeout.Reset();
			base.Reset();
		}

		public override IPBComponent DeepCopy()
		{
			return new WaitAction { Condition = Condition, Timeout = Timeout };
		}
    }
}