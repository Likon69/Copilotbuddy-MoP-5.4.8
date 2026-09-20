using System;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Styx.CommonBot.Routines
{
    public abstract class CombatRoutine : IDisposable, IBehaviors, ICombatRoutine
    {
        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            ShutDown();
        }

        #endregion

        #region Implementation of ICombatRoutine

        public abstract string Name { get; }
        public abstract WoWClass Class { get; }
        public virtual double? PullDistance { get { return 45.0; } }

        public virtual bool NeedRest { get { return false; } }
        public virtual void Rest() { }

        public virtual bool NeedPreCombatBuffs { get { return false; } }
        public virtual void PreCombatBuff() { }

        public virtual bool NeedPullBuffs { get { return false; } }
        public virtual void PullBuff() { }
        public virtual void Pull() { }

        public virtual bool NeedCombatBuffs { get { return false; } }
        public virtual void CombatBuff() { }
        public virtual void Combat() { }

        public virtual bool NeedHeal { get { return false; } }
        public virtual void Heal() { }

        public virtual bool NeedDeath { get { return false; } }
        public virtual void Death() { }

        /// <summary>
        /// Called when this CC is selected as the current CC.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when the button for this CC is pressed.
        /// </summary>
        public virtual void OnButtonPress() { }

        /// <summary>
        /// Whether this CC want the button on the form to be enabled.
        /// </summary>
        public virtual bool WantButton { get { return false; } }

        /// <summary>
        /// The text to appear on the button in the form. [Default: "CC Configuration"]
        /// </summary>
        public string ButtonText { get { return "CC Configuration"; } }

        /// <summary>
        /// Called in every pulse of the bot. This way you can maintain stuff per-pulse like a plugin.
        /// </summary>
        public virtual void Pulse() { }

        #endregion

        #region Implementation of IBehaviors

        /// <summary>
        /// Should be wrapped in a DecoratorContinue. This behavior is used within a sequence.
        /// </summary>
        public virtual Composite DeathBehavior
        {
            get
            {
                return new DecoratorContinue(ret => NeedDeath,
                    new Action(ret => Death()));
            }
        }

        /// <summary>Behavior used when resting.</summary>
        public virtual Composite RestBehavior
        {
            get
            {
                return new Decorator(cnd => NeedRest,
                    new Action(action => Rest()));
            }
        }

        /// <summary>Behavior used for buffing, regular buffs like 'Power Word: Fortitude', 'MotW' etc..</summary>
        public virtual Composite PreCombatBuffBehavior
        {
            get
            {
                return new Decorator(cnd => NeedPreCombatBuffs,
                    new Action(action => PreCombatBuff()));
            }
        }

        /// <summary>Behavior used when buffing prior to pulling.</summary>
        public virtual Composite PullBuffBehavior
        {
            get
            {
                return new Decorator(cnd => NeedPullBuffs,
                    new Action(action => PullBuff()));
            }
        }

        /// <summary>Behavior used when engaging mobs in combat.</summary>
        public virtual Composite PullBehavior
        {
            get
            {
                return
                    new Action(delegate (object context)
                    {
                        LocalPlayer me = StyxWoW.Me;

                        if (me.CurrentTarget == null || (StyxWoW.Me.IsFlying && StyxWoW.Me.IsMoving))
                            return RunStatus.Failure;

                        if (me.Mounted)
                        {
                            Mount.Dismount("Pulling");
                            return RunStatus.Running;
                        }

                        if (StyxWoW.Me.Combat)
                        {
                            return RunStatus.Success;
                        }

                        WoWUnit currentTarget = me.CurrentTarget;

                        if (me.CurrentTarget.IsDead)
                        {
                            Blacklist.Add(me.CurrentTarget.CurrentTargetGuid, BlacklistFlags.Pull, TimeSpan.FromMinutes(5.0), "Target is dead!");
                            me.ClearTarget();
                            return RunStatus.Failure;
                        }

                        if (currentTarget.TaggedByOther && !currentTarget.TaggedByMe && !StyxWoW.Me.GroupInfo.IsInParty && !StyxWoW.Me.GroupInfo.IsInRaid)
                        {
                            Blacklist.Add(me.CurrentTargetGuid, BlacklistFlags.Pull, TimeSpan.FromMinutes(5.0), "Target is tagged by somebody else!");
                            me.ClearTarget();
                            return RunStatus.Failure;
                        }

                        if (RoutineManager.Current.NeedPullBuffs)
                        {
                            RoutineManager.Current.PullBuff();
                        }

                        RoutineManager.Current.Pull();
                        return RunStatus.Success;
                    });
            }
        }

        /// <summary>Behavior used for combat buffs. eg; 'Horn of Winter', 'Power Infusion' etc..</summary>
        public virtual Composite CombatBuffBehavior
        {
            get
            {
                return new Decorator(cnd => NeedCombatBuffs,
                    new Action(action => CombatBuff()));
            }
        }

        /// <summary>Behavior used in combat.</summary>
        public virtual Composite CombatBehavior
        {
            get { return new Action(action => Combat()); }
        }

        /// <summary>Behavior used when healing.</summary>
        public virtual Composite HealBehavior
        {
            get
            {
                return new Decorator(cnd => NeedHeal,
                    new Action(action => Heal()));
            }
        }

        /// <summary>Behavior used for moving to targets.</summary>
        public virtual Composite MoveToTargetBehavior { get { return null; } }

        #endregion

        public virtual CapabilityFlags SupportedCapabilities { get { return CapabilityFlags.None; } }

        public bool HasCapability(CapabilityFlags capability)
        {
            return capability != CapabilityFlags.None && (SupportedCapabilities & capability) == capability;
        }

        protected CapabilityState GetCapabilityState(CapabilityFlags capability)
        {
            return RoutineManager.GetCapabilityState(capability);
        }

        protected void SetCapabilityState(CapabilityFlags capability, CapabilityState state)
        {
            RoutineManager.SetCapabilityState(capability, state);
        }

        /// <summary>
        /// Called when this routine is disposed.
        /// </summary>
        public virtual void ShutDown()
        {
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", Class, Name);
        }
    }
}
