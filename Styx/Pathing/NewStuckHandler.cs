using System;
using System.Diagnostics;
using System.Windows.Media;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

namespace Styx.Pathing
{
    internal class NewStuckHandler : StuckHandler
    {
        #region Constructor

        public NewStuckHandler(NavigationProvider provider)
        {
            NavProvider = provider;
        }

        #endregion

        #region Properties & Fields

        public NavigationProvider NavProvider { get; private set; }

        private WoWPoint LastLocation { get; set; }

        private bool TriedDismount { get; set; }
        private bool TriedJump { get; set; }
        private bool TriedStrafeForwardLeft { get; set; }
        private bool TriedStrafeForwardRight { get; set; }
        private bool TriedStrafeLeft { get; set; }
        private bool TriedStrafeRight { get; set; }

        private readonly WaitTimer _mountUpWaitTimer = new WaitTimer(TimeSpan.FromSeconds(10.0));

        private long _callCount = 1L;

        private static WoWPoint _lastStuckLocation = WoWPoint.Empty;

        private readonly Stopwatch _stuckStopwatch = new Stopwatch();

        #endregion

        #region Private Methods

        private void ResetVariables()
        {
            TriedDismount = false;
            TriedJump = false;
            TriedStrafeLeft = false;
            TriedStrafeRight = false;
            TriedStrafeForwardLeft = false;
            TriedStrafeForwardRight = false;
        }

        #endregion

        #region Event Handlers

        private void Mount_OnMountUp(object sender, MountUpEventArgs e)
        {
            e.Cancel = !_mountUpWaitTimer.IsFinished;
        }

        private void WoWMovement_OnMovementFlagsChanged(WoWMovement.MovementEventArgs movementEventArgs)
        {
            if (movementEventArgs.Stop)
            {
                _stuckStopwatch.Restart();
                _lastStuckLocation = WoWPoint.Empty;
            }
        }

        #endregion

        #region StuckHandler Members

        public override void OnSetAsCurrent()
        {
            base.OnSetAsCurrent();
            Mount.OnMountUp += Mount_OnMountUp;
            WoWMovement.OnMovementFlagsChanged += WoWMovement_OnMovementFlagsChanged;
        }

        public override void OnRemoveAsCurrent()
        {
            WoWMovement.OnMovementFlagsChanged -= WoWMovement_OnMovementFlagsChanged;
            Mount.OnMountUp -= Mount_OnMountUp;
            base.OnRemoveAsCurrent();
        }

        public override bool IsStuck()
        {
            if (StyxWoW.Me.Stunned || StyxWoW.Me.Fleeing || StyxWoW.Me.Dazed || StyxWoW.Me.Rooted)
            {
                Reset();
                return false;
            }

            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null || !activeMover.IsMe)
            {
                return false;
            }

            if (_stuckStopwatch.ElapsedMilliseconds < 500L)
            {
                return false;
            }

            WoWPoint location = StyxWoW.Me.Location;
            if (_lastStuckLocation != WoWPoint.Empty)
            {
                double maxDistance = (activeMover.MovementInfo.IsSwimming ? activeMover.MovementInfo.SwimmingForwardSpeed : activeMover.MovementInfo.ForwardSpeed) * _stuckStopwatch.Elapsed.TotalSeconds * 0.6;
                float? pathDistance = Navigator.PathDistance(_lastStuckLocation, location, (float)maxDistance);
                if (pathDistance != null && pathDistance.GetValueOrDefault() < maxDistance)
                {
                    Logging.WriteVerbose(Colors.Red, "We are stuck! ({0})", location);
                    _lastStuckLocation = location;
                    return true;
                }
            }

            _stuckStopwatch.Restart();
            _lastStuckLocation = location;
            return false;
        }

        public override void Unstick()
        {
            _mountUpWaitTimer.Reset();
            float rotation = StyxWoW.Me.Rotation;
            WoWPoint location = StyxWoW.Me.Location;

            if (LastLocation.DistanceSqr(location) >= 100f)
            {
                ResetVariables();
            }
            LastLocation = location;

            int sleepDuration = (_callCount % 2L == 0L) ? 1000 : 600;
            _callCount += 1L;

            WoWPoint checkPoint = location.RayCast(rotation, 3f).Add(0f, 0f, 1f);

            if (!TriedDismount && StyxWoW.Me.Mounted)
            {
                if (!GameWorld.IsInLineOfSight(location.Add(0f, 0f, StyxWoW.Me.BoundingHeight + 2f), checkPoint) || !GameWorld.IsInLineOfSight(location.Add(0f, 0f, StyxWoW.Me.BoundingHeight / 2f), checkPoint))
                {
                    Logging.WriteVerbose("Trying dismount");
                    Mount.Dismount("Stuck Handler");
                }
                TriedDismount = true;
            }
            else if (!TriedJump)
            {
                if (GameWorld.IsInLineOfSight(location.Add(0f, 0f, StyxWoW.Me.BoundingHeight / 2f), checkPoint))
                {
                    Logging.WriteVerbose("Trying jump");
                    WoWMovement.Move(WoWMovement.MovementDirection.Forward | WoWMovement.MovementDirection.JumpAscend);
                    StyxWoW.Sleep(100);
                    WoWMovement.MoveStop(WoWMovement.MovementDirection.Forward | WoWMovement.MovementDirection.JumpAscend);
                    StyxWoW.Sleep(200);
                }
                TriedJump = true;
            }
            else if (!TriedStrafeForwardLeft)
            {
                Logging.WriteVerbose("Trying strafe forward left for {0}ms", sleepDuration);
                WoWMovement.Move(WoWMovement.MovementDirection.Forward | WoWMovement.MovementDirection.StrafeLeft);
                StyxWoW.Sleep(sleepDuration);
                WoWMovement.MoveStop(WoWMovement.MovementDirection.Forward | WoWMovement.MovementDirection.StrafeLeft);
                StyxWoW.Sleep(200);
                TriedStrafeForwardLeft = true;
            }
            else if (!TriedStrafeForwardRight)
            {
                Logging.WriteVerbose("Trying strafe forward right for {0}ms", sleepDuration);
                WoWMovement.Move(WoWMovement.MovementDirection.Forward | WoWMovement.MovementDirection.StrafeRight);
                StyxWoW.Sleep(sleepDuration);
                WoWMovement.MoveStop(WoWMovement.MovementDirection.Forward | WoWMovement.MovementDirection.StrafeRight);
                StyxWoW.Sleep(200);
                TriedStrafeForwardRight = true;
            }
            else if (StyxWoW.Me.Mounted)
            {
                Logging.WriteVerbose("Trying dismount");
                Mount.Dismount("Stuck Handler");
            }
            else if (!TriedStrafeLeft)
            {
                Logging.WriteVerbose("Trying strafe left for {0}ms", sleepDuration);
                WoWMovement.Move(WoWMovement.MovementDirection.StrafeLeft);
                StyxWoW.Sleep(sleepDuration);
                WoWMovement.MoveStop(WoWMovement.MovementDirection.StrafeLeft);
                StyxWoW.Sleep(200);
                TriedStrafeLeft = true;
            }
            else if (!TriedStrafeRight)
            {
                Logging.WriteVerbose("Trying strafe right for {0}ms", sleepDuration);
                WoWMovement.Move(WoWMovement.MovementDirection.StrafeRight);
                StyxWoW.Sleep(sleepDuration);
                WoWMovement.MoveStop(WoWMovement.MovementDirection.StrafeRight);
                StyxWoW.Sleep(200);
                TriedStrafeRight = true;
            }
            else
            {
                BlackspotManager.AddBlackspot(StyxWoW.Me.Location, 5f, 3f, "StuckHandler");
                Logging.WriteVerbose("Adding blackspot at current location and backing off for {0}ms", sleepDuration * 2);
                WoWMovement.MoveStop();
                StyxWoW.Sleep(100);
                WoWMovement.Move(WoWMovement.MovementDirection.Backwards);
                StyxWoW.Sleep(sleepDuration * 2);
                WoWMovement.MoveStop();
                StyxWoW.Sleep(200);
                Navigator.Clear();
                ResetVariables();
            }

            _stuckStopwatch.Restart();
        }

        public override void Reset()
        {
            _stuckStopwatch.Restart();
            _lastStuckLocation = WoWPoint.Empty;
            ResetVariables();
        }

        #endregion
    }
}
