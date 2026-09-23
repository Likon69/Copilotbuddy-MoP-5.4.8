using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Threading;
using System.Threading.Tasks;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.Dynamic;
using Bots.Professionbuddy.PropertyGridUtilities;
using Buddy.Coroutines;
using Bots.Professionbuddy.Localization;

namespace Bots.Professionbuddy.Components
{
	[PBXmlElement("Custom", new []{"CustomAction"})]
	public sealed class CustomAction : DynamicallyCompiledCodeAction
	{
		private Coroutine _coroutine;

		public CustomAction()
			: base(CsharpCodeType.Coroutine)
		{

			Action = async c => {};
			Properties["Code"] = new MetaProp(
				"Code",
				typeof (string),
				new EditorAttribute(typeof (MultilineTextEditor), typeof (UITypeEditor)),
				new DisplayNameAttribute(Strings.Action_Common_Code));
			Code = "";
			Properties["Code"].PropertyChanged += CustomAction_PropertyChanged;
		}

		public Func<object,Task> Action { get; set; }


		[PBXmlAttribute]
		public override string Code
		{
			get { return Properties.GetValue<string>("Code"); }
			set { Properties["Code"].Value = value; }
		}

		public override string Name
		{
			get { return Strings.Action_CustomAction_Name; }
		}

		public override string Title
		{
			get { return string.Format("{0}:({1})", Strings.Action_CustomAction_Name, Code); }
		}


		public override string Help
		{
			get { return Strings.Action_CustomAction_Help; }
		}

		public override Delegate CompiledMethod
		{
			get { return Action; }
			set { Action = (Func<object, Task>)value; }
		}

		private void CustomAction_PropertyChanged(object sender, MetaPropArgs e)
		{
			DynamicCodeCompiler.CodeIsModified = true;
		}

		protected async override Task Run()
		{
			try
			{
				if (_coroutine == null)
					_coroutine = new Coroutine(() => Action(this));
				while (true)
				{
					_coroutine.Resume();
					if (_coroutine.IsFinished)
						break;
					await Coroutine.Yield();
				}
			}
			catch (Exception ex)
			{
				if (ex.GetType() != typeof (ThreadAbortException))
					PBLog.Warn("{0}:({1})\n{2}", Strings.Action_CustomAction_Name, Code, ex);
				throw;
			}
			finally
			{
				_coroutine.Dispose();
				IsDone = true;
			}
		}

		public override void Reset()
		{
			if (_coroutine != null)
			{
				if (!_coroutine.IsDisposed)
					_coroutine.Dispose();
				_coroutine = null;
			}
			base.Reset();
		}

		public override IPBComponent DeepCopy()
		{
			return new CustomAction {Code = Code};
		}
	}
}