using System;
using System.Runtime.InteropServices;

namespace Tripper.Tools.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BoundingSphere
    {
        public BoundingSphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public Vector3 Center;

        public float Radius;
    }
}
