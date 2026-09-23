namespace Bots.DungeonBuddy.Attributes;

public enum CallBehaviorMode
{
	/// <summary>
	/// Behavior is called only While in combat
	/// </summary>
	InCombat,
	/// <summary>
	/// While in this mode the behavior will be called before pull and during Combat. Boss must be in ObjectManager.
	/// </summary>
	Proximity,
	/// <summary>
	/// While in this mode the behavior will be called when the bot starts looking for the boss and throughout combat. Boss does not need to be in objectManager so be sure to if context is null.
	/// </summary>
	CurrentBoss,
}
