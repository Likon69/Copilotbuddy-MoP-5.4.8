using System.Windows.Media;
using Styx.Common;
using Styx.CommonBot;

namespace Bots.DungeonBuddy.Helpers;

public class Logger
{
	public static void Write(Color color, string format, params object[] args)
	{
		Logging.Write(color, "[DungeonBuddy]: " + string.Format(format, args));
	}

	public static void Write(string format, params object[] args)
	{
		Write(string.Format(format, args));
	}

	public static void Write(string message)
	{
		Logging.Write(Colors.LightSkyBlue, "[DungeonBuddy]: " + message);
	}

	public static void WriteError(Error error)
	{
		switch (error.Type)
		{
		case ErrorType.Error:
			WriteError(error.ToString());
			break;
		case ErrorType.Warning:
			WriteWarning(error.ToString());
			break;
		}
	}

	public static void WriteDebug(string message)
	{
		Logging.WriteDiagnostic(Colors.Yellow, "[DungeonBuddy-DEBUG]: " + message);
	}

	public static void WriteDebug(string format, params object[] args)
	{
		WriteDebug(string.Format(format, args));
	}

	public static void WriteWarning(string message)
	{
		Logging.WriteVerbose(Colors.DarkOrange, "[DungeonBuddy-WARN]: " + message);
	}

	public static void WriteWarning(string format, params object[] args)
	{
		WriteWarning(string.Format(format, args));
	}

	public static void WriteError(string message)
	{
		Logging.WriteVerbose(Colors.Red, "[DungeonBuddy-ERROR]: " + message);
		TreeRoot.Stop();
	}

	public static void WriteError(string format, params object[] args)
	{
		WriteError(string.Format(format, args));
	}
}
