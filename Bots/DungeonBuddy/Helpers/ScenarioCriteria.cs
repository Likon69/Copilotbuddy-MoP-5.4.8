using System.Collections.Generic;
using Styx.WoWInternals;

namespace Bots.DungeonBuddy.Helpers;

public class ScenarioCriteria
{
	public readonly int CurrentQuantity;

	public readonly bool IsComplete;

	public readonly int StepNumber;

	public readonly string Text;

	public readonly int TotalQuantity;

	internal ScenarioCriteria(int stepNumber)
	{
		StepNumber = stepNumber;
		List<string> returnValues = Lua.GetReturnValues($"return C_Scenario.GetCriteriaInfo({StepNumber})");
		Text = ((returnValues[0] == "nil") ? string.Empty : returnValues[0]);
		IsComplete = returnValues[2] == "1";
		CurrentQuantity = int.Parse(returnValues[3]);
		TotalQuantity = int.Parse(returnValues[4]);
	}
}
