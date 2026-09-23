using System;
using System.Collections.Generic;
using Styx;
using Styx.WoWInternals;

namespace Bots.DungeonBuddy.Helpers;

public class SpellActionButton
{
	private int _cooldownDurationMs;

	private long _cooldownStartMs;

	public int Slot { get; private set; }

	public bool CanUse
	{
		get
		{
			if (IsEnabled)
			{
				return !OnCooldown;
			}
			return false;
		}
	}

	public bool IsEnabled { get; private set; }

	public bool IsValid { get; private set; }

	public TimeSpan CoolDownLeft
	{
		get
		{
			long num = Environment.TickCount - _cooldownStartMs;
			if (IsValid && _cooldownStartMs != 0L && num < _cooldownDurationMs)
			{
				return TimeSpan.FromMilliseconds(_cooldownDurationMs - num);
			}
			return TimeSpan.Zero;
		}
	}

	public bool OnCooldown
	{
		get
		{
			if (CoolDownLeft != TimeSpan.Zero)
			{
				return Charges == 0;
			}
			return false;
		}
	}

	public WoWSpell Spell => WoWSpell.FromId(SpellId);

	public int SpellId { get; private set; }

	public int Charges { get; private set; }

	public int MaxCharges { get; private set; }

	public static SpellActionButton ExtraActionButton => new SpellActionButton(169);

	public static List<SpellActionButton> PetActionButtons
	{
		get
		{
			using (StyxWoW.Memory.AcquireFrame())
			{
				List<SpellActionButton> list = new List<SpellActionButton>(10);
				for (int i = 0; i < 12; i++)
				{
					SpellActionButton spellActionButton = new SpellActionButton(i + 182);
					if (spellActionButton.IsValid)
					{
						list.Add(spellActionButton);
					}
				}
				return list;
			}
		}
	}

	public static List<SpellActionButton> VehicleActionButtons
	{
		get
		{
			using (StyxWoW.Memory.AcquireFrame())
			{
				List<SpellActionButton> list = new List<SpellActionButton>(10);
				for (int i = 0; i < 12; i++)
				{
					SpellActionButton spellActionButton = new SpellActionButton(i + 133);
					if (spellActionButton.IsValid)
					{
						list.Add(spellActionButton);
					}
				}
				return list;
			}
		}
	}

	public static List<SpellActionButton> OverrideActionBarButtons
	{
		get
		{
			using (StyxWoW.Memory.AcquireFrame())
			{
				List<SpellActionButton> list = new List<SpellActionButton>(6);
				for (int i = 0; i < 6; i++)
				{
					SpellActionButton spellActionButton = new SpellActionButton(i + 157);
					if (spellActionButton.IsValid)
					{
						list.Add(spellActionButton);
					}
				}
				return list;
			}
		}
	}

	public SpellActionButton(int slot)
	{
		Slot = slot;
		Update();
	}

	public void Update()
	{
		using (StyxWoW.Memory.AcquireFrame())
		{
			SpellId = Lua.GetReturnVal<int>($"return GetActionInfo({Slot})", 1u);
			IsValid = SpellId != 0 && StyxWoW.Me.KnowsSpell(SpellId);
			if (!IsValid)
			{
				IsEnabled = false;
				SpellId = 0;
				_cooldownDurationMs = 0;
				_cooldownStartMs = 0L;
				return;
			}
			string lua = $"isUsable, notEnoughMana = IsUsableAction({Slot}) if isUsable and notEnoughMana == nil then return 1 end return nil";
			IsEnabled = Lua.GetReturnVal<bool>(lua, 0u);
			List<string> returnValues = Lua.GetReturnValues($"return GetActionCooldown({Slot})");
			SpellId = Lua.GetReturnVal<int>($"return GetActionInfo({Slot})", 1u);
			_cooldownStartMs = (long)(float.Parse(returnValues[0]) * 1000f);
			if (int.TryParse(returnValues[1], out int cooldownDuration))
			{
				_cooldownDurationMs = cooldownDuration * 1000;
			}
			else
			{
				_cooldownDurationMs = 0;
			}
			Charges = int.Parse(returnValues[3]);
			MaxCharges = int.Parse(returnValues[4]);
		}
	}

	public void Use()
	{
		Lua.DoString("UseAction({0})", Slot);
	}
}
