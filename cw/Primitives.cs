using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Globalization;
using System.Linq;
using System.Text;
using SharpGL;
using static Extensions.Extensions;

namespace Primitives
{

    public class NurbsSurface4x4
    {
        public Vector3[,] Points;
        public float[,] Weights;
        public Vector3[] ClipSpacePoints;

        public NurbsSurface4x4()
        {
            Points = new Vector3[4, 4]
            {
                { new(-3, 4, -3), new(-3, 0, -1), new(-3, 0, 1), new(-3, 4, 3) },
                { new(-1, 0, -3), new(-1, 0, -1), new(-1, 0, 1), new(-1, 0, 3) },
                { new(1, 0, -3), new(1, 0, -1), new(1, 0, 1), new(1, 0, 3) },
                { new(3, 4, -3), new(3, 0, -1), new(3, 0, 1), new(3, 4, 3) },
            };
            Weights = new float[4, 4]
            {
                { 5, 2, 2, 5 },
                { 2, 1, 1, 2 },
                { 2, 1, 1, 2 },
                { 5, 2, 2, 5 }
            };
            ClipSpacePoints = new Vector3[16];
        }

        public int FindPoint(Vector2 point)
        {
            int id = -1;
            for (int i = 0; i < 16; ++i)
            {
                double length = Math.Sqrt(Math.Pow(ClipSpacePoints[i].X - point.X, 2) +
                                          Math.Pow(ClipSpacePoints[i].Y - point.Y, 2));
                if (length < 3e-2 && Math.Abs(ClipSpacePoints[i].Z) <= 1 && 
                    (id < 0 || ClipSpacePoints[i].Z < ClipSpacePoints[id].Z))
                {
                    id = i;
                }
            }

            return id;
        }

        public static float GetCoefficientPolynom(int i, float t)
        {
            return i switch
            {
                0 => (1 - t) * (1 - t) * (1 - t) / 6,
                1 => ((3 * t - 6) * t * t + 4) / 6,
                2 => (((-3 * t + 3) * t + 3) * t + 1) / 6,
                3 => t * t * t / 6,
                _ => 0
            };
        }

        public Vector3 GetValue(float u, float v)
        {
            Vector3 f = Vector3.Zero;
            float g = 0;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    float coeff = GetCoefficientPolynom(i, u) * GetCoefficientPolynom(j, v);
                    f += coeff * Points[i, j] * Weights[i, j];
                    g += coeff * Weights[i, j];
                }
            }

            return f / g;
        }

        public void GenerateMesh(ref Mesh mesh, int uCount, int vCount, Material material)
        {
            mesh.Vertices.Clear();
            mesh.Polygons.Clear();
            uint vertexId = 0;
            
            float du = 1f / uCount, dv = 1f / vCount, u = 0, v;
            Vector3 value;
            for (int i = 0; i < uCount; ++i)
            {
                v = 0;
                for (int j = 0; j < vCount; ++j)
                {
                    value = GetValue(u, v);
                    mesh.Vertices.Add(new Vertex(value.X, value.Y, value.Z, vertexId++));
                    v += dv;
                }
                value = GetValue(u, 1);
                mesh.Vertices.Add(new Vertex(value.X, value.Y, value.Z, vertexId++));
                u += du;
            }
            v = 0;
            for (int j = 0; j < vCount; ++j)
            {
                value = GetValue(1, v);
                mesh.Vertices.Add(new Vertex(value.X, value.Y, value.Z, vertexId++));
                v += dv;
            }
            value = GetValue(1, 1);
            mesh.Vertices.Add(new Vertex(value.X, value.Y, value.Z, vertexId));

            for (int i = 0; i < uCount; ++i)
            {
                for (int j = 0; j < vCount; ++j)
                {
                    mesh.Polygons.Add(new Polygon(mesh.Vertices[i * (vCount + 1) + j], mesh.Vertices[i * (vCount + 1) + j + 1], mesh.Vertices[(i+1) * (vCount + 1) + j], material));
                    mesh.Polygons.Add(new Polygon(mesh.Vertices[(i+1) * (vCount + 1) + j], mesh.Vertices[i * (vCount + 1) + j + 1], mesh.Vertices[(i+1) * (vCount + 1) + j + 1], material));
                }
            }
            mesh.CalculateVerticesNormals();
        }

        public void CalculateClipSpacePoints(Matrix4x4 viewMatrix, Matrix4x4 projMatrix)
        {
            Matrix4x4 transformMatrix = Matrix4x4.Transpose(projMatrix * viewMatrix);
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    Vector4 point = Vector4.Transform(new Vector4(Points[i, j], 1), transformMatrix);
                    ClipSpacePoints[i * 4 + j] = new Vector3(point.X / point.W, point.Y / point.W, point.Z / point.W);
                }
            }
        }

        public float[] ToArray()
        {
            float[] array = new float[48];
            int k = 0;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    array[k++] = Points[i, j].X;
                    array[k++] = Points[i, j].Y;
                    array[k++] = Points[i, j].Z;
                }
            }
            return array;
        }

        public void LoadFromFile(string filename)
        {
            StreamReader file = new StreamReader(filename);
            string line;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    line = file.ReadLine();
                    string[] numbers = line.Split(" ");
                    Points[i, j].X = float.Parse(numbers[0], CultureInfo.InvariantCulture);
                    Points[i, j].Y = float.Parse(numbers[1], CultureInfo.InvariantCulture);
                    Points[i, j].Z = float.Parse(numbers[2], CultureInfo.InvariantCulture);
                    Weights[i, j] = float.Parse(numbers[3], CultureInfo.InvariantCulture);
                }
            }

            file.Close();
        }

        public void SaveToFile(string filename)
        {
            StreamWriter file = new StreamWriter(filename);
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    file.WriteLine("{0} {1} {2} {3}", 
                        Points[i, j].X.ToString("F2", CultureInfo.InvariantCulture), 
                        Points[i, j].Y.ToString("F2", CultureInfo.InvariantCulture), 
                        Points[i, j].Z.ToString("F2", CultureInfo.InvariantCulture), 
                        Weights[i, j].ToString("F2", CultureInfo.InvariantCulture));
                }
            }
            file.Close();
        }
    }
    
    public class Vertex
    {
        public Vector4 Point;
        public uint Id;

        public List<Polygon> Polygons;
        public Vector4 Normal;

        public Vertex(float x, float y, float z, uint id)
        {
            Point = new Vector4(x, y, z, 1);
            Polygons = new List<Polygon>();
            Id = id;
        }
        
        public Vertex(Vector4 point, uint id)
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

        public void ToArray(bool coords, bool colors, bool normals, out float[] vertices, out uint[] elements)
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
                    vertices[i] = vertex.Polygons[0].Material.Color.X;
                    vertices[i + 1] = vertex.Polygons[0].Material.Color.Y;
                    vertices[i + 2] = vertex.Polygons[0].Material.Color.Z;
                    i += 3;
                }
                if (normals)
                {
                    vertices[i] = vertex.Normal.X;
                    vertices[i + 1] = vertex.Normal.Y;
                    vertices[i + 2] = vertex.Normal.Z;
                    i += 3;
                }
            }

            int elementsCount = Polygons.Sum(a => a.Vertices.Length);
            elements = new uint[elementsCount];
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
        public Vector3 Point;
        public Vector3 Intensity;
        public float Attenuation;

        public PointLight(Vector3 intensity, Vector3 point, float attenuation)
        {
            Intensity = intensity;
            Point = point;
            Attenuation = attenuation;
        }
    }

    public class Shader
    {
        public readonly uint Id;

        public Shader(OpenGL gl, string vert, string frag, string geom = null)
        {
            var tmp = new int[1];
            var txt = new StringBuilder(512);
            Id = gl.CreateProgram();
            uint vertId = gl.CreateShader(OpenGL.GL_VERTEX_SHADER);
            gl.ShaderSource(vertId, ReadFromRes(vert));
            gl.CompileShader(vertId);
            gl.GetShaderInfoLog(vertId, 512, IntPtr.Zero, txt);
            gl.GetShader(vertId, OpenGL.GL_COMPILE_STATUS, tmp);
            if (tmp[0] != OpenGL.GL_TRUE) Debug.WriteLine(txt);
            Debug.Assert(tmp[0] == OpenGL.GL_TRUE, "Vertex shader compilation failed");
            gl.AttachShader(Id, vertId);

            if (geom != null)
            {
                uint geomId = gl.CreateShader(OpenGL.GL_GEOMETRY_SHADER);
                gl.ShaderSource(geomId, ReadFromRes(geom));
                gl.CompileShader(geomId);
                gl.GetShaderInfoLog(vertId, 512, IntPtr.Zero, txt);
                gl.GetShader(geomId, OpenGL.GL_COMPILE_STATUS, tmp);
                if (tmp[0] != OpenGL.GL_TRUE) Debug.WriteLine(txt);
                Debug.Assert(tmp[0] == OpenGL.GL_TRUE, "Geometrical Ssader compilation failed");
                gl.AttachShader(Id, geomId);
            }
            
            uint fragId = gl.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
            gl.ShaderSource(fragId, ReadFromRes(frag));
            gl.CompileShader(fragId);
            gl.GetShaderInfoLog(vertId, 512, IntPtr.Zero, txt);
            gl.GetShader(fragId, OpenGL.GL_COMPILE_STATUS, tmp);
            if (tmp[0] != OpenGL.GL_TRUE) Debug.WriteLine(txt);
            Debug.Assert(tmp[0] == OpenGL.GL_TRUE, "Fragment shader compilation failed");
            gl.AttachShader(Id, fragId);
            gl.LinkProgram(Id);
            
            gl.GetProgram(Id, OpenGL.GL_LINK_STATUS, tmp);
            Debug.Assert(tmp[0] == OpenGL.GL_TRUE, "Shader program link failed");
        }

        public void SetMatrix4(OpenGL gl, string name, Matrix4x4 matrix)
        {
            int location = gl.GetUniformLocation(Id, name);
            gl.UniformMatrix4(location , 1, false, matrix.ToArray());
        }
        
        public void SetVec3(OpenGL gl, string name, Vector3 vec)
        {
            int location = gl.GetUniformLocation(Id, name);
            gl.Uniform3(location, vec.X, vec.Y, vec.Z);
        }

        public void SetFloat(OpenGL gl, string name, float value)
        {
            int location = gl.GetUniformLocation(Id, name);
            gl.Uniform1(location, value);
        }
        
        public void SetInt(OpenGL gl, string name, int value)
        {
            int location = gl.GetUniformLocation(Id, name);
            gl.Uniform1(location, value);
        }
        
        public void SetUint(OpenGL gl, string name, uint value)
        {
            int location = gl.GetUniformLocation(Id, name);
            gl.Uniform1(location, value);
        }
    }
}
