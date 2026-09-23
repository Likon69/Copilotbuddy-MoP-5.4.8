using System;

namespace Bots.DungeonBuddy.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class ScenarioStageAttribute : Attribute
{
	// note: optional parameters in attributes cause compiler errors in some .net 4 and lower installations.
	public ScenarioStageAttribute(int stageNumber) : this(stageNumber, "", 0) { }

	public ScenarioStageAttribute(int stageNumber, string stageName) : this(stageNumber, stageName, 0) { }

	public ScenarioStageAttribute(int stageNumber, string stageName, int stepNumber)
	{
		StageNumber = stageNumber;
		StepNumber = stepNumber;
		StageName = stageName;
	}

	public string StageName { get; set; }
	public int StageNumber { get; set; }
	public int StepNumber { get; set; }
}
