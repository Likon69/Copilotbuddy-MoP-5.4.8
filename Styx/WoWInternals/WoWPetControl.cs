using System;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx.Common;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public class WoWPetControl
    {
        private static PerFrameCachedValue<WoWPetSpell> _petActionAttack;

        private static PerFrameCachedValue<WoWPetSpell> _petActionFollow;

        private static PerFrameCachedValue<WoWPetSpell> _petStanceAssist;

        private static PerFrameCachedValue<WoWPetSpell> _petStanceDefensive;

        private static PerFrameCachedValue<WoWPetSpell> _petStancePassive;

        public static bool CanPetBeDismissed
        {
            get
            {
                if (StyxWoW.Me.GotAlivePet)
                {
                    return Lua.GetReturnVal<bool>("return PetCanBeDismissed()", 0u);
                }

                return false;
            }
        }

        private static WoWPetSpell PetActionAttack => _petActionAttack ?? (_petActionAttack = new PerFrameCachedValue<WoWPetSpell>(() => FindActionByName("Attack")));

        private static WoWPetSpell PetActionFollow => _petActionFollow ?? (_petActionFollow = new PerFrameCachedValue<WoWPetSpell>(() => FindActionByName("Follow")));

        private static WoWPetSpell PetStanceAssist => _petStanceAssist ?? (_petStanceAssist = new PerFrameCachedValue<WoWPetSpell>(() => FindActionByName("Assist")));

        private static WoWPetSpell PetStanceDefensive => _petStanceDefensive ?? (_petStanceDefensive = new PerFrameCachedValue<WoWPetSpell>(() => FindActionByName("Defensive")));

        private static WoWPetSpell PetStancePassive => _petStancePassive ?? (_petStancePassive = new PerFrameCachedValue<WoWPetSpell>(() => FindActionByName("Passive")));

        public static bool CanCastAction(WoWPetSpell petAction)
        {
            if (petAction == null)
            {
                return false;
            }

            if (petAction.SpellType == WoWPetSpell.PetSpellType.Spell && petAction.Spell == null)
            {
                return false;
            }

            if (StyxWoW.Me.Mounted)
            {
                return false;
            }

            if (petAction.SpellType == WoWPetSpell.PetSpellType.Spell)
            {
                return !petAction.Spell.Cooldown;
            }

            return true;
        }

        public static void CastAction(WoWPetSpell action, WoWUnit wowUnit = null)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            if (wowUnit != null && !wowUnit.IsValid)
            {
                throw new ArgumentException("The unit passed in is invalid", "wowUnit");
            }

            string actionName = GetActionName(action);
            if (wowUnit == null)
            {
                Logging.WriteDiagnostic("Instructing pet to '{0}'.", actionName);
                Lua.DoString("CastPetAction({0})", action.ActionBarIndex + 1);
                return;
            }

            Logging.WriteDiagnostic("Instructing pet to '{0}' on '{1}'.", actionName, wowUnit.SafeName);
            ulong focusedUnitGuid = StyxWoW.Me.FocusedUnitGuid;
            using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
            {
                StyxWoW.Me.FocusInternal(wowUnit.Guid);
                Lua.DoString("CastPetAction({0}, 'focus')", action.ActionBarIndex + 1);
                StyxWoW.Me.FocusInternal(focusedUnitGuid);
            }
        }

        public static WoWPetSpell FindActionByName(string actionName)
        {
            return StyxWoW.Me.PetSpells.FirstOrDefault(petSpell => petSpell.ToString() == actionName);
        }

        public static string GetActionName(WoWPetSpell action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            return action.SpellType switch
            {
                WoWPetSpell.PetSpellType.Spell => action.Spell.Name,
                WoWPetSpell.PetSpellType.Action => action.Action.ToString(),
                WoWPetSpell.PetSpellType.Stance => action.Stance.ToString(),
                _ => "Unknown",
            };
        }

        public static bool IsActionActive(WoWPetSpell petAction)
        {
            if (petAction == null)
            {
                return false;
            }

            return Lua.GetReturnVal<bool>($"return GetPetActionInfo({petAction.ActionBarIndex + 1})", 4u);
        }

        public static async Task<bool> DismissPet()
        {
            if (!CanPetBeDismissed)
            {
                return false;
            }

            if (StyxWoW.Me.Class == WoWClass.Hunter)
            {
                if (StyxWoW.Me.IsMoving)
                {
                    WoWMovement.MoveStop();
                    await Coroutine.Wait(2000, () => !StyxWoW.Me.IsMoving);
                }

                SpellManager.Cast("Dismiss Pet");
                await Coroutine.Wait(3500, () => !CanPetBeDismissed);
            }
            else
            {
                Lua.DoString("PetDismiss(); DestroyTotem(1); DestroyTotem(2); DestroyTotem(3); DestroyTotem(4);");
            }

            return true;
        }

        public static void Attack(WoWUnit wowUnit)
        {
            if (wowUnit != null && wowUnit.IsValid && StyxWoW.Me.GotAlivePet && StyxWoW.Me.Pet.CurrentTarget != wowUnit)
            {
                CastAction(PetActionAttack, wowUnit);
            }
        }

        public static void Follow()
        {
            if (StyxWoW.Me.GotAlivePet)
            {
                WoWPetSpell petActionFollow = PetActionFollow;
                if ((!IsActionActive(petActionFollow) || StyxWoW.Me.Pet.GotTarget) && CanCastAction(petActionFollow))
                {
                    CastAction(petActionFollow);
                }
            }
        }

        public static void SetStance(PetStance stance)
        {
            if (StyxWoW.Me.GotAlivePet)
            {
                switch (stance)
                {
                    default:
                        throw new ArgumentOutOfRangeException("stance");
                    case PetStance.Assist:
                        ActivateStance(PetStanceAssist);
                        break;
                    case PetStance.Defensive:
                        ActivateStance(PetStanceDefensive);
                        break;
                    case PetStance.Passive:
                        ActivateStance(PetStancePassive);
                        break;
                }
            }
        }

        private static void ActivateStance(WoWPetSpell petStance)
        {
            if (!IsActionActive(petStance) && CanCastAction(petStance))
            {
                CastAction(petStance);
            }
        }
    }
}
