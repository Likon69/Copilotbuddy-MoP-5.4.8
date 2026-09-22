namespace Styx.CommonBot.AreaManagement.Triangulation
{
    public struct Triangle
    {
        public int P1;
        public int P2;
        public int P3;

        public Triangle(int p1, int p2, int p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }
    }
}
