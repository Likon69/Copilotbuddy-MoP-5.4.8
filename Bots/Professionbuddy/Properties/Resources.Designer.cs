using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Bots.Professionbuddy.Properties
{
	[DebuggerNonUserCode]
	[CompilerGenerated]
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	internal class Resources
	{
		private static ResourceManager resourceManager;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(resourceManager, null))
				{
					ResourceManager temp = new ResourceManager("Bots.Professionbuddy.Properties.Resources", typeof(Resources).Assembly);
					resourceManager = temp;
				}
				return resourceManager;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get { return resourceCulture; }
			set { resourceCulture = value; }
		}

		internal static Bitmap add
		{
			get { return (Bitmap)ResourceManager.GetObject("add", resourceCulture); }
		}

		internal static Bitmap copy
		{
			get { return (Bitmap)ResourceManager.GetObject("copy", resourceCulture); }
		}

		internal static Bitmap cut
		{
			get { return (Bitmap)ResourceManager.GetObject("cut", resourceCulture); }
		}

		internal static Bitmap delete
		{
			get { return (Bitmap)ResourceManager.GetObject("delete", resourceCulture); }
		}

		internal static Bitmap help
		{
			get { return (Bitmap)ResourceManager.GetObject("help", resourceCulture); }
		}

		internal static Bitmap open
		{
			get { return (Bitmap)ResourceManager.GetObject("open", resourceCulture); }
		}

		internal static Bitmap paste
		{
			get { return (Bitmap)ResourceManager.GetObject("paste", resourceCulture); }
		}

		internal static Bitmap rightarrow
		{
			get { return (Bitmap)ResourceManager.GetObject("rightarrow", resourceCulture); }
		}

		internal static Bitmap save
		{
			get { return (Bitmap)ResourceManager.GetObject("save", resourceCulture); }
		}

		internal Resources()
		{
		}
	}
}
