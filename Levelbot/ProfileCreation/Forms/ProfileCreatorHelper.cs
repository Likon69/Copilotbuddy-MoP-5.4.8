using System.Windows.Forms;

namespace Levelbot.ProfileCreation.Forms;

internal static class ProfileCreatorHelper
{
	public static int Clamp(int value, int min, int max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static float Clamp(float value, float min, float max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static string PromptProfileName()
	{
		return PromptProfileName(null);
	}

	public static string PromptProfileName(string defaultValue)
	{
		FormSelectProfileName formSelectProfileName = new FormSelectProfileName();
		DialogResult dialogResult = formSelectProfileName.ShowDialog();
		if (dialogResult == DialogResult.OK)
		{
			return formSelectProfileName.ProfileName;
		}
		return defaultValue;
	}
}
