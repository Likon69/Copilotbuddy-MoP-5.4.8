using System;

namespace Bots.DungeonBuddy.Profiles;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class ValueRangeAttribute : Attribute
{
	/// <summary>
	/// Constructs an instance of the <see cref="ValueRangeAttribute"/>.
	/// </summary>
	/// <param name="min">The minimum, inclusive value.</param>
	/// <param name="max">The maximum, inclusive value.</param>
	public ValueRangeAttribute(object min, object max)
	{
		Min = min;
		Max = max;
	}

	public object Min { get; private set; }
	public object Max { get; private set; }
}
