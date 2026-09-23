using System;

namespace Tripper.Tools.Math
{
    public static class MathEx
    {
        public static float ToRadians(float degrees)
        {
            return (float)((double)degrees * 0.017453292519943295);
        }

        public static void ToRadians(ref float angle)
        {
            angle = (float)((double)angle * 0.017453292519943295);
        }

        public static float ToDegrees(float radians)
        {
            return (float)((double)radians * 57.295779513082323);
        }

        public static void ToDegrees(ref float angle)
        {
            angle = (float)((double)angle * 57.295779513082323);
        }

        public static float NormalizeRadians(float radians)
        {
            while (radians < 0f)
            {
                radians += 6.28318548f;
            }
            while (radians >= 6.28318548f)
            {
                radians -= 6.28318548f;
            }
            return radians;
        }

        public static float NormalizeDegrees(float degrees)
        {
            while (degrees < 0f)
            {
                degrees += 360f;
            }
            while (degrees >= 360f)
            {
                degrees -= 360f;
            }
            return degrees;
        }

        public static float Lerp(float min, float max, float value)
        {
            return min + (max - min) * value;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }
            return value;
        }

        private static void CalculateNormal(ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, out Vector3 normal)
        {
            Vector3 e0;
            Vector3.Subtract(ref v2, ref v1, out e0);
            Vector3 e1;
            Vector3.Subtract(ref v3, ref v1, out e1);
            Vector3.Cross(ref e0, ref e1, out normal);
        }

        public static Vector3[] CalculateNormalsPerVertex(Vector3[] vertices, int[] indices)
        {
            Vector3[] normals = new Vector3[vertices.Length];
            for (int i = 0; i < indices.Length / 3; i++)
            {
                int i1 = indices[i * 3 + 0];
                int i2 = indices[i * 3 + 1];
                int i3 = indices[i * 3 + 2];
                Vector3 e0;
                Vector3.Subtract(ref vertices[i2], ref vertices[i1], out e0);
                Vector3 e1;
                Vector3.Subtract(ref vertices[i3], ref vertices[i1], out e1);
                Vector3 normal;
                Vector3.Cross(ref e0, ref e1, out normal);
                normal.Normalize();
                Vector3.Add(ref normals[i1], ref normal, out normals[i1]);
                Vector3.Add(ref normals[i2], ref normal, out normals[i2]);
                Vector3.Add(ref normals[i3], ref normal, out normals[i3]);
            }
            for (int j = 0; j < normals.Length; j++)
            {
                normals[j].Normalize();
            }
            return normals;
        }

        public static T Min<T>(T left, T right) where T : IComparable<T>
        {
            if (left.CompareTo(right) < 0)
            {
                return left;
            }
            return right;
        }

        public static T Max<T>(T left, T right) where T : IComparable<T>
        {
            if (left.CompareTo(right) > 0)
            {
                return left;
            }
            return right;
        }

        public unsafe static void ClipPolygonToPlane(Vector3[] inVerts, int numInVerts, Vector3[] outVerts, out int numOutVerts, Plane plane, bool keepOnPlane, float epsilon)
        {
            if (numInVerts > 32)
            {
                throw new ArgumentOutOfRangeException("numInVerts");
            }

            int* ptr = stackalloc int[32];
            float* ptr2 = stackalloc float[32];
            int* ptr3 = stackalloc int[3];
            ptr3[0] = 0;
            ptr3[1] = 0;
            ptr3[2] = 0;

            for (int i = 0; i < numInVerts; i++)
            {
                float num = inVerts[i].X * plane.Normal.X + inVerts[i].Y * plane.Normal.Y + inVerts[i].Z * plane.Normal.Z + plane.D;
                ptr2[i] = num;
                int num2;
                if (num > epsilon)
                {
                    num2 = 1;
                }
                else if (num < -epsilon)
                {
                    num2 = 2;
                }
                else
                {
                    num2 = 0;
                }
                ptr[i] = num2;
                ptr3[num2]++;
            }

            if (ptr3[2] != 0)
            {
                numOutVerts = 0;
                for (int j = 0; j < numInVerts; j++)
                {
                    int num3 = ptr[j];
                    if (num3 == 0)
                    {
                        int num4 = numOutVerts;
                        numOutVerts = num4 + 1;
                        outVerts[num4] = inVerts[j];
                    }
                    else
                    {
                        if (num3 == 1)
                        {
                            int num4 = numOutVerts;
                            numOutVerts = num4 + 1;
                            outVerts[num4] = inVerts[j];
                        }
                        int num5 = (j == numInVerts - 1) ? 0 : (j + 1);
                        int num6 = ptr[num5];
                        if (num6 != 0 && num6 != num3)
                        {
                            float rs = ptr2[j] / (ptr2[j] - ptr2[num5]);
                            int num4 = numOutVerts;
                            numOutVerts = num4 + 1;
                            outVerts[num4] = inVerts[j] + (inVerts[num5] - inVerts[j]) * rs;
                        }
                    }
                }
                return;
            }

            if (ptr3[1] == 0 && !keepOnPlane)
            {
                numOutVerts = 0;
                return;
            }

            Array.Copy(inVerts, outVerts, numInVerts);
            numOutVerts = numInVerts;
        }
    }
}
