using System;

namespace Styx.Common
{
    public static class ShapeHelper
    {
        public static void Box(Vector3 center, float width, float height, float depth,
            out Vector3[] vertices, out int[] indices)
        {
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float halfDepth = depth / 2f;
            vertices = new[]
            {
                new Vector3(center.X - halfWidth, center.Y - halfHeight, center.Z - halfDepth),
                new Vector3(center.X - halfWidth, center.Y - halfHeight, center.Z + halfDepth),
                new Vector3(center.X - halfWidth, center.Y + halfHeight, center.Z + halfDepth),
                new Vector3(center.X - halfWidth, center.Y + halfHeight, center.Z - halfDepth),
                new Vector3(center.X + halfWidth, center.Y + halfHeight, center.Z + halfDepth),
                new Vector3(center.X + halfWidth, center.Y - halfHeight, center.Z + halfDepth),
                new Vector3(center.X + halfWidth, center.Y - halfHeight, center.Z - halfDepth),
                new Vector3(center.X + halfWidth, center.Y + halfHeight, center.Z - halfDepth)
            };
            indices = new[]
            {
                0, 1, 2, 2, 3, 0, 2, 4, 7, 7, 3, 2, 2, 1, 5, 5, 4, 2,
                0, 3, 7, 7, 6, 0, 4, 5, 6, 6, 7, 4, 6, 5, 1, 1, 0, 6
            };
        }

        public static void Disc(Vector3 center, float radius, int slices,
            out Vector3[] vertices, out int[] indices)
        {
            float step = (float)(6.2831853071795862 / slices);
            vertices = new Vector3[slices + 1];
            vertices[0] = center;
            float angle = 0f;
            for (int i = 1; i < slices + 1; i++)
            {
                float sin = (float)Math.Sin(angle);
                float cos = (float)Math.Cos(angle);
                vertices[i] = new Vector3(center.X + cos * radius, center.Y, center.Z + sin * radius);
                angle += step;
            }

            indices = new int[slices * 3];
            for (int i = 0; i < slices; i++)
            {
                indices[i * 3] = i + 1;
                indices[i * 3 + 1] = i == slices - 1 ? 1 : i + 2;
                indices[i * 3 + 2] = 0;
            }
        }

        public static void Sphere(Vector3 center, float radius, int stacks, int slices,
            out Vector3[] vertices, out int[] indices)
        {
            int vertexCount = (stacks + 1) * (slices + 1);
            int indexCount = 3 * stacks * (slices + 1) * 2;
            indices = new int[indexCount];
            vertices = new Vector3[vertexCount];
            float stackStep = (float)(3.1415926535897931 / stacks);
            float sliceStep = (float)(6.2831853071795862 / slices);
            int baseIndex = 0;
            int vertexIndex = 0;
            int index = 0;
            for (int i = 0; i < stacks + 1; i++)
            {
                float stackSin = (float)Math.Sin(i * stackStep);
                float stackCos = (float)Math.Cos(i * stackStep);
                for (int j = 0; j < slices + 1; j++)
                {
                    float x = stackSin * (float)Math.Sin(j * sliceStep);
                    float z = stackSin * (float)Math.Cos(j * sliceStep);
                    vertices[vertexIndex++] = center + new Vector3(x * radius, stackCos * radius, z * radius);
                    if (i != stacks - 1)
                    {
                        indices[index++] = baseIndex + slices + 1;
                        indices[index++] = baseIndex + 1;
                        indices[index++] = baseIndex;
                        indices[index++] = baseIndex + slices;
                        indices[index++] = baseIndex + slices + 1;
                        indices[index++] = baseIndex;
                        baseIndex++;
                    }
                }
            }
        }
    }
}
