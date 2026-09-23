using System;
using System.Windows.Forms;
using CommonBehaviors.Actions;
using NewMixedMode.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Localization;
using Styx.TreeSharp;

namespace NewMixedMode;

public class MixedModeEx : BotBase
{
	private BotBase _currentBot;

	private BotBase _primaryBot;

	private BotBase _secondaryBot;

	public static MixedModeEx Instance { get; private set; }

	public MixedModeSettings Settings { get; set; }

	public BotBase PrimaryBot
	{
		get
		{
			return _primaryBot;
		}
		set
		{
			if (value == _primaryBot)
			{
				return;
			}
			try
			{
				if (_primaryBot != null)
				{
					_primaryBot.OnDeselected();
				}
				value?.OnSelected();
			}
			catch (Exception ex)
			{
				Logging.WriteException(ex);
			}
			finally
			{
				_primaryBot = value;
			}
		}
	}

	public BotBase SecondaryBot
	{
		get
		{
			return _secondaryBot;
		}
		set
		{
			if (value == _secondaryBot)
			{
				return;
			}
			try
			{
				if (_secondaryBot != null)
				{
					_secondaryBot.OnDeselected();
				}
				value?.OnSelected();
			}
			catch (Exception ex)
			{
				Logging.WriteException(ex);
			}
			finally
			{
				_secondaryBot = value;
			}
		}
	}

	private BotBase CurrentBot
	{
		get
		{
			return _currentBot;
		}
		set
		{
			if (value != null && (_currentBot == null || _currentBot.Name != value.Name))
			{
				Logging.Write(NewMixedModeResources.MixedModeEx_CurrentBot_Switching_bot_to_ + value.Name);
				if (_currentBot != null)
				{
					_currentBot.Stop();
				}
				BotPoi.Clear("Switching bot bases");
				_currentBot = value;
				_currentBot.Start();
			}
		}
	}

	public override bool RequiresProfileScope
	{
		get
		{
			if (PrimaryBot != null)
			{
				return PrimaryBot.RequiresProfileScope;
			}
			return true;
		}
	}

	public override string Name => Globalization.MixedMode_Name;

	public override Composite Root
	{
		get
		{
			if (CurrentBot == null)
			{
				return new ActionAlwaysSucceed();
			}
			return CurrentBot.Root;
		}
	}

	public override PulseFlags PulseFlags
	{
		get
		{
			if (CurrentBot == null)
			{
				return PulseFlags.All;
			}
			return CurrentBot.PulseFlags;
		}
	}

	public override Form ConfigurationForm => new FormChooser();

	public MixedModeEx()
	{
		Instance = this;
		Settings = new MixedModeSettings();
	}

	public override void Pulse()
	{
		CurrentBot = (SecondaryBot.RequirementsMet ? SecondaryBot : PrimaryBot);
		if (CurrentBot != null)
		{
			CurrentBot.Pulse();
		}
	}

	public override void Start()
	{
		if (ProfileManager.CurrentProfile == null)
		{
			ProfileManager.LoadEmpty();
		}
		if (!BotManager.Instance.Bots.ContainsKey(Settings.PrimaryBot))
		{
			throw new HonorbuddyUnableToStartException(NewMixedModeResources.MixedModeEx_Start_You_have_not_selected_a_primary_bot_);
		}
		if (!BotManager.Instance.Bots.ContainsKey(Settings.SecondaryBot))
		{
			throw new HonorbuddyUnableToStartException(NewMixedModeResources.MixedModeEx_Start_You_have_not_selected_a_secondary_bot_);
		}
		PrimaryBot = BotManager.Instance.Bots[Settings.PrimaryBot];
		SecondaryBot = BotManager.Instance.Bots[Settings.SecondaryBot];
		Initialize();
	}

	public override void Initialize()
	{
		if (PrimaryBot != null)
		{
			PrimaryBot.DoInitialize();
		}
		if (SecondaryBot != null)
		{
			SecondaryBot.DoInitialize();
		}
	}

	public override void OnSelected()
	{
		if (PrimaryBot != null)
		{
			PrimaryBot.OnSelected();
		}
		if (SecondaryBot != null)
		{
			SecondaryBot.OnSelected();
		}
	}

	public override void OnDeselected()
	{
		if (PrimaryBot != null)
		{
			PrimaryBot.OnDeselected();
		}
		if (SecondaryBot != null)
		{
			SecondaryBot.OnDeselected();
		}
	}
}
