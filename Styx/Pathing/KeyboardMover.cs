using System;
using Styx.Helpers;
using Styx.WoWInternals;

namespace Styx.Pathing
{
    public class KeyboardMover : IPlayerMover
    {
        private float lastRotation;

        public void Move(WoWMovement.MovementDirection direction)
        {
            WoWMovement.Move(direction);
        }

        private float CalculateFacingDelta(WoWPoint destination)
        {
            float neededFacing = WoWMathHelper.CalculateNeededFacing(StyxWoW.Me.Location, destination);
            float rotation = StyxWoW.Me.Rotation;
            lastRotation = ((rotation != 0f) ? rotation : lastRotation);
            float pi = (float)Math.PI;
            float delta = neededFacing - lastRotation;
            if (delta > pi)
            {
                delta = 0f - (pi - (delta - pi));
            }
            else if (delta < 0f - pi)
            {
                delta = pi - (0f - pi - delta);
            }
            return delta;
        }

        public void MoveTowards(WoWPoint location)
        {
            if (WoWMathHelper.IsFacing(StyxWoW.Me.Location, StyxWoW.Me.Rotation, location, 2f))
            {
                WoWMovement.Move(WoWMovement.MovementDirection.Forward);
            }
            else
            {
                WoWMovement.MoveStop();
            }
            WoWMathHelper.CalculateNeededFacing(StyxWoW.Me.Location, location);
            _ = StyxWoW.Me.Rotation;
            StyxWoW.Me.Location.Distance(location);
            float facingDelta = CalculateFacingDelta(location);
            float turnStep = Math.Abs(facingDelta) / 6f;
            if ((double)turnStep < 0.1)
            {
                turnStep = Math.Abs(facingDelta);
            }
            float turnThreshold = turnStep + 0.05f;
            if (facingDelta < 0f)
            {
                bool turning = true;
                while (turning)
                {
                    StyxWoW.Me.SetFacing(StyxWoW.Me.Rotation - turnStep);
                    facingDelta = CalculateFacingDelta(location);
                    if ((double)(turnThreshold - facingDelta) < (double)turnThreshold + 0.1 || (facingDelta > 0f && facingDelta > turnThreshold))
                    {
                        turning = false;
                    }
                }
            }
            else
            {
                if (!(facingDelta > 0f))
                {
                    return;
                }
                bool turning = true;
                while (turning)
                {
                    StyxWoW.Me.SetFacing(StyxWoW.Me.Rotation + turnStep);
                    facingDelta = CalculateFacingDelta(location);
                    if ((double)(facingDelta - turnThreshold) < 0.1 || (facingDelta < 0f && facingDelta < 0f - turnThreshold))
                    {
                        turning = false;
                    }
                }
            }
        }

        public void MoveStop()
        {
            WoWMovement.MoveStop();
        }
    }
}
