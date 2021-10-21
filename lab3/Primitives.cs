using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Numerics;
using System.Globalization;
using System.Linq;

namespace Primitives
{
    public class Vertex
    {
        public Vector4 Point;
        public Vector4 PointInWorld;

        public List<Polygon> Polygons;
        public Vector4 Normal;
        public Vector4 NormalInWorld;

        public Vertex(float x, float y, float z)
        {
            Point = new Vector4(x, y, z, 1);
            Polygons = new List<Polygon>();
        }
        
        public Vertex(Vector4 point)
        {
            Point = point;
            Polygons = new List<Polygon>();
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
        public Material Material;

        public Polygon(Vertex a, Vertex b, Vertex c, Material material)
        {
            Vertices = new Vertex[] {a, b, c};
            CalculateNormal();
            Material = material;
            a.Polygons.Add(this);
            b.Polygons.Add(this);
            c.Polygons.Add(this);
        }

        public Polygon(Vertex a, Vertex b, Vertex c, Vertex d, Material material)
        {
            Vertices = new Vertex[] {a, b, c, d};
            CalculateNormal();
            Material = material;
            a.Polygons.Add(this);
            b.Polygons.Add(this);
            c.Polygons.Add(this);
            d.Polygons.Add(this);
        }

        public Polygon(Vertex[] vertices, Material material)
        {
            Vertices = vertices;
            CalculateNormal();
            Material = material;
            foreach (Vertex vertex in Vertices)
            {
                vertex.Polygons.Add(this);
            }
        }
        
        private void CalculateNormal()
        {
            Vertex a = Vertices[0], b = Vertices[1], c = Vertices[2];
            Vector3 vec1 = new Vector3(b.Point.X - a.Point.X, b.Point.Y - a.Point.Y, b.Point.Z - a.Point.Z);
            Vector3 vec2 = new Vector3(c.Point.X - a.Point.X, c.Point.Y - a.Point.Y, c.Point.Z - a.Point.Z);
            Normal = new Vector4(Vector3.Normalize(Vector3.Cross(vec1, vec2)), 0);
        }
    }

    public class Mesh
    {
        public List<Vertex> Vertices;
        public List<Polygon> Polygons;

        public Mesh()
        {
            Vertices = new List<Vertex>();
            Polygons = new List<Polygon>();
        }

        public Mesh(List<Vertex> vertices, List<Polygon> polygons)
        {
            Vertices = vertices;
            Polygons = polygons;
            CalculateVerticesNormals();
        }

        public void CalculateVerticesNormals()
        {
            foreach (Vertex vertex in Vertices)
            {
                Vector4 normal = Vector4.Zero;
                foreach (Polygon polygon in vertex.Polygons)
                {
                    normal += polygon.Normal;
                }
                vertex.Normal = Vector4.Normalize(normal);
            }
        }
    }

    public class Material
    {
        public Vector3 Color;
        public Vector3 Ka;
        public Vector3 Kd;
        public Vector3 Ks;
        public float P;

        public Material(Vector3 color, Vector3 ka, Vector3 kd, Vector3 ks, float p)
        {
            Color = color;
            Ka = ka;
            Kd = kd;
            Ks = ks;
            P = p;
        }
    }

    public class AmbientLight
    {
        public Vector3 Intensity;

        public AmbientLight(Vector3 intensity)
        {
            Intensity = intensity;
        }
    }

    public class PointLight
    {
        public Vector4 Point;
        public Vector3 Intensity;
        public float K;

        public PointLight(Vector3 intensity, Vector4 point, float k)
        {
            Intensity = intensity;
            Point = point;
            K = k;
        }
    }

    public static class PrimitiveForms
    {
        public static void LoadFromObj(string filename, Mesh mesh, Material material)
        {
            mesh.Vertices.Clear();
            mesh.Polygons.Clear();
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
                    mesh.Vertices.Add(new Vertex(x, y, z));
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
                        v.Add(mesh.Vertices[Convert.ToInt32(ids[0]) - 1]);
                        if (ids.Length == 3)
                            normal = Convert.ToInt32(ids[2]) - 1;
                    }

                    var polygon = new Polygon(v.ToArray(), material);
                    if (normal != -1)
                        polygon.Normal = new Vector4(normals[normal], 0);
                    mesh.Polygons.Add(polygon);
                }
            }
            mesh.CalculateVerticesNormals();
            file.Close();
        }

        public static void Prism(int sides, float height, float radius, Mesh mesh, Material material)
        {
            mesh.Vertices.Clear();
            mesh.Polygons.Clear();
            height /= 2;
            var dphi = 2 * Math.PI / sides;
            var rotation = Matrix4x4.CreateRotationY((float) dphi);
            mesh.Vertices.Add(new Vertex(0, height, 0));
            mesh.Vertices.Add(new Vertex(radius, height, 0));
            for (int i = 0; i < sides; ++i)
            {
                mesh.Vertices.Add(new Vertex(Vector4.Transform(mesh.Vertices.Last().Point, rotation)));
                mesh.Polygons.Add(new Polygon(mesh.Vertices[i+1], mesh.Vertices[i+2], mesh.Vertices[0], material));
            }
            mesh.Vertices.Add(new Vertex(0, -height, 0));
            mesh.Vertices.Add(new Vertex(radius, -height, 0));
            for (int i = 0; i < sides; ++i)
            {
                mesh.Vertices.Add(new Vertex(Vector4.Transform(mesh.Vertices.Last().Point, rotation)));
                mesh.Polygons.Add(new Polygon(mesh.Vertices[i+sides+4], mesh.Vertices[i+sides+3], mesh.Vertices[sides+2], material));
            }
            for (int i = 1; i <= sides; ++i)
            {
                mesh.Polygons.Add(new Polygon(mesh.Vertices[i+1], mesh.Vertices[i], mesh.Vertices[i+sides+2], mesh.Vertices[i+sides+3], material));
            }
            mesh.CalculateVerticesNormals();
        }

        public static void Cube(Mesh mesh, Material material)
        {
            mesh.Vertices = new List<Vertex>
            {
                new Vertex(-.5f, -.5f, -.5f), new Vertex(.5f, -.5f, -.5f), new Vertex(.5f, .5f, -.5f), new Vertex(-.5f, .5f, -.5f),
                new Vertex(-.5f, -.5f, .5f), new Vertex(.5f, -.5f, .5f), new Vertex(.5f, .5f, .5f), new Vertex(-.5f, .5f, .5f),
            };
            mesh.Polygons = new List<Polygon>
            {
                new Polygon(mesh.Vertices[3], mesh.Vertices[2], mesh.Vertices[1], mesh.Vertices[0], material),
                new Polygon(mesh.Vertices[4], mesh.Vertices[5], mesh.Vertices[6], mesh.Vertices[7], material),
                new Polygon(mesh.Vertices[5], mesh.Vertices[4], mesh.Vertices[0], mesh.Vertices[1], material),
                new Polygon(mesh.Vertices[6], mesh.Vertices[5], mesh.Vertices[1], mesh.Vertices[2], material),
                new Polygon(mesh.Vertices[7], mesh.Vertices[6], mesh.Vertices[2], mesh.Vertices[3], material),
                new Polygon(mesh.Vertices[4], mesh.Vertices[7], mesh.Vertices[3], mesh.Vertices[0], material),
            };
            mesh.CalculateVerticesNormals();
        }
        
        public static void Octahedron(Mesh mesh, Material material)
        {
            mesh.Vertices = new List<Vertex>
            {
                new Vertex(0, 0, -1),
                new Vertex(1, 0, 0), new Vertex(0, 1, 0),
                new Vertex(-1, 0, 0), new Vertex(0, -1, 0),
                new Vertex(0, 0, 1)
            };
            mesh.Polygons = new List<Polygon>
            {
                new Polygon(mesh.Vertices[0], mesh.Vertices[2], mesh.Vertices[1], material),
                new Polygon(mesh.Vertices[0], mesh.Vertices[3], mesh.Vertices[2], material),
                new Polygon(mesh.Vertices[0], mesh.Vertices[4], mesh.Vertices[3], material),
                new Polygon(mesh.Vertices[0], mesh.Vertices[1], mesh.Vertices[4], material),
                new Polygon(mesh.Vertices[2], mesh.Vertices[5], mesh.Vertices[1], material),
                new Polygon(mesh.Vertices[1], mesh.Vertices[5], mesh.Vertices[4], material),
                new Polygon(mesh.Vertices[4], mesh.Vertices[5], mesh.Vertices[3], material),
                new Polygon(mesh.Vertices[3], mesh.Vertices[5], mesh.Vertices[2], material)
            };
            mesh.CalculateVerticesNormals();
        }
    }
}
