using System;

namespace Bots.DungeonBuddy.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class EncounterHandlerAttribute : Attribute
{
	/// <summary>Initializes a new instance of the <see cref="EncounterHandlerAttribute"/> class.</summary>
	/// <param name="bossEntry">The boss entry id.</param>
	public EncounterHandlerAttribute(int bossEntry) : this(bossEntry, "") { }

	/// <summary>Initializes a new instance of the <see cref="EncounterHandlerAttribute"/> class.</summary>
	/// <param name="bossEntry">The boss entry id.</param>
	/// <param name="bossDisplayName">Display name of the boss.</param>
	public EncounterHandlerAttribute(int bossEntry, string bossDisplayName)
	{
		BossEntry = bossEntry;
		BossName = bossDisplayName;
		BossRange = 75;
	}

	/// <summary>Initializes a new instance of the <see cref="EncounterHandlerAttribute"/> class.</summary>
	/// <param name="bossEntry">The boss entry id.</param>
	/// <param name="bossDisplayName">Display name of the boss.</param>
	/// <param name="bossRange">The boss range. Default (75).</param>
	/// <param name="mode">The mode. Default(CallBehavior.InCombat)</param>
	public EncounterHandlerAttribute(int bossEntry, string bossDisplayName, int bossRange, CallBehaviorMode mode)
	{
		BossEntry = bossEntry;
		BossName = bossDisplayName;
		BossRange = bossRange;
		Mode = mode;
	}

	public string BossName { get; set; }
	public int BossEntry { get; set; }
	public int BossRange { get; set; }
	public int BossRangeSqr { get { return BossRange * BossRange; } }
	public CallBehaviorMode Mode { get; set; }
}
