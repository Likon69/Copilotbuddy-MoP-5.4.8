namespace Styx.Common
{
    public struct Quaternion
    {
        public float W;
        public Vector3 Vector;

        public override string ToString()
        {
            return string.Format("[Quaternion] W: {0}, Vector: {1}", W, Vector);
        }
    }
}
