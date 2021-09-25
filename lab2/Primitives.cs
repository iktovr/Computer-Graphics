using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Numerics;
using System.Globalization;

namespace Primitives
{
    public class Vertex
    {
        public Vector4 Point;
        public Vector4 PointInWorld;

        public Vertex(float x, float y, float z)
        {
            Point = new Vector4(x, y, z, 1);
        }

        public override string ToString()
        {
            return Point.ToString();
        }
    }

    public class Polygon
    {
        public Vertex[] Vertices;

        public Vector4 Normal;
        public Vector4 NormalInWorld;
        public Vector3 Color;

        public static Vector3 StandardColor = new Vector3(0.117f, 0.565f, 1);

        public Polygon(Vertex a, Vertex b, Vertex c, Vector3 color)
        {
            Vertices = new Vertex[] {a, b, c};
            CalculateNormal();
            Color = color;
        }

        public Polygon(Vertex a, Vertex b, Vertex c, Vertex d, Vector3 color)
        {
            Vertices = new Vertex[] {a, b, c, d};
            CalculateNormal();
            Color = color;
        }

        public Polygon(Vertex[] vertices, Vector3 color)
        {
            Vertices = vertices;
            CalculateNormal();
            Color = color;
        }
        
        public Polygon(Vertex a, Vertex b, Vertex c) : this(a, b, c, StandardColor) {}
        public Polygon(Vertex a, Vertex b, Vertex c, Vertex d) : this(a, b, c, d, StandardColor) {}
        public Polygon(Vertex[] vertices) : this(vertices, StandardColor) {}

        private void CalculateNormal()
        {
            Vertex a = Vertices[0], b = Vertices[1], c = Vertices[2];
            Vector3 vec1 = new Vector3(a.Point.X - b.Point.X, a.Point.Y - b.Point.Y, a.Point.Z - b.Point.Z);
            Vector3 vec2 = new Vector3(c.Point.X - a.Point.X, c.Point.Y - a.Point.Y, c.Point.Z - a.Point.Z);
            Normal = new Vector4(Vector3.Normalize(Vector3.Cross(vec1, vec2)), 0);
        }
    }

    public static class PrimitiveForms
    {
        public static void LoadFromObj(string filename, ref List<Vertex> vertices, ref List<Polygon> polygons)
        {
            vertices = new List<Vertex>();
            polygons = new List<Polygon>();
            var normals = new List<Vector3>();
            
            StreamReader file = new StreamReader(filename);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("#"))
                    continue;

                string[] items = line.Split(" ");
                if (items[0] == "v")
                {
                    var x = (float) Convert.ToDouble(items[1], CultureInfo.InvariantCulture);
                    var y = (float) Convert.ToDouble(items[2], CultureInfo.InvariantCulture);
                    var z = (float) Convert.ToDouble(items[3], CultureInfo.InvariantCulture);
                    vertices.Add(new Vertex(x, y, z));
                }
                else if (items[0] == "vn")
                {
                    var x = (float) Convert.ToDouble(items[1], CultureInfo.InvariantCulture);
                    var y = (float) Convert.ToDouble(items[2], CultureInfo.InvariantCulture);
                    var z = (float) Convert.ToDouble(items[3], CultureInfo.InvariantCulture);
                    normals.Add(new Vector3(x, y, z));
                }
                else if (items[0] == "f")
                {
                    var v = new List<Vertex>();
                    int normal = -1;
                    for (int i = 1; i < items.Length; ++i)
                    {
                        string[] ids = items[i].Split("/");
                        v.Add(vertices[Convert.ToInt32(ids[0]) - 1]);
                        if (ids.Length == 3)
                            normal = Convert.ToInt32(ids[2]) - 1;
                    }
                    var polygon = new Polygon(v.ToArray());
                    if (normal != -1)
                        polygon.Normal = new Vector4(normals[normal], 0);
                    polygons.Add(polygon);
                }
            }
            file.Close();
        }
        
        public static void Cube(ref List<Vertex> vertices, ref List<Polygon> polygons)
        {
            vertices = new List<Vertex>
            {
                new Vertex(-.5f, -.5f, -.5f), new Vertex(.5f, -.5f, -.5f), new Vertex(.5f, .5f, -.5f), new Vertex(-.5f, .5f, -.5f),
                new Vertex(-.5f, -.5f, .5f), new Vertex(.5f, -.5f, .5f), new Vertex(.5f, .5f, .5f), new Vertex(-.5f, .5f, .5f),
            };
            polygons = new List<Polygon>
            {
                new Polygon(vertices[0], vertices[1], vertices[2], vertices[3]),
                new Polygon(vertices[7], vertices[6], vertices[5], vertices[4]),
                new Polygon(vertices[1], vertices[0], vertices[4], vertices[5]),
                new Polygon(vertices[2], vertices[1], vertices[5], vertices[6]),
                new Polygon(vertices[3], vertices[2], vertices[6], vertices[7]),
                new Polygon(vertices[0], vertices[3], vertices[7], vertices[4]),
            };
        }
        
        public static void Octahedron(ref List<Vertex> vertices, ref List<Polygon> polygons)
        {
            vertices = new List<Vertex>
            {
                new Vertex(0, 0, -1),
                new Vertex(1, 0, 0), new Vertex(0, 1, 0),
                new Vertex(-1, 0, 0), new Vertex(0, -1, 0),
                new Vertex(0, 0, 1)
            };
            polygons = new List<Polygon>
            {
                new Polygon(vertices[1], vertices[2], vertices[5]),
                new Polygon(vertices[2], vertices[3], vertices[5]),
                new Polygon(vertices[3], vertices[4], vertices[5]),
                new Polygon(vertices[4], vertices[1], vertices[5]),
                new Polygon(vertices[1], vertices[0], vertices[2]),
                new Polygon(vertices[2], vertices[0], vertices[3]),
                new Polygon(vertices[3], vertices[0], vertices[4]),
                new Polygon(vertices[4], vertices[0], vertices[1])
            };
        }
    }
}