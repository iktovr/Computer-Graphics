using System.Numerics;
using Cairo;

namespace Extensions
{
    public static class Extensions
    {
        public static void MoveTo(this Context context, Vector2 point)
        {
            context.MoveTo(point.X, point.Y);
        }
        
        public static void LineTo(this Context context, Vector2 point)
        {
            context.LineTo(point.X, point.Y);
        }
        
        public static void Line(this Context context, Vector2 point1, Vector2 point2)
        {
            context.MoveTo(point1.X, point1.Y);
            context.LineTo(point2.X, point2.Y);
            context.Stroke();
        }
        
        public static void Line(this Context context, double x1, double y1, double x2, double y2)
        {
            context.MoveTo(x1, y1);
            context.LineTo(x2, y2);
            context.Stroke();
        }
    }
}