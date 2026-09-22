using System;

namespace Styx.Common
{
    public static class MathEx
    {
        private static readonly Random RandomGenerator = new Random();

        public static double Random(double min, double max)
        {
            return min + RandomGenerator.NextDouble() * (max - min);
        }

        public static float Lerp(float min, float max, float amount)
        {
            return min + (max - min) * amount;
        }

        public static float InverseLerp(float min, float max, float amount)
        {
            return (amount - min) / (max - min);
        }

        public static float GetAmount(float min, float max, float value)
        {
            return (value - min) / (max - min);
        }

        public static float ToRadians(float degrees)
        {
            return (float)(degrees * 0.017453292519943295);
        }

        public static float ToDegrees(float radians)
        {
            return (float)(radians * 57.295779513082323);
        }

        public static float DistanceSqr(ref Vector3 point, ref LineSegment line)
        {
            if (line.Start == line.End)
                return Vector3.DistanceSqr(ref point, ref line.Start);

            Vector3 direction = line.End - line.Start;
            float startProjection = Vector3.Dot(direction, point - line.Start);
            if (startProjection < 0f)
                return Vector3.DistanceSqr(ref point, ref line.Start);

            float endProjection = Vector3.Dot(-direction, point - line.End);
            if (endProjection < 0f)
                return Vector3.DistanceSqr(ref point, ref line.End);

            return Vector3.Cross(point - line.Start, point - line.End).MagnitudeSqr /
                   (line.End - line.Start).MagnitudeSqr;
        }

        public static float DistanceSqr(Vector3 point, LineSegment line)
        {
            return DistanceSqr(ref point, ref line);
        }

        public static float Distance(ref Vector3 point, ref LineSegment line)
        {
            return (float)Math.Sqrt(DistanceSqr(ref point, ref line));
        }

        public static float Distance(Vector3 point, LineSegment line)
        {
            return (float)Math.Sqrt(DistanceSqr(ref point, ref line));
        }

        public static float WrapAngle(float radian)
        {
            double angle = Math.IEEERemainder(radian, 6.2831853071795862);
            if (angle <= -3.1415926535897931)
                angle += 6.2831853071795862;
            else if (angle > 3.1415926535897931)
                angle -= 6.2831853071795862;
            return (float)angle;
        }

        public static Vector3 CalculatePointFrom(Vector3 myLocation, Vector3 targetLocation, float pointDistanceToTarget)
        {
            Vector3 direction = targetLocation - myLocation;
            direction.Normalize();
            return targetLocation - direction * pointDistanceToTarget;
        }

        public static Vector3 GetPointAt(Vector3 from, float distance, float rotationRadians)
        {
            float x = (float)(Math.Cos(rotationRadians) * distance);
            float y = (float)(Math.Sin(rotationRadians) * distance);
            return from + new Vector3(x, y, 0f);
        }

        public static bool IntersectsPath(Vector3 target, float radius, Vector3 start, Vector3 destination)
        {
            var ray = new Ray(start, Vector3.NormalizedDirection(start, destination));
            var sphere = new Sphere(target, Clamp(5f, 10f, radius));
            float? intersection = ray.Intersects(sphere);
            return intersection.HasValue && intersection.Value < start.Distance(destination);
        }

        public static bool IsOnSegment(double xi, double yi, double xj, double yj, double xk, double yk)
        {
            return (xi <= xk || xj <= xk) && (xk <= xi || xk <= xj) &&
                   (yi <= yk || yj <= yk) && (yk <= yi || yk <= yj);
        }

        public static int ComputeDirection(double xi, double yi, double xj, double yj, double xk, double yk)
        {
            double first = (xk - xi) * (yj - yi);
            double second = (xj - xi) * (yk - yi);
            if (first < second)
                return -1;
            return first <= second ? 0 : 1;
        }

        public static bool DoLineSegmentsIntersect(double x1, double y1, double x2, double y2,
            double x3, double y3, double x4, double y4)
        {
            int first = ComputeDirection(x3, y3, x4, y4, x1, y1);
            int second = ComputeDirection(x3, y3, x4, y4, x2, y2);
            int third = ComputeDirection(x1, y1, x2, y2, x3, y3);
            int fourth = ComputeDirection(x1, y1, x2, y2, x4, y4);
            return (((first > 0 && second < 0) || (first < 0 && second > 0)) &&
                    ((third > 0 && fourth < 0) || (third < 0 && fourth > 0))) ||
                   (first == 0 && IsOnSegment(x3, y3, x4, y4, x1, y1)) ||
                   (second == 0 && IsOnSegment(x3, y3, x4, y4, x2, y2)) ||
                   (third == 0 && IsOnSegment(x1, y1, x2, y2, x3, y3)) ||
                   (fourth == 0 && IsOnSegment(x1, y1, x2, y2, x4, y4));
        }

        public static bool DoLineSegmentsIntersect(Vector2 line1Start, Vector2 line1End,
            Vector2 line2Start, Vector2 line2End)
        {
            return DoLineSegmentsIntersect(line1Start.X, line1Start.Y, line1End.X, line1End.Y,
                line2Start.X, line2Start.Y, line2End.X, line2End.Y);
        }

        public static float Clamp(float min, float max, float value)
        {
            if (value <= min)
                return min;
            if (value >= max)
                return max;
            return value;
        }
    }
}
