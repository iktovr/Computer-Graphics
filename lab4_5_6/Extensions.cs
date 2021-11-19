using System.Reflection;
using System.IO;
using System.Numerics;

namespace Extensions
{
    public static class Extensions
    {
        public static string ReadFromRes(string name) {
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(name))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static float[] ToArray(this Vector3 vec)
        {
            return new float[] {vec.X, vec.Y, vec.Z};
        }

        public static float[] ToArray(this Matrix4x4 m)
        {
            return new float[]
            {
                m.M11, m.M21, m.M31, m.M41,
                m.M12, m.M22, m.M32, m.M42,
                m.M13, m.M23, m.M33, m.M43,
                m.M14, m.M24, m.M34, m.M44
            };
        }
        
        private static Matrix4x4 TransposeInvert(this Matrix4x4 m)
        {
            return new Matrix4x4(
                m.M33 * m.M22 - m.M23 * m.M32, 
                m.M23 * m.M31 - m.M21 * m.M33, 
                m.M21 * m.M32 - m.M31 * m.M22, 0,
                m.M13 * m.M32 - m.M33 * m.M12,
                m.M33 * m.M11 - m.M13 * m.M31,
                m.M31 * m.M12 - m.M11 * m.M32, 0,
                m.M23 * m.M12 - m.M13 * m.M22,
                m.M21 * m.M13 - m.M23 * m.M11,
                m.M11 * m.M22 - m.M21 * m.M12, 0,
                0,   0,   0,   0);
        }
    }
}