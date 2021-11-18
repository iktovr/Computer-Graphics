using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Globalization;
using System.Linq;

namespace Primitives
{
    public class Vertex
    {
        public Vector4 Point;
        public ushort Id;

        public List<Polygon> Polygons;
        public Vector4 Normal;

        public Vertex(float x, float y, float z, ushort id)
        {
            Point = new Vector4(x, y, z, 1);
            Polygons = new List<Polygon>();
            Id = id;
        }
        
        public Vertex(Vector4 point, ushort id)
        {
            Point = point;
            Polygons = new List<Polygon>();
            Id = id;
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
        public Vector3 Origin = Vector3.Zero;
        public Vector3 Scale = Vector3.One;
        public Vector3 Rotation = Vector3.Zero;

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

        public Matrix4x4 GetModelMatrix()
        {
            var rotation = Matrix4x4.CreateRotationX(Rotation.X) *
                           Matrix4x4.CreateRotationY(Rotation.Y) *
                           Matrix4x4.CreateRotationZ(Rotation.Z);
            var translation = Matrix4x4.Transpose(Matrix4x4.CreateTranslation(Origin));
            var scale = Matrix4x4.CreateScale(Scale);

            return translation * rotation * scale;
        }

        public void ToArray(bool coords, bool colors, bool normals, out float[] vertices, out ushort[] elements)
        {
            int multiplier = Convert.ToInt32(coords) + Convert.ToInt32(colors) + Convert.ToInt32(normals);
            vertices = new float[Vertices.Count * 3 * multiplier];
            int i = 0;
            foreach (Vertex vertex in Vertices)
            {
                if (coords)
                {
                    vertices[i] = vertex.Point.X;
                    vertices[i+1] = vertex.Point.Y;
                    vertices[i+2] = vertex.Point.Z;
                    i += 3;
                }
                if (colors)
                {
                    vertices[i] = 0;
                    vertices[i+1] = 0;
                    vertices[i+2] = 0;
                    i += 3;
                }
                if (normals)
                {
                    vertices[i] = vertex.Normal.X;
                    vertices[i+1] = vertex.Normal.Y;
                    vertices[i+2] = vertex.Normal.Z;
                    i += 3;
                }
            }

            int elementsCount = Polygons.Sum(a => a.Vertices.Length);
            elements = new ushort[elementsCount];
            i = 0;
            foreach (Polygon polygon in Polygons)
            {
                foreach (Vertex vertex in polygon.Vertices)
                {
                    elements[i] = vertex.Id;
                    ++i;
                }
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

        public Material()
        {
            Color = Vector3.One;
            Ka = Vector3.One;
            Kd = Vector3.One;
            Ks = Vector3.One;
            P = 0;
        }

        public Material(Vector3 color, Vector3 ka, Vector3 kd, Vector3 ks, float p)
        {
            Color = color;
            Ka = ka;
            Kd = kd;
            Ks = ks;
            P = p;
        }
    }

    public class Camera
    {
        public Vector3 Position;
        public Vector3 Target;
        public Vector3 Up;
        public float AspectRatio;
        public float FOV;
        public float NearPlane;
        public float FarPlane;

        public Camera(Vector3 position, Vector3 target, Vector3 up, float aspectRatio, float fov, float nearPlane, float farPlane)
        {
            Position = position;
            Target = target;
            Up = up;
            AspectRatio = aspectRatio;
            FOV = fov;
            NearPlane = nearPlane;
            FarPlane = farPlane;
        }

        public Matrix4x4 GetViewMatrix()
        {
            var cameraDirection = Vector3.Normalize(Position - Target);
            var cameraRight = Vector3.Cross(Up, cameraDirection);
            var matrix1 = new Matrix4x4(
                cameraRight.X, cameraRight.Y, cameraRight.Z, 0,
                Up.X, Up.Y, Up.Z, 0,
                cameraDirection.X, cameraDirection.Y, cameraDirection.Z, 0,
                0, 0, 0, 1
            );
            var matrix2 = new Matrix4x4(
                1, 0, 0, -Position.X,
                0, 1, 0, -Position.Y,
                0, 0, 1, -Position.Z,
                0, 0, 0, 1
            );
            return matrix1 * matrix2;
            // return Matrix4x4.CreateLookAt(Position, Target, cameraUp);
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            var sin = (float)Math.Sin(FOV);
            var cotan = (float)Math.Cos(FOV) / sin;
            var clip = FarPlane - NearPlane;
            return new Matrix4x4(
                cotan/AspectRatio,     0,     0,      0,
                0,                 cotan,     0,      0,
                0, 0, -(NearPlane+FarPlane)/clip, -(2f*NearPlane*FarPlane)/clip,
                0,                     0,     -1,     1
            );
        }

        public Vector3 GetRightVector()
        {
            return Vector3.Normalize(Vector3.Cross(Up, Position - Target));
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
        public float Attenuation;

        public PointLight(Vector3 intensity, Vector4 point, float attenuation)
        {
            Intensity = intensity;
            Point = point;
            Attenuation = attenuation;
        }
    }

    public static class PrimitiveForms
    {
        public static void LoadFromObj(string filename, Mesh mesh, Material material)
        {
            ushort id = 0;
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
                    mesh.Vertices.Add(new Vertex(x, y, z, id++));
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

                    for (int i = 2; i < v.Count; ++i)
                    {
                        var polygon = new Polygon(v[0], v[i-1], v[i], material);
                        if (normal != -1)
                            polygon.Normal = new Vector4(normals[normal], 0);
                        mesh.Polygons.Add(polygon);
                    }
                }
            }
            mesh.CalculateVerticesNormals();
            file.Close();
        }

        public static void Prism(int sidesX, int sidesY, float height, float radius, Mesh mesh, Material material)
        {
            ushort id = 0;
            mesh.Vertices.Clear();
            mesh.Polygons.Clear();
            var dh = height / sidesY;
            var dphi = 2 * Math.PI / sidesX;
            height /= 2;
            var rotation = Matrix4x4.CreateRotationY((float) dphi);
            var highCenter = new Vertex(0, height, 0, 0);
            var lowCenter = new Vertex(0, height - dh * sidesY, 0, 0);
            mesh.Vertices.Add(new Vertex(radius, height, 0, id++));
            for (int i = 1; i < sidesX; ++i)
            {
                mesh.Vertices.Add(new Vertex(Vector4.Transform(mesh.Vertices.Last().Point, rotation), id++));
                mesh.Polygons.Add(new Polygon(mesh.Vertices[i-1], mesh.Vertices[i], highCenter, material));
            }
            mesh.Polygons.Add(new Polygon(mesh.Vertices[sidesX-1], mesh.Vertices[0], highCenter, material));

            for (int i = 1; i < sidesY+1; ++i)
            {
                for (int j = 0; j < sidesX; ++j)
                {
                    mesh.Vertices.Add(new Vertex(mesh.Vertices[(i-1)*sidesX + j].Point, id++));
                    mesh.Vertices[^1].Point.Y -= dh;
                }
            }
            
            for (int i = 1; i < sidesY+1; ++i)
            {
                for (int j = 1; j < sidesX; ++j)
                {
                    mesh.Polygons.Add(new Polygon(mesh.Vertices[(i-1)*sidesX + j], mesh.Vertices[(i-1)*sidesX + j-1], mesh.Vertices[i*sidesX + j-1], material));
                    mesh.Polygons.Add(new Polygon(mesh.Vertices[i*sidesX + j-1], mesh.Vertices[i*sidesX + j], mesh.Vertices[(i-1)*sidesX + j], material));
                }
                mesh.Polygons.Add(new Polygon(mesh.Vertices[(i-1)*sidesX], mesh.Vertices[(i-1)*sidesX + sidesX-1], mesh.Vertices[i*sidesX + sidesX-1], material));
                mesh.Polygons.Add(new Polygon(mesh.Vertices[i*sidesX + sidesX-1], mesh.Vertices[i*sidesX], mesh.Vertices[(i-1)*sidesX], material));
            }
            
            for (int i = 1; i < sidesX; ++i)
            {
                mesh.Polygons.Add(new Polygon(mesh.Vertices[^i], mesh.Vertices[^(i+1)], lowCenter, material));
            }
            mesh.Polygons.Add(new Polygon(mesh.Vertices[^sidesX], mesh.Vertices[^1], lowCenter, material));
            highCenter.Id = id++;
            mesh.Vertices.Add(highCenter);
            lowCenter.Id = id;
            mesh.Vertices.Add(lowCenter);
            mesh.CalculateVerticesNormals();
        }

        public static void Cube(Mesh mesh, Material material)
        {
            mesh.Vertices = new List<Vertex>
            {
                new(-.5f, -.5f, -.5f, 0), new(.5f, -.5f, -.5f, 1), new(.5f, .5f, -.5f, 2), new(-.5f, .5f, -.5f, 3),
                new(-.5f, -.5f, .5f, 4), new(.5f, -.5f, .5f, 5), new(.5f, .5f, .5f, 6), new(-.5f, .5f, .5f, 7),
            };
            mesh.Polygons = new List<Polygon>
            {
                new(mesh.Vertices[3], mesh.Vertices[2], mesh.Vertices[1], material),
                new(mesh.Vertices[1], mesh.Vertices[0], mesh.Vertices[3], material),
                new(mesh.Vertices[4], mesh.Vertices[5], mesh.Vertices[6], material),
                new(mesh.Vertices[6], mesh.Vertices[7], mesh.Vertices[4], material),
                new(mesh.Vertices[5], mesh.Vertices[4], mesh.Vertices[0], material),
                new(mesh.Vertices[0], mesh.Vertices[1], mesh.Vertices[5], material),
                new(mesh.Vertices[6], mesh.Vertices[5], mesh.Vertices[1], material),
                new(mesh.Vertices[1], mesh.Vertices[2], mesh.Vertices[6], material),
                new(mesh.Vertices[7], mesh.Vertices[6], mesh.Vertices[2], material),
                new(mesh.Vertices[2], mesh.Vertices[3], mesh.Vertices[7], material),
                new(mesh.Vertices[4], mesh.Vertices[7], mesh.Vertices[3], material),
                new(mesh.Vertices[3], mesh.Vertices[0], mesh.Vertices[4], material),
            };
            mesh.CalculateVerticesNormals();
        }
        
        public static void Octahedron(Mesh mesh, Material material)
        {
            mesh.Vertices = new List<Vertex>
            {
                new(0, 0, -1, 0),
                new(1, 0, 0, 1), new(0, 1, 0, 2),
                new(-1, 0, 0, 3), new(0, -1, 0, 4),
                new(0, 0, 1, 5)
            };
            mesh.Polygons = new List<Polygon>
            {
                new(mesh.Vertices[0], mesh.Vertices[2], mesh.Vertices[1], material),
                new(mesh.Vertices[0], mesh.Vertices[3], mesh.Vertices[2], material),
                new(mesh.Vertices[0], mesh.Vertices[4], mesh.Vertices[3], material),
                new(mesh.Vertices[0], mesh.Vertices[1], mesh.Vertices[4], material),
                new(mesh.Vertices[2], mesh.Vertices[5], mesh.Vertices[1], material),
                new(mesh.Vertices[1], mesh.Vertices[5], mesh.Vertices[4], material),
                new(mesh.Vertices[4], mesh.Vertices[5], mesh.Vertices[3], material),
                new(mesh.Vertices[3], mesh.Vertices[5], mesh.Vertices[2], material)
            };
            mesh.CalculateVerticesNormals();
        }
    }
}
