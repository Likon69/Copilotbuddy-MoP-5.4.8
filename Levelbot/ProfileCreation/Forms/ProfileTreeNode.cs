using System.Windows.Forms;

namespace Levelbot.ProfileCreation.Forms;

internal class ProfileTreeNode : TreeNode
{
	private int _minLevel;

	private int _maxLevel;

	public string ProfileName { get; set; }

	public float MinDurability { get; set; }

	public int MinFreeBagSlots { get; set; }

	public QualityFlags SellFlags { get; set; }

	public QualityFlags MailFlags { get; set; }

	public int MinLevel
	{
		get
		{
			return _minLevel;
		}
		set
		{
			_minLevel = value;
			RefreshText();
		}
	}

	public int MaxLevel
	{
		get
		{
			return _maxLevel;
		}
		set
		{
			_maxLevel = value;
			RefreshText();
		}
	}

	public ProfileTreeNode()
		: this("")
	{
	}

	public ProfileTreeNode(string profileName)
		: base(FormatText(profileName, 1, 80))
	{
		ProfileName = profileName;
		_minLevel = 1;
		_maxLevel = 80;
		MinDurability = 0.4f;
		MinFreeBagSlots = 2;
		SellFlags = QualityFlags.Grey | QualityFlags.White;
		MailFlags = QualityFlags.Green | QualityFlags.Blue | QualityFlags.Purple;
	}

	public override string ToString()
	{
		return FormatText(ProfileName, _minLevel, _maxLevel);
	}

	public void RefreshText()
	{
		base.Text = ToString();
	}

	private static string FormatText(string profileName, int minLevel, int maxLevel)
	{
		if (minLevel == 0 && maxLevel == 0)
		{
			return profileName;
		}
		return $"{profileName} ({minLevel}-{maxLevel})";
	}
}
