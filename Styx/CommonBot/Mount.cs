using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using CommonBehaviors.Actions;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.Resources;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Styx.CommonBot
{
    public static class Mount
    {
        #region Fields

        internal static readonly WaitTimer MountTimer = WaitTimer.FiveSeconds;

        private static readonly WaitTimer MountKillTimer = WaitTimer.FiveSeconds;

        private static readonly Random Rand = new Random();

        private static readonly RangedDictionary<WoWPoint> CantMountLocations = new RangedDictionary<WoWPoint>();

        private static readonly WaitTimer RefreshTimer = new WaitTimer(TimeSpan.FromMinutes(5.0));

        private static List<MountWrapper> _cachedMounts;

        private static ulong _lastGuid;

        private const int ApprenticeRiding = 33388;

        #endregion

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        #region Events

        /// <summary>
        /// Fired when the player starts to mount.
        /// </summary>
        public static event EventHandler<MountUpEventArgs> OnMountUp;

        /// <summary>
        /// Fired when the player has finished dismounting.
        /// </summary>
        public static event EventHandler<EventArgs> OnDismount;

        #endregion

        static Mount()
        {
            BotEvents.Player.OnMobKilled += Player_OnMobKilled;
            BotEvents.Player.OnMapChanged += Player_OnMapChanged;
        }

        private static void Player_OnMapChanged(BotEvents.Player.MapChangedEventArgs args)
        {
            _cachedMounts = null;
        }

        private static void Player_OnMobKilled(BotEvents.Player.MobKilledEventArgs args)
        {
            MountKillTimer.Reset();
        }

        public static bool UseMount
        {
            get
            {
                GrindArea grindArea = StyxWoW.AreaManager != null ? StyxWoW.AreaManager.CurrentGrindArea : null;
                if (grindArea != null && grindArea.UseMount != null)
                {
                    return grindArea.UseMount.Value;
                }

                Profile currentProfile = ProfileManager.CurrentProfile;
                return currentProfile == null || currentProfile.UseMount == null || currentProfile.UseMount.Value;
            }
        }

        /// <summary>
        /// Attempts to dismount the player.
        /// </summary>
        [Obsolete("Use ActionLandAndDismount or CommonCoroutines.LandAndDismount instead")]
        public static void Dismount()
        {
            Dismount(string.Empty);
        }

        /// <summary>
        /// Attempts to dismount the player.
        /// </summary>
        /// <param name="reason">reason why we are dismounting. </param>
        [Obsolete("Use ActionLandAndDismount or CommonCoroutines.LandAndDismount instead")]
        public static void Dismount(string reason)
        {
            ShapeshiftForm shapeshift = Me.Shapeshift;
            if (!StyxWoW.Me.Mounted && shapeshift != ShapeshiftForm.FlightForm && shapeshift != ShapeshiftForm.EpicFlightForm)
            {
                return;
            }

            Logging.WriteDiagnostic(StyxResources.Stop_and_dismount___ + (!string.IsNullOrEmpty(reason) ? " Reason: " + reason : string.Empty));

            WoWMovement.MoveStop();
            StyxWoW.SleepForLagDuration();

            if (StyxWoW.Me.IsFlying)
            {
                WoWMovement.Move(WoWMovement.MovementDirection.Descend);
                using (StyxWoW.Memory.ReleaseFrame(true))
                {
                    while (StyxWoW.Me.IsFlying)
                    {
                        StyxWoW.Sleep(100);
                    }
                }

                WoWMovement.MoveStop(WoWMovement.MovementDirection.Descend);
            }

            if (shapeshift == ShapeshiftForm.FlightForm || shapeshift == ShapeshiftForm.EpicFlightForm)
            {
                Lua.DoString("RunMacroText('/cancelform')", "WoW.lua");
            }
            else
            {
                Lua.DoString("Dismount()", "WoW.lua");
            }

            RaiseOnDismount(reason);
        }

        internal static void RaiseOnDismount(string reason)
        {
            if (reason == null)
            {
                reason = string.Empty;
            }

            EventHandler<EventArgs> onDismount = OnDismount;
            if (onDismount != null)
            {
                foreach (Delegate d in onDismount.GetInvocationList())
                {
                    try
                    {
                        d.DynamicInvoke(reason, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                    }
                }
            }
        }

        public static void MountUp(LocationRetriever travelingTo)
        {
            MountUp(() => true, travelingTo);
        }

        internal static void SetMountInfo()
        {
            if (CharacterSettings.Instance.UseRandomMount)
            {
                List<MountWrapper> list = GroundMounts;
                if (list != null && list.Count > 0)
                {
                    MountWrapper mountWrapper = list[Rand.Next(0, list.Count)];
                    CharacterSettings.Instance.MountName = mountWrapper.Name;
                }

                list = FlyingMounts;
                if (list != null && list.Count > 0)
                {
                    MountWrapper mountWrapper2 = list[Rand.Next(0, list.Count)];
                    CharacterSettings.Instance.FlyingMountName = mountWrapper2.Name;
                }
            }

            if (string.IsNullOrEmpty(CharacterSettings.Instance.MountName) || CharacterSettings.Instance.MountName.Contains("Automatically detected"))
            {
                List<MountWrapper> groundMounts = GroundMounts;
                if (groundMounts != null && groundMounts.Count > 0)
                {
                    MountWrapper mountWrapper3 = groundMounts[Rand.Next(0, groundMounts.Count)];
                    CharacterSettings.Instance.MountName = mountWrapper3.Name;
                }
            }

            if (string.IsNullOrEmpty(CharacterSettings.Instance.FlyingMountName) || CharacterSettings.Instance.FlyingMountName.Contains("Automatically detected"))
            {
                if (StyxWoW.Me.Class == WoWClass.Druid)
                {
                    if (SpellManager.HasSpell("Swift Flight Form"))
                    {
                        CharacterSettings.Instance.FlyingMountName = "Swift Flight Form";
                        return;
                    }

                    if (SpellManager.HasSpell("Flight Form"))
                    {
                        CharacterSettings.Instance.FlyingMountName = "Flight Form";
                        return;
                    }
                }

                List<MountWrapper> flyingMounts = FlyingMounts;
                if (flyingMounts != null && flyingMounts.Count > 0)
                {
                    MountWrapper mountWrapper4 = flyingMounts[Rand.Next(0, flyingMounts.Count)];
                    CharacterSettings.Instance.FlyingMountName = mountWrapper4.Name;
                }
            }
        }

        public static void SummonMount(string mountName)
        {
            Lua.DoString(string.Format("for companion=1, GetNumCompanions(\"MOUNT\") do local _, name = GetCompanionInfo(\"MOUNT\", companion) name = string.lower(name) if name == \"{0}\" then CallCompanion(\"MOUNT\", companion) break end end", mountName), "WoW.lua");
            MountTimer.Reset();
        }

        public static void SummonMount(int mountSpellId)
        {
            Lua.DoString(string.Format("for companion=1, GetNumCompanions(\"MOUNT\") do local _, _, id = GetCompanionInfo(\"MOUNT\", companion) id = tostring(id) if id == \"{0}\" then CallCompanion(\"MOUNT\", companion) break end end", mountSpellId), "WoW.lua");
            MountTimer.Reset();
        }

        public static bool MountUp(CanMountDelegate extra, LocationRetriever travelingTo)
        {
            SetMountInfo();
            if (GroundMounts == null || GroundMounts.Count == 0)
            {
                return false;
            }

            if (!StyxWoW.Me.InVehicle && UseMount && CanMount() && extra() && !string.IsNullOrEmpty(CharacterSettings.Instance.MountName) && !ShouldAbortMountUp(travelingTo))
            {
                WoWMovement.MoveStop();
                if (StyxWoW.Me.Class == WoWClass.Druid && StyxWoW.Me.Shapeshift != ShapeshiftForm.Normal)
                {
                    Lua.DoString("CancelShapeshiftForm()", "WoW.lua");
                }

                StyxWoW.SleepForLagDuration();
                StyxWoW.Sleep(200);
                OurMountUp();
                MountTimer.Reset();
                return true;
            }

            return false;
        }

        private static bool ShouldAbortMountUp(LocationRetriever travelingTo)
        {
            EventHandler<MountUpEventArgs> onMountUp = OnMountUp;
            if (onMountUp == null)
            {
                return false;
            }

            var mountUpEventArgs = new MountUpEventArgs { Destination = travelingTo() };
            foreach (EventHandler<MountUpEventArgs> eventHandler in onMountUp.GetInvocationList())
            {
                try
                {
                    eventHandler(null, mountUpEventArgs);
                }
                catch
                {
                    continue;
                }

                if (mountUpEventArgs.Cancel)
                {
                    return true;
                }
            }

            return false;
        }

        private static void OurMountUp()
        {
            string mountname = CharacterSettings.Instance.MountName.ToLowerInvariant();
            WoWRace race = StyxWoW.Me.Race;
            WoWClass @class = StyxWoW.Me.Class;

            if (race == WoWRace.BloodElf && @class == WoWClass.Paladin)
            {
                if (mountname.Contains("warhorse") || mountname.Contains("призыв боевого коня"))
                {
                    mountname = "34769";
                }
                else if (mountname.Contains("charger") || mountname.Contains("призыв скакуна"))
                {
                    mountname = "34767";
                }
            }

            if (race == WoWRace.Worgen && (mountname == "running wild" || mountname == "87840") && SpellManager.HasSpell(87840))
            {
                SpellManager.Cast(87840);
            }
            else if (mountname.All(char.IsDigit))
            {
                int spellId;
                if (int.TryParse(mountname, out spellId))
                {
                    if (GroundMounts.All(m => m.CreatureSpellId != spellId))
                    {
                        MountWrapper mountWrapper = GroundMounts.FirstOrDefault();
                        if (mountWrapper == null)
                        {
                            return;
                        }

                        Logging.Write(Colors.Red, "Mount Name must be set to a ground mount. Changing it to {0}", mountWrapper.Name);
                        CharacterSettings.Instance.MountName = mountWrapper.Name;
                        spellId = mountWrapper.CreatureSpellId;
                    }

                    MountWrapper mount = GroundMounts.First(m => m.CreatureSpellId == spellId);
                    Logging.Write(StyxResources.Mounting___0_, mount.Name);
                    SummonMount(spellId);
                }
            }
            else
            {
                if (GroundMounts.All(m => m.Name.ToLowerInvariant() != mountname))
                {
                    MountWrapper mountWrapper = GroundMounts.FirstOrDefault();
                    if (mountWrapper == null)
                    {
                        return;
                    }

                    Logging.Write(Colors.Red, "Mount Name must be set to a ground mount. Changing it to {0}", mountWrapper.Name);
                    CharacterSettings.Instance.MountName = mountWrapper.Name;
                }

                Logging.Write(StyxResources.Mounting___0_, CharacterSettings.Instance.MountName);
                SummonMount(CharacterSettings.Instance.MountName.ToLowerInvariant());
            }

            int tickCount = Environment.TickCount;
            string lastRedErrorMessage = StyxWoW.LastRedErrorMessage;
            using (StyxWoW.Memory.ReleaseFrame(true))
            {
                while (!StyxWoW.Me.Mounted && Environment.TickCount - tickCount < 6500)
                {
                    ObjectManager.Update();

                    if (StyxWoW.Me.Combat)
                    {
                        break;
                    }

                    if (lastRedErrorMessage != StyxResources.You_can_t_mount_here_ && StyxWoW.LastRedErrorMessage == StyxResources.You_can_t_mount_here_)
                    {
                        AddCantMountSpot(StyxWoW.Me.Location);
                        break;
                    }

                    StyxWoW.Sleep(250);
                }
            }

            Navigator.NavigationProvider.StuckHandler.Reset();
        }

        internal static bool IsNearCantMountSpot(WoWPoint location)
        {
            return location.GetPointsAtDistanceOf(10f, CantMountLocations).Count > 0;
        }

        public static bool CanMount()
        {
            if (!MountKillTimer.IsFinished)
            {
                return false;
            }

            if (!MountTimer.IsFinished)
            {
                return false;
            }

            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null || !activeMover.IsMe)
            {
                return false;
            }

            if (StyxWoW.Me.IsDead || StyxWoW.Me.IsGhost)
            {
                return false;
            }

            if (!UseMount)
            {
                return false;
            }

            if (WoWPetBattle.IsInBattle)
            {
                return false;
            }

            if (!StyxWoW.Me.CurrentMap.IsMountAllowed)
            {
                return false;
            }

            if (StyxWoW.Me.Combat)
            {
                return false;
            }

            int mountId;
            if (((int.TryParse(CharacterSettings.Instance.MountName, out mountId) && mountId == 93326) || CharacterSettings.Instance.MountName == "Sandstone Drake") && StyxWoW.Me.GetSkill(SkillLine.Riding).CurrentValue > 75)
            {
                return false;
            }

            WoWPoint location = StyxWoW.Me.Location;
            if (IsNearCantMountSpot(location))
            {
                return false;
            }

            if (!StyxWoW.Me.IsOutdoors)
            {
                AddCantMountSpot(location);
                return false;
            }

            if (StyxWoW.Me.Level > 20)
            {
                return true;
            }

            bool flag = Mounts.Count != 0;
            if (StyxWoW.Me.Race == WoWRace.Worgen && SpellManager.HasSpell(87840))
            {
                flag = true;
            }

            if (!flag)
            {
                return false;
            }

            float boundingHeight = Me.BoundingHeight;
            WoWPoint woWPoint = Me.Location.Add(0f, 0f, boundingHeight);
            WoWPoint woWPoint2 = woWPoint.Add(0f, 0f, boundingHeight);
            if (!GameWorld.IsInLineOfSight(woWPoint, woWPoint2))
            {
                AddCantMountSpot(location);
                return false;
            }

            return false;
        }

        /// <summary>
        /// We store the area where we can't mount
        /// </summary>
        /// <param name="loc">wowpoint</param>
        public static void AddCantMountSpot(WoWPoint loc)
        {
            CantMountLocations.Add(loc);
        }

        public static void StateMount(LocationRetriever travelingTo)
        {
            if (UseMount && !StyxWoW.Me.Mounted)
            {
                if (CanMount())
                {
                    MountUp(travelingTo);
                }

                return;
            }
        }

        public static bool ShouldMount(WoWPoint travelingTo)
        {
            if (!CanMount())
            {
                return false;
            }

            double mountDistance = BotPoi.Current.Type == PoiType.Kill ? MountDistance + Targeting.PullDistance : (double)MountDistance;
            WoWPoint location = StyxWoW.Me.Location;
            if (location.DistanceSqr(travelingTo) > mountDistance * mountDistance)
            {
                return true;
            }

            float? pathDistance = Navigator.PathDistance(location, travelingTo, (float)mountDistance);
            return pathDistance != null && pathDistance.Value >= mountDistance;
        }

        public static float MountDistance
        {
            get { return (float)CharacterSettings.Instance.MountDistance; }
        }

        public static bool ShouldDismount(WoWPoint travelingTo)
        {
            if (!StyxWoW.Me.Mounted)
            {
                return false;
            }

            if (StyxWoW.Me.Combat && !StyxWoW.Me.IsMoving && Targeting.Instance.FirstUnit != null)
            {
                Logging.WriteDiagnostic("Dismount for attacker.");
                return true;
            }

            if (travelingTo == WoWPoint.Empty)
            {
                return false;
            }

            float distSqr = StyxWoW.Me.Location.DistanceSqr(travelingTo);
            if (BotPoi.Current.Type != PoiType.Kill || distSqr > Targeting.PullDistanceSqr)
            {
                return false;
            }

            if (Battlegrounds.IsInsideBattleground && Targeting.Instance.FirstUnit is WoWPlayer)
            {
                Logging.WriteDiagnostic("Dismount for PVP in {0}", StyxWoW.Me.ZoneText);
                return true;
            }

            Logging.WriteDiagnostic("Dismount to kill bot poi.");
            return true;
        }

        /// <summary>Gets the number of mounts.</summary>
        /// <value>The total number of mounts.</value>
        public static int NumMounts
        {
            get { return Lua.GetReturnVal<int>("return GetNumCompanions('MOUNT')", 0U); }
        }

        public static MountWrapper Current
        {
            get
            {
                if (!StyxWoW.Me.Mounted)
                {
                    return null;
                }

                return Me.PassiveAuras.Values
                    .Where(aura => aura.Spell.SpellEffects.Any(effect => effect.EffectType == WoWSpellEffectType.ApplyAura && effect.AuraType == WoWApplyAuraType.Mounted))
                    .Select(aura => MountWrapper.FromSpellId(aura.SpellId))
                    .FirstOrDefault(mountWrapper => mountWrapper != null);
            }
        }

        /// <summary>Gets the mounts the current player has.</summary>
        /// <value>The mounts.</value>
        public static List<MountWrapper> Mounts
        {
            get
            {
                if (RefreshTimer.IsFinished || _lastGuid != StyxWoW.Me.Guid || _cachedMounts == null)
                {
                    RefreshTimer.Reset();
                    _lastGuid = StyxWoW.Me.Guid;
                    _cachedMounts = BuildMountList();
                }

                return _cachedMounts;
            }
        }

        private static List<MountWrapper> BuildMountList()
        {
            List<MountWrapper> result;
            using (StyxWoW.Memory.AcquireFrame())
            {
                var list = new List<MountWrapper>();
                int numMounts = NumMounts;
                bool hasCloudSerpentRiding = SpellManager.HasSpell("Cloud Serpent Riding");
                bool isBattleground = StyxWoW.Me.CurrentMap.IsBattleground;

                for (int i = 1; i <= numMounts; i++)
                {
                    try
                    {
                        MountWrapper mountWrapper;
                        if (TryGetMountWrapper(i, isBattleground, hasCloudSerpentRiding, out mountWrapper))
                        {
                            list.Add(mountWrapper);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Write(StyxResources.Error_getting_mount_info_for_mount_slot__0___Exception___1_, i, ex);
                    }
                }

                result = list;
            }

            return result;
        }

        private static bool TryGetMountWrapper(int slot, bool isBattleground, bool hasCloudSerpentRiding, out MountWrapper mountWrapper)
        {
            mountWrapper = new MountWrapper(slot);
            int creatureSpellId = mountWrapper.CreatureSpellId;

            switch (creatureSpellId)
            {
                case 44151:
                    if (StyxWoW.Me.GetSkill(SkillLine.Engineering).CurrentValue < 375)
                    {
                        goto case -1;
                    }
                    break;

                case 44153:
                    if (StyxWoW.Me.GetSkill(SkillLine.Engineering).CurrentValue < 300)
                    {
                        goto case -1;
                    }
                    break;

                case 61309:
                    if (StyxWoW.Me.GetSkill(SkillLine.Tailoring).CurrentValue < 425)
                    {
                        goto case -1;
                    }
                    break;

                case 61451:
                    if (StyxWoW.Me.GetSkill(SkillLine.Tailoring).CurrentValue < 300)
                    {
                        goto case -1;
                    }
                    break;

                case 75596:
                    if (StyxWoW.Me.GetSkill(SkillLine.Tailoring).CurrentValue < 425)
                    {
                        goto case -1;
                    }
                    break;

                case -1:
                    Logging.WriteDiagnostic("[Mount] " + mountWrapper.Name + " is known, but we don't have the skill to use it!");
                    return false;
            }

            return (!mountWrapper.IsHybridMount || !isBattleground) && (!mountWrapper.IsCloudSerpentMount || hasCloudSerpentRiding);
        }

        /// <summary>Gets the ground mounts.</summary>
        /// <value>The ground mounts.</value>
        public static List<MountWrapper> GroundMounts
        {
            get { return Mounts.Where(m => m.IsGroundMount).ToList(); }
        }

        /// <summary>Gets the flying mounts.</summary>
        /// <value>The flying mounts.</value>
        public static List<MountWrapper> FlyingMounts
        {
            get
            {
                IEnumerable<MountWrapper> mounts = Mounts.Where(m => m.IsFlyingMount);
                if (StyxWoW.Me.MapId == 870U && !SpellManager.HasSpell("Wisdom of the Four Winds") && SpellManager.HasSpell("Cloud Serpent Riding"))
                {
                    mounts = mounts.Where(m => m.IsCloudSerpentMount);
                }

                return mounts.ToList();
            }
        }

        /// <summary>Gets the underwater mounts.</summary>
        /// <value>The underwater mounts.</value>
        public static List<MountWrapper> UnderwaterMounts
        {
            get { return Mounts.Where(m => m.IsUnderwaterMount).ToList(); }
        }

        #region Nested type: ActionLandAndDismount

        public class ActionLandAndDismount : PrioritySelector
        {
            public ActionLandAndDismount(string reason = "", bool dismount = true, WoWPoint? landPoint = null)
                : base()
            {
                CreateBehavior(reason, dismount, landPoint).ForEach(new Styx.Common.Extensions.ForEachDelegate<Composite>(AddChild));
            }

            private IEnumerable<Composite> CreateBehavior(string reason = "", bool dismount = true, WoWPoint? landPoint = null)
            {
                yield return new Action(context =>
                {
                    WoWUnit activeMover = WoWMovement.ActiveMover;
                    if (!(Parent is Selector) && (!StyxWoW.Me.Mounted || (!dismount && activeMover != null && !activeMover.IsFlying)))
                    {
                        return RunStatus.Success;
                    }

                    return RunStatus.Failure;
                });

                yield return new ActionRunCoroutine(context => CommonCoroutines.LandAndDismount(reason, dismount, landPoint));
            }
        }

        #endregion

        #region Nested type: MountWrapper

        public sealed class MountWrapper
        {
            private MountWrapper()
            {
            }

            internal MountWrapper(int slot)
            {
                Slot = slot;
                List<string> returnValues = Lua.GetReturnValues("return GetCompanionInfo('MOUNT', " + slot + ")");

                int creature;
                if (!int.TryParse(returnValues[0], out creature))
                {
                    creature = 0;
                }

                CreatureId = creature;
                Name = returnValues[1];

                int creatureSpell;
                if (!int.TryParse(returnValues[2], out creatureSpell))
                {
                    creatureSpell = 0;
                }

                CreatureSpellId = creatureSpell;
                Icon = returnValues[3];
                IsSummoned = returnValues[4] == "1";

                if (CreatureSpellId != 0)
                {
                    CreatureSpell = WoWSpell.FromId(CreatureSpellId);
                    Type = (MountType)CreatureSpell.SpellEffect1.MiscValueB;
                    return;
                }

                CreatureSpell = null;
                Type = MountType.Unknown;
            }

            /// <summary>Gets or sets the type.</summary>
            /// <value>The type.</value>
            public MountType Type { get; private set; }

            /// <summary>Gets or sets the slot.</summary>
            /// <value>The slot.</value>
            public int Slot { get; private set; }

            /// <summary>Gets or sets the identifier of the creature.</summary>
            /// <value>The identifier of the creature.</value>
            public int CreatureId { get; private set; }

            /// <summary>Gets or sets the identifier of the creature spell.</summary>
            /// <value>The identifier of the creature spell.</value>
            public int CreatureSpellId { get; private set; }

            /// <summary>Gets or sets the creature spell.</summary>
            /// <value>The creature spell.</value>
            public WoWSpell CreatureSpell { get; private set; }

            /// <summary>Gets or sets the icon.</summary>
            /// <value>The icon.</value>
            public string Icon { get; private set; }

            /// <summary>Gets or sets a value indicating whether this object is summoned.</summary>
            /// <value>true if this object is summoned, false if not.</value>
            public bool IsSummoned { get; private set; }

            /// <summary>Gets or sets the name.</summary>
            /// <value>The name.</value>
            public string Name { get; private set; }

            /// <summary>Gets a value indicating whether we can mount.</summary>
            /// <value>true if we can mount, false if not.</value>
            public bool CanMount
            {
                get { return CreatureSpell != null && CreatureSpell.CanCast; }
            }

            public bool IsClassMount
            {
                get { return ClassMounts.Contains(CreatureSpellId); }
            }

            public bool IsCloudSerpentMount
            {
                get { return CloudSerpentMounts.Contains(CreatureSpellId); }
            }

            public bool IsFlyingMount
            {
                get
                {
                    return Type == MountType.Flying || Type == MountType.PandaFlyingMount || Type == MountType.TransformFlight || Type == MountType.Scaling || IsCloudSerpentMount || IsHybridMount;
                }
            }

            public bool IsGroundMount
            {
                get { return Type == MountType.Ground || Type == MountType.EpicGroundOnly || IsClassMount; }
            }

            public bool IsHybridMount
            {
                get { return HybridMounts.Contains(CreatureSpellId); }
            }

            public bool IsUnderwaterMount
            {
                get { return Type == MountType.Underwater || Type == MountType.UnderwaterVashjir; }
            }

            private bool IsSameMount(MountWrapper other)
            {
                return CreatureSpellId == other.CreatureSpellId;
            }

            public override bool Equals(object obj)
            {
                return !ReferenceEquals(null, obj) && (ReferenceEquals(this, obj) || (obj is MountWrapper && IsSameMount((MountWrapper)obj)));
            }

            public override int GetHashCode()
            {
                return CreatureSpellId;
            }

            public static bool operator ==(MountWrapper left, MountWrapper right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(MountWrapper left, MountWrapper right)
            {
                return !Equals(left, right);
            }

            public static MountWrapper FromSpellId(int spellId)
            {
                WoWSpell woWSpell = WoWSpell.FromId(spellId);
                if (woWSpell == null || !woWSpell.IsValid)
                {
                    return null;
                }

                SpellEffect spellEffect = woWSpell.SpellEffects.FirstOrDefault(effect => effect.AuraType == WoWApplyAuraType.Mounted && effect.EffectType == WoWSpellEffectType.ApplyAura);
                if (spellEffect == null)
                {
                    return null;
                }

                int miscValueA = spellEffect.MiscValueA;
                if (miscValueA == 0)
                {
                    return null;
                }

                return new MountWrapper
                {
                    Slot = -1,
                    CreatureId = miscValueA,
                    Name = woWSpell.Name,
                    Icon = woWSpell.Icon,
                    CreatureSpell = woWSpell,
                    CreatureSpellId = spellId,
                    IsSummoned = StyxWoW.Me.HasAura(spellId),
                    Type = (MountType)spellEffect.MiscValueB
                };
            }

            private static readonly HashSet<int> CloudSerpentMounts = new HashSet<int>
            {
                127170, 123992, 127156, 123993, 127169, 127161, 127164, 127165, 127158, 113119,
                127154, 129918, 139442, 124408, 132036
            };

            private static readonly HashSet<int> HybridMounts = new HashSet<int>
            {
                113199, 121820, 125893, 44151, 44153, 75973, 48025, 75596, 61309, 61451,
                63796, 71342, 98727, 93326, 59996, 107203, 88331, 97359
            };

            private static readonly HashSet<int> ClassMounts = new HashSet<int>
            {
                5784, 23161, 87840, 13819, 23214, 73629, 73630, 69820, 69826, 34769,
                34767
            };
        }

        #endregion
    }
}
