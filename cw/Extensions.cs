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
    }
}