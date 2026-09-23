using System;

namespace Bots.DungeonBuddy.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class ObjectHandlerAttribute : Attribute
{
	public ObjectHandlerAttribute(int objectEntryId) : this(objectEntryId, "", 40f) { }

	public ObjectHandlerAttribute(int objectEntryId, string objectDisplayName)
		: this(objectEntryId, objectDisplayName, 40f) { }

	public ObjectHandlerAttribute(int objectEntryId, string objectDisplayName, float range)
	{
		ObjectEntry = objectEntryId;
		ObjectName = objectDisplayName;
		ObjectRange = range;
	}

	public string ObjectName { get; set; }
	public int ObjectEntry { get; set; }
	public float ObjectRange { get; set; }
	public float ObjectRangeSqr { get { return ObjectRange * ObjectRange; } }
}
