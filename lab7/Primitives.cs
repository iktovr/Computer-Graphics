using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System.Text;
using SharpGL;
using static Extensions.Extensions;

namespace Primitives
{
    public class CubicSpline
    {
        public List<Vector2> Points;
        public List<Vector2> Derivatives;
        public float TangentFactor;

        public CubicSpline(Vector2 a, Vector2 b, Vector2 da, Vector2 db, float tangentFactor)
        {
            Points = new List<Vector2> {a, b};
            Derivatives = new List<Vector2> {da, db};
            TangentFactor = tangentFactor;
        }

        public Vector2 GetValue(int i, float t)
        {
            Vector2 a = Points[i + 1] - Points[i] - TangentFactor * Derivatives[i];
            Vector2 b = 2 * (Points[i] - Points[i + 1]) + TangentFactor * Derivatives[i] + TangentFactor * Derivatives[i + 1];
            return Points[i] + t * (TangentFactor * Derivatives[i] + t * (a + b * (t - 1)));;
        }

        public int FindPoint(Vector2 point, float epsilon, bool points, bool derivatives)
        {
            if (derivatives)
            {
                for (int i = 0; i < Derivatives.Count; ++i)
                {
                    if ((Points[i] + Derivatives[i] - point).Length() < epsilon)
                    {
                        return i + Points.Count;
                    }
                    
                    if ((Points[i] - Derivatives[i] - point).Length() < epsilon)
                    {
                        return i + Points.Count + Points.Count;
                    }
                }
            }

            if (points)
            {
                for (int i = 0; i < Points.Count; ++i)
                {
                    if ((Points[i] - point).Length() < epsilon)
                    {
                        return i;
                    }
                }
            }
            
            return -1;
        }

        public void AddPoint(Vector2 point)
        {
            Vector2 derivative = Vector2.Normalize(point - Points.Last()) / 4;
            Derivatives.Add(derivative);
            Points.Add(point);
        }

        public void RemovePoint(int index)
        {
            if (Points.Count == 2 || index >= Points.Count)
            {
                return;
            }
            
            Points.RemoveAt(index);
            Derivatives.RemoveAt(index);
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
