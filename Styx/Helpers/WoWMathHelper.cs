using System;
using System.Collections.Generic;
using System.Linq;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

namespace Styx.Helpers
{
    public static class WoWMathHelper
    {
        public static float DegreesToRadians(float degrees)
        {
            return (float)((double)degrees * 0.017453292519943295);
        }

        public static float RadiansToDegrees(float radians)
        {
            return (float)((double)radians * 57.295779513082323);
        }

        public static bool IsSafelyBehind(WoWPoint me, WoWPoint target, float targetFacingRadians)
        {
            return IsBehind(me, target, targetFacingRadians, DegreesToRadians(260f));
        }

        public static bool IsBehind(WoWPoint me, WoWPoint target, float targetFacingRadians, float arcRadians = 3.14159274f)
        {
            Vector3 targetToMe = me - target;
            targetToMe.Z = 0f;
            Vector3 targetDir = new Vector3((float)Math.Cos((double)targetFacingRadians), (float)Math.Sin((double)targetFacingRadians), 0f);

            float dot = Vector3.Dot(ref targetDir, ref targetToMe);
            float angle = (float)Math.Acos(Clamp((double)(dot / (targetToMe.Magnitude * targetDir.Magnitude)), -1.0, 1.0));

            return angle >= arcRadians / 2f;
        }

        public static bool IsSafelyFacing(WoWPoint me, float myFacingRadians, WoWPoint target)
        {
            return IsFacing(me, myFacingRadians, target, DegreesToRadians(100f));
        }

        public static bool IsFacing(WoWPoint me, float myFacingRadians, WoWPoint target, float arcRadians = 3.14159274f)
        {
            Vector3 meToTarget = target - me;
            meToTarget.Z = 0f;
            Vector3 myDir = new Vector3((float)Math.Cos((double)myFacingRadians), (float)Math.Sin((double)myFacingRadians), 0f);

            float dot = Vector3.Dot(ref myDir, ref meToTarget);
            float angle = (float)Math.Acos(Clamp((double)(dot / (meToTarget.Magnitude * myDir.Magnitude)), -1.0, 1.0));

            return angle <= arcRadians / 2f;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }

        public static float CalculateNeededFacing(WoWPoint start, WoWPoint faceTarget)
        {
            return NormalizeRadian((float)Math.Atan2((double)(faceTarget.Y - start.Y), (double)(faceTarget.X - start.X)));
        }

        public static WoWPoint CalculatePointBehind(WoWPoint target, float targetFacingRadians, float distanceToTarget)
        {
            targetFacingRadians = NormalizeRadian(targetFacingRadians);
            WoWPoint direction = new WoWPoint((float)Math.Cos((double)targetFacingRadians), (float)Math.Sin((double)targetFacingRadians), 0f);
            return target - direction * distanceToTarget;
        }

        public static WoWPoint CalculatePointInFront(WoWPoint target, float targetFacingRadians, float distanceToTarget)
        {
            targetFacingRadians = NormalizeRadian(targetFacingRadians);
            WoWPoint direction = new WoWPoint((float)Math.Cos((double)targetFacingRadians), (float)Math.Sin((double)targetFacingRadians), 0f);
            return target + direction * distanceToTarget;
        }

        public static WoWPoint CalculatePointAtSide(WoWPoint target, float targetFacingInRadians, float distanceToTarget, bool rightSide)
        {
            if (rightSide)
            {
                targetFacingInRadians += 1.57079637f;
            }
            else
            {
                targetFacingInRadians += 4.712389f;
            }

            return CalculatePointBehind(target, targetFacingInRadians, distanceToTarget);
        }

        public static WoWPoint CalculatePointFrom(WoWPoint from, WoWPoint target, float distance)
        {
            WoWPoint direction = WoWPoint.GetDirection(target, from);
            return target + direction * distance;
        }

        public static float NormalizeRadian(float radian)
        {
            if (radian < 0f)
            {
                double num = -(double)radian;
                num %= 6.2831853071795862;
                num = -num + 6.2831853071795862;
                return (float)num;
            }
            return (float)((double)radian % 6.2831853071795862);
        }

        public static bool IsInPath(WoWPoint location, float aggroRange, WoWPoint myLocation, List<WoWPoint> path)
        {
            if (IsInPath(location, aggroRange, myLocation, path[0]))
            {
                return true;
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (IsInPath(location, aggroRange, path[i], path[i + 1]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsInPath(WoWUnit unit, WoWPoint start, WoWPoint destination)
        {
            if (unit.IsDead)
            {
                return false;
            }
            WoWPoint location = unit.Location;
            float myAggroRange = unit.MyAggroRange;
            return IsInPath(location, myAggroRange, start, destination);
        }

        public static bool IsInPath(WoWPoint location, float radius, WoWPoint start, WoWPoint destination)
        {
            BoundingSphere sphere = new BoundingSphere(location, radius);
            return IsInPath(sphere, start, destination);
        }

        public static bool IsInPath(BoundingSphere sphere, WoWPoint start, WoWPoint destination)
        {
            Ray ray = new Ray(start, start.GetDirectionTo(destination));
            float? num = ray.Intersects(sphere);
            return num != null && num.Value < start.Distance(destination);
        }

        public static bool IsInPath(IEnumerable<WoWUnit> units, WoWPoint destination)
        {
            WoWPoint myLocation = StyxWoW.Me.Location;
            return units.Any(unit => IsInPath(unit, myLocation, destination));
        }

        public static WoWUnit GetClosestInPath(IEnumerable<WoWUnit> units, WoWPoint destination)
        {
            WoWPoint myLocation = StyxWoW.Me.Location;
            return units.Where(unit => IsInPath(unit, myLocation, destination))
                .OrderBy(unit => unit.DistanceSqr)
                .FirstOrDefault();
        }

        public static WoWPoint GetPointAt(WoWPoint from, float distance, float rotationRadians, float pitchRadians)
        {
            float x = (float)(Math.Cos((double)pitchRadians) * Math.Cos((double)rotationRadians) * (double)distance);
            float y = (float)(Math.Cos((double)pitchRadians) * Math.Sin((double)rotationRadians) * (double)distance);
            float z = (float)(Math.Sin((double)pitchRadians) * (double)distance);
            return from + new WoWPoint(x, y, z);
        }

        private static float GetAngle(WoWObject obj, float x, float y)
        {
            float dx = x - obj.X;
            float dy = y - obj.Y;
            float ang = (float)Math.Atan2((double)dy, (double)dx);
            return (float)((ang >= 0f) ? ((double)ang) : (6.2831853071795862 + (double)ang));
        }

        public static bool IsBehind(WoWObject target, float arcAngle, WoWObject caster)
        {
            if (target == caster)
            {
                return true;
            }
            float arc = NormalizeRadian(arcAngle);
            float angle = GetAngle(target, caster.X, caster.Y);
            angle = NormalizeRadian(angle);
            if ((double)angle > 3.1415926535897931)
            {
                angle -= 6.28318548f;
            }
            float lborder = -1f * (arc / 2f);
            float rborder = arc / 2f;
            return angle >= lborder && angle <= rborder;
        }

        public static bool IsBehind(WoWObject target, WoWObject caster)
        {
            return IsBehind(target, 3.14159274f, caster);
        }
    }
}
