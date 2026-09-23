using System.Collections.Generic;
using Styx.WoWInternals;

namespace Bots.DungeonBuddy.Helpers;

public class ScenarioStage
{
	public readonly string Description;

	public readonly string Name;

	public readonly int NumberOfSteps;

	public readonly int StageNumber;

	private readonly ScenarioCriteria[] _steps;

	internal ScenarioStage(int stageNumber)
	{
		List<string> returnValues = Lua.GetReturnValues("return C_Scenario.GetStepInfo()");
		Name = ((returnValues[0] == "nil") ? string.Empty : returnValues[0]);
		Description = ((returnValues[1] == "nil") ? string.Empty : returnValues[1]);
		NumberOfSteps = int.Parse(returnValues[2]);
		_steps = new ScenarioCriteria[NumberOfSteps];
		StageNumber = stageNumber;
		for (int i = 0; i < NumberOfSteps; i++)
		{
			_steps[i] = new ScenarioCriteria(i + 1);
		}
	}

	public ScenarioCriteria GetStep(int stepNumber)
	{
		return _steps[stepNumber - 1];
	}
}
