using System;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidPathNotFoundException : Exception
{
	public AvoidPathNotFoundException(string message)
		: base(message)
	{
	}
}
