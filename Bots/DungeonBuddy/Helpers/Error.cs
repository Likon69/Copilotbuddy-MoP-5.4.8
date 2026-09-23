namespace Bots.DungeonBuddy.Helpers;

public class Error
{
	public Error(string message, ErrorType type, int lineNumber = 0, int linePosition = 0)
	{
		Message = message;
		Type = type;
		LineNumber = lineNumber;
		LinePosition = linePosition;
	}

	public string Message { get; set; }
	public ErrorType Type { get; set; }
	public int LineNumber { get; set; }
	public int LinePosition { get; set; }

	public override string ToString()
	{
		if (LineNumber != 0)
		{
			return $"[{Type}] Line {LineNumber}: {Message}";
		}
		return $"[{Type}] {Message}";
	}
}
