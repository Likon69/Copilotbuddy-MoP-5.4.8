using System.Collections.Generic;
using System.Linq;

namespace Bots.DungeonBuddy.Helpers;

public class ErrorCollection : List<Error>
{
	public bool HasErrors => this.Any(error => error.Type == ErrorType.Error);

	public bool HasWarnings => this.Any(error => error.Type == ErrorType.Warning);

	public int ErrorCount => this.Count(error => error.Type == ErrorType.Error);

	public int WarningCount => this.Count(error => error.Type == ErrorType.Warning);

	public IEnumerable<Error> Errors => this.Where(error => error.Type == ErrorType.Error);

	public IEnumerable<Error> Warnings => this.Where(error => error.Type == ErrorType.Warning);

	public ErrorCollection()
	{
	}

	public ErrorCollection(int capacity)
		: base(capacity)
	{
	}

	public ErrorCollection(IEnumerable<Error> collection)
		: base(collection)
	{
	}
}
