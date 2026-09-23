using System.ComponentModel;
using System.Drawing.Design;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.Dynamic;
using Bots.Professionbuddy.PropertyGridUtilities;
using Bots.Professionbuddy.Localization;

namespace Bots.Professionbuddy.Components
{
	[PBXmlElement("Define", new[] { "Declaration" })]
	public sealed class DefineAction : DynamicallyCompiledCodeAction
	{
		public DefineAction()
			: base(CsharpCodeType.Declaration)
		{
			Properties["Code"] = new MetaProp(
				"Code",
				typeof (string),
				new EditorAttribute(typeof (MultilineTextEditor), typeof (UITypeEditor)),
				new DisplayNameAttribute(Strings.Action_Common_Code));

			Code = "";
			Properties["Code"].PropertyChanged += Code_PropertyChanged;
		}

		[PBXmlAttribute]
		public override string Code
		{
			get { return Properties.GetValue<string>("Code"); }
			set { Properties["Code"].Value = value; }
		}

		public override string Name
		{
			get { return Strings.Action_Declaration_Name; }
		}

		public override string Title
		{
			get { return string.Format("{0}:({1})", Name, Code); }
		}

		public override string Help
		{
			get { return Strings.Action_Declaration_Help; }
		}

		private void Code_PropertyChanged(object sender, MetaPropArgs e)
		{
			DynamicCodeCompiler.CodeIsModified = true;
		}

		public override IPBComponent DeepCopy()
		{
			return new DefineAction {Code = Code};
		}

		public override bool IsDone
		{
			get { return true; }
		}
	}
}
