using System;
using System.Collections.Generic;
using Styx.Resources;
using Tripper.Tools.Math;

namespace Styx.CommonBot.AreaManagement.Triangulation
{
    internal static class Triangulator
    {
        public static List<Triangle> Triangulate(List<Vector2> vertices)
        {
            int vertexCount = vertices.Count;
            if (vertexCount < 3)
                throw new ArgumentException(StyxResources.Need_at_least_three_vertices_for_triangulation);

            int maxTriangles = vertexCount * 4;
            Vector2 min = vertices[0];
            Vector2 max = min;
            for (int i = 1; i < vertexCount; i++)
            {
                min = Vector2.Min(min, vertices[i]);
                max = Vector2.Max(max, vertices[i]);
            }

            float dx = max.X - min.X;
            float dy = max.Y - min.Y;
            float maxDimension = Math.Max(dx, dy);
            Vector2 middle = (min + max) / 2f;

            vertices.Add(new Vector2(middle.X - maxDimension * 2f, middle.Y - maxDimension));
            vertices.Add(new Vector2(middle.X, middle.Y + maxDimension * 2f));
            vertices.Add(new Vector2(middle.X + maxDimension * 2f, middle.Y - maxDimension));
            var triangles = new List<Triangle> { new Triangle(vertexCount, vertexCount + 1, vertexCount + 2) };

            for (int i = 0; i < vertexCount; i++)
            {
                var edges = new List<Edge>();
                for (int j = 0; j < triangles.Count; j++)
                {
                    if (!InCircle(vertices[i], vertices[triangles[j].P1], vertices[triangles[j].P2], vertices[triangles[j].P3]))
                        continue;

                    edges.Add(new Edge(triangles[j].P1, triangles[j].P2));
                    edges.Add(new Edge(triangles[j].P2, triangles[j].P3));
                    edges.Add(new Edge(triangles[j].P3, triangles[j].P1));
                    triangles.RemoveAt(j);
                    j--;
                }

                if (i >= vertexCount)
                    continue;

                for (int j = edges.Count - 2; j >= 0; j--)
                {
                    for (int k = edges.Count - 1; k >= j + 1; k--)
                    {
                        if (edges[j] != edges[k])
                            continue;
                        edges.RemoveAt(k);
                        edges.RemoveAt(j);
                        k--;
                    }
                }

                for (int j = 0; j < edges.Count; j++)
                {
                    if (triangles.Count >= maxTriangles)
                        throw new Exception("Exceeded maximum edges");
                    triangles.Add(new Triangle(edges[j].P1, edges[j].P2, i));
                }
            }

            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                if (triangles[i].P1 >= vertexCount || triangles[i].P2 >= vertexCount || triangles[i].P3 >= vertexCount)
                    triangles.RemoveAt(i);
            }

            vertices.RemoveAt(vertices.Count - 1);
            vertices.RemoveAt(vertices.Count - 1);
            vertices.RemoveAt(vertices.Count - 1);
            return triangles;
        }

        private static bool InCircle(Vector2 point, Vector2 point1, Vector2 point2, Vector2 point3)
        {
            if (Math.Abs(point1.Y - point2.Y) < float.Epsilon && Math.Abs(point2.Y - point3.Y) < float.Epsilon)
                return false;

            float centerX;
            float centerY;
            if (Math.Abs(point2.Y - point1.Y) < float.Epsilon)
            {
                float slope2 = -(point3.X - point2.X) / (point3.Y - point2.Y);
                float middleX2 = (point2.X + point3.X) * 0.5f;
                float middleY2 = (point2.Y + point3.Y) * 0.5f;
                centerX = (point2.X + point1.X) * 0.5f;
                centerY = slope2 * (centerX - middleX2) + middleY2;
            }
            else if (Math.Abs(point3.Y - point2.Y) < float.Epsilon)
            {
                float slope1 = -(point2.X - point1.X) / (point2.Y - point1.Y);
                float middleX1 = (point1.X + point2.X) * 0.5f;
                float middleY1 = (point1.Y + point2.Y) * 0.5f;
                centerX = (point3.X + point2.X) * 0.5f;
                centerY = slope1 * (centerX - middleX1) + middleY1;
            }
            else
            {
                float slope1 = -(point2.X - point1.X) / (point2.Y - point1.Y);
                float slope2 = -(point3.X - point2.X) / (point3.Y - point2.Y);
                float middleX1 = (point1.X + point2.X) * 0.5f;
                float middleX2 = (point2.X + point3.X) * 0.5f;
                float middleY1 = (point1.Y + point2.Y) * 0.5f;
                float middleY2 = (point2.Y + point3.Y) * 0.5f;
                centerX = (slope1 * middleX1 - slope2 * middleX2 + middleY2 - middleY1) / (slope1 - slope2);
                centerY = slope1 * (centerX - middleX1) + middleY1;
            }

            float dx = point2.X - centerX;
            float dy = point2.Y - centerY;
            float radiusSquared = dx * dx + dy * dy;
            dx = point.X - centerX;
            dy = point.Y - centerY;
            float distanceSquared = dx * dx + dy * dy;
            return distanceSquared <= radiusSquared;
        }
    }
}
