namespace Styx.Common
{
    public struct Sphere
    {
        public Vector3 Center;
        public float Radius;

        public Sphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public override string ToString()
        {
            return string.Format("Center:{0} Radius:{1}", Center, Radius);
        }
    }
}
