using System.Collections.Generic;
using Styx.WoWInternals;

namespace Bots.DungeonBuddy.Helpers;

public class ScenarioInfo
{
	public readonly ScenarioStage CurrentStage;

	public readonly int CurrentStageNumber;

	public readonly bool HasBonusStep;

	public readonly bool IsBonusStepComplete;

	public readonly string Name;

	public readonly int NumberOfStages;

	public bool IsComplete => CurrentStageNumber > NumberOfStages;

	internal ScenarioInfo()
	{
		List<string> returnValues = Lua.GetReturnValues("return C_Scenario.GetInfo()");
		Name = ((returnValues[0] == "nil") ? string.Empty : returnValues[0]);
		CurrentStageNumber = int.Parse(returnValues[1]);
		NumberOfStages = int.Parse(returnValues[2]);
		CurrentStage = new ScenarioStage(CurrentStageNumber);
		HasBonusStep = returnValues[4] == "1";
		IsBonusStepComplete = returnValues[5] == "1";
	}
}
