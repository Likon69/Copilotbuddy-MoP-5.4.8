using System.Windows.Media;
using Bots.DungeonBuddy.Helpers;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Behaviors;

public class ActionLogger : Action
{
	private readonly Color _color;

	private readonly string _format;

	private readonly object[] _args;

	private readonly bool _useColor;

	public ActionLogger(string format, params object[] args)
	{
		_format = format;
		_args = args;
	}

	public ActionLogger(Color color, string format, params object[] args)
		: this(format, args)
	{
		_color = color;
		_useColor = true;
	}

	protected override RunStatus Run(object context)
	{
		if (_useColor)
		{
			Logger.Write(_color, _format, _args);
		}
		else
		{
			Logger.Write(_format, _args);
		}
		if (base.Parent != null && base.Parent is Selector)
		{
			return RunStatus.Failure;
		}
		return RunStatus.Success;
	}
}
