using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cairo;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

using Extensions;
using Gdk;
using Primitives;
using Window = Gtk.Window;

namespace lab2
{
    class MainWindow : Window
    {
        [UI] private DrawingArea _canvas = null;
        [UI] private Adjustment _xAngle = null;
        [UI] private Adjustment _yAngle = null;
        [UI] private Adjustment _zAngle = null;
        [UI] private Adjustment _xScale = null;
        [UI] private Adjustment _yScale = null;
        [UI] private Adjustment _zScale = null;
        [UI] private Adjustment _xShift = null;
        [UI] private Adjustment _yShift = null;
        [UI] private Adjustment _zShift = null;
        [UI] private CheckButton _zBuffer = null;
        [UI] private CheckButton _wireframe = null;
        [UI] private CheckButton _hideInvisible = null;
        [UI] private CheckButton _showNormals = null;
        [UI] private ComboBoxText _colors = null;
        [UI] private ComboBoxText _models = null;
        [UI] private ComboBoxText _projections = null;

        private Matrix4x4 _worldMatrix;
        private Matrix4x4 _viewMatrix;
        private List<Vertex> _vertices;
        private List<Polygon> _polygons;
        
        private Vector2 _pointerPos;
        private int _pointerButton = -1;
        private bool _fillPolygons = true;
        private FileChooserDialog _fileChooser;

        private readonly Cairo.Color BACKGROUND_COLOR = new Cairo.Color(1, 0.98, 0.94);
        private readonly Cairo.Color LINE_COLOR = new Cairo.Color(0, 0, 0);
        private readonly Cairo.Color NORMAL_COLOR = new Cairo.Color(0, 1, 0);

        private enum Projection
        {
            None,
            Front,
            Top,
            Right,
            Isometric,
            Dimetric
        }
        private enum Model
        {
            Cube,
            Octahedron,
            Obj
        }
        
        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
            _viewMatrix = new Matrix4x4(
                1, 0, 0, 0,
                0, -1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
            _vertices = new List<Vertex>();
            _polygons = new List<Polygon>();
            
            _colors.Active = 1;
            _models.Active = (int)Model.Octahedron;
            _projections.Active = (int)Projection.None;
            CalculateWorldMatrix();
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += (o, args) =>
            {
                Application.Quit();
            };

            _canvas.Events |= EventMask.ScrollMask | EventMask.PointerMotionMask | EventMask.ButtonPressMask |
                              EventMask.ButtonReleaseMask;
            _canvas.Drawn += CanvasDrawnHandler;
            _canvas.SizeAllocated += (o, args) =>
            {
                float maxDistX = 0, maxDistY = 0;
                foreach (var vertex in _vertices)
                {
                    maxDistX = Math.Max(maxDistX, Math.Abs(vertex.PointInWorld.X));
                    maxDistY = Math.Max(maxDistY, Math.Abs(vertex.PointInWorld.Y));
                }

                var scale = Math.Min(args.Allocation.Width / maxDistX / 4, args.Allocation.Height / maxDistY / 4);
                _viewMatrix = Matrix4x4.Identity;
                _viewMatrix.M22 *= -1;
                _viewMatrix *= scale;

                // В методах Transform вектор - строка, поэтому сдвиг на 4 строке
                _viewMatrix.M41 = args.Allocation.Width / 2f;
                _viewMatrix.M42 = args.Allocation.Height / 2f;
                _canvas.QueueDraw();
            };
            _canvas.ButtonPressEvent += (o, args) =>
            {
                if (_pointerButton != -1) return;
                _pointerButton = (int)args.Event.Button;
                _pointerPos.X = (float)args.Event.X;
                _pointerPos.Y = (float)args.Event.Y;
            };
            _canvas.ButtonReleaseEvent += (o, args) =>
            {
                _pointerButton = -1;
            };
            _canvas.MotionNotifyEvent += CanvasMotionNotifyHandler;
            _canvas.ScrollEvent += (o, args) =>
            {
                if (args.Event.Direction == ScrollDirection.Down)
                {
                    _xScale.Value -= _xScale.StepIncrement;
                    _yScale.Value -= _yScale.StepIncrement;
                    _zScale.Value -= _zScale.StepIncrement;
                }
                else if (args.Event.Direction == ScrollDirection.Up)
                {
                    _xScale.Value += _xScale.StepIncrement;
                    _yScale.Value += _yScale.StepIncrement;
                    _zScale.Value += _zScale.StepIncrement;
                }
                CalculateWorldMatrix();
                _canvas.QueueDraw();
            };

            _zBuffer.Toggled += (o, args) => { TransformToWorld(); _canvas.QueueDraw(); };
            _wireframe.Toggled += (o, args) => { _canvas.QueueDraw(); };
            _hideInvisible.Toggled += (o, args) => { _canvas.QueueDraw(); };
            _showNormals.Toggled += (o, args) => { _canvas.QueueDraw(); };

            _colors.Changed += (o, args) =>
            {
                // No colors
                if (_colors.Active == 0)
                    _fillPolygons = false;
                // Solid
                else if (_colors.Active == 1)
                {
                    _fillPolygons = true;
                    SetStandardColor();
                }
                // Random colors
                else if (_colors.Active == 2)
                {
                    _fillPolygons = true;
                    SetRandomColors();
                }
                _canvas.QueueDraw();
            };
            
            _fileChooser = new FileChooserDialog("Choose .obj file", (Window) this.Toplevel, 
                FileChooserAction.Open, 
                "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            var filter = new FileFilter();
            filter.Name = ".obj";
            filter.AddPattern("*.obj");
            _fileChooser.AddFilter(filter);
            _fileChooser.SetCurrentFolder(".");
            
            _models.RemoveAll();
            _models.Append(Model.Cube.ToString(), "Cube");
            _models.Append(Model.Octahedron.ToString(), "Octahedron");
            _models.Append(Model.Obj.ToString(), "Load .obj");
            _models.Changed += (o, args) =>
            {
                if (_models.Active == -1)
                    return;
                
                if (_models.Active == (int)Model.Obj)
                {
                    ResponseType response = (ResponseType) _fileChooser.Run();
                    if (response == ResponseType.Accept)
                    {
                        PrimitiveForms.LoadFromObj(_fileChooser.Filename, ref _vertices, ref _polygons);
                        TransformToWorld();
                        _canvas.QueueDraw();
                    }
                    // Destroy ломает программу
                    _fileChooser.Hide();
                    _models.Active = -1;
                }
                else if (_models.Active == (int)Model.Cube)
                    PrimitiveForms.Cube(ref _vertices, ref _polygons);
                else if (_models.Active == (int)Model.Octahedron)
                    PrimitiveForms.Octahedron(ref _vertices, ref _polygons);
            
                if (_colors.Active == 1)
                    SetStandardColor();
                else if (_colors.Active == 2)
                    SetRandomColors();
                TransformToWorld();
                _canvas.QueueDraw();
            };
            
            _projections.RemoveAll();
            _projections.Append(Projection.None.ToString(), "None");
            _projections.Append(Projection.Front.ToString(), "Front view");
            _projections.Append(Projection.Top.ToString(), "Top view");
            _projections.Append(Projection.Right.ToString(), "Right view");
            _projections.Append(Projection.Isometric.ToString(), "Isometric");
            _projections.Append(Projection.Dimetric.ToString(), "Dimetric");
            _projections.Changed += (o, args) => { CalculateWorldMatrix(); };
            
            _xAngle.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _yAngle.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _zAngle.ValueChanged += (o, args) => { CalculateWorldMatrix(); };            
            _xScale.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _yScale.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _zScale.ValueChanged += (o, args) => { CalculateWorldMatrix(); };            
            _xShift.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _yShift.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _zShift.ValueChanged += (o, args) => { CalculateWorldMatrix(); };

            CreateMatrixView();
        }

        private void CalculateWorldMatrix()
        {
            Matrix4x4 rotation;
            if (_projections.Active == (int)Projection.Isometric)
            {
                rotation = Matrix4x4.CreateRotationY((float) (45 * Math.PI / 180)) *
                           Matrix4x4.CreateRotationX((float) (35 * Math.PI / 180));
            }
            else if (_projections.Active == (int)Projection.Dimetric)
            {
                rotation = Matrix4x4.CreateRotationY((float) (26 * Math.PI / 180)) *
                           Matrix4x4.CreateRotationX((float) (30 * Math.PI / 180));
            }
            else
            {
                rotation = Matrix4x4.CreateRotationX((float)(_xAngle.Value * Math.PI / 180)) *
                           Matrix4x4.CreateRotationY((float)(_yAngle.Value * Math.PI / 180)) *
                           Matrix4x4.CreateRotationZ((float)(_zAngle.Value * Math.PI / 180));
            }

            var translation = Matrix4x4.CreateTranslation((float) _xShift.Value, (float) _yShift.Value, (float) _zShift.Value);
            var scale = Matrix4x4.CreateScale((float) _xScale.Value, (float) _yScale.Value, (float) _zScale.Value);

            var projectionMatrix = Matrix4x4.Identity;
            if (_projections.Active == (int)Projection.Front)
                projectionMatrix.M33 = 0;
            else if (_projections.Active == (int)Projection.Top)
                projectionMatrix.M22 = 0;
            else if (_projections.Active == (int)Projection.Right)
                projectionMatrix.M11 = 0;

            _worldMatrix = projectionMatrix * scale * rotation * translation;
            
            UpdateMatrixView();
            TransformToWorld();
            _canvas.QueueDraw();
        }

        private static Matrix4x4 TransposeInvert(Matrix4x4 m)
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

        private void TransformToWorld()
        {
            foreach (var vertex in _vertices)
            {
                vertex.PointInWorld = Vector4.Transform(vertex.Point, _worldMatrix);
            }

            var normalMatrix = TransposeInvert(_worldMatrix);
            foreach (var polygon in _polygons)
            {
                polygon.NormalInWorld = Vector4.Transform(polygon.Normal, normalMatrix);
            }
            
            if (_zBuffer.Active)
                _polygons = _polygons.OrderBy((p) => p.Vertices.Select((v) => v.PointInWorld.Z).Max()).ToList();
        }

        private Vector2 TransformToView(Vector4 point)
        {
            Vector4 dot = Vector4.Transform(point, _viewMatrix);
            return new Vector2(dot.X, dot.Y);
        }

        private void SetRandomColors()
        {
            Random gen = new((int) DateTime.Now.Ticks & 0x0000FFFF);
            foreach (var polygon in _polygons)
            {
                HSV.ToRgb(gen.NextDouble(), 1, 1, out var r, out var g, out var b);
                polygon.Color = new Vector3((float)r, (float)g, (float)b);
            }
        }

        private void SetStandardColor()
        {
            foreach (var polygon in _polygons)
            {
                polygon.Color = Polygon.StandardColor;
            }
        }
            
        private readonly Vector4 _viewDirection = new Vector4(0, 0, -1, 0);
        private const int NormalLength = 20;
        
        private void CanvasDrawnHandler(object o, DrawnArgs args)
        {
            var cr = args.Cr;
            cr.Antialias = Antialias.Subpixel;
            cr.LineJoin = LineJoin.Bevel; // чинит острые концы у линий при маленьком увеличении
            
            cr.SetSourceColor(BACKGROUND_COLOR);
            cr.Paint();
            
            cr.SetSourceColor(LINE_COLOR);
            foreach (var polygon in _polygons)
            {
                if (Vector4.Dot(polygon.NormalInWorld, _viewDirection) > 0 && _hideInvisible.Active)
                    continue;
                
                cr.MoveTo(TransformToView(polygon.Vertices[0].PointInWorld));
                for (int i = 1; i < polygon.Vertices.Length; ++i)
                {
                    cr.LineTo(TransformToView(polygon.Vertices[i].PointInWorld));
                }
                cr.ClosePath();
                cr.Save();
                if (_fillPolygons)
                {
                    cr.SetSourceRGB(polygon.Color.X, polygon.Color.Y, polygon.Color.Z);
                    cr.FillPreserve();
                }
                cr.Restore();
                if (_wireframe.Active)
                    cr.Stroke();
                else
                    cr.NewPath();

                if (_showNormals.Active)
                {
                    cr.Save();
                    cr.SetSourceColor(NORMAL_COLOR);
                    Vector4 center = Vector4.Zero;
                    foreach (var vertex in polygon.Vertices)
                    {
                        center += vertex.PointInWorld;
                    }

                    center /= polygon.Vertices.Length;
                    cr.MoveTo(TransformToView(center));
                    var viewNormal = TransformToView(polygon.NormalInWorld);
                    if (viewNormal.Length() > NormalLength)
                    {
                        viewNormal = Vector2.Normalize(viewNormal) * NormalLength;
                    }
                    cr.RelLineTo(viewNormal);
                    cr.Stroke();
                    cr.Restore();
                }
            }
        }

        // private static void MatrixToAngles(Matrix4x4 matrix, out double x, out double y, out double z)
        // {
        //     x = Math.Atan2(matrix.M21, matrix.M11) / Math.PI * 180;
        //     y = Math.Atan2(-matrix.M31, Math.Sqrt(1 - matrix.M31 * matrix.M31)) / Math.PI * 180;
        //     z = Math.Atan2(matrix.M32, matrix.M33) / Math.PI * 180;
        //     // x = Math.Atan2(-matrix.M23, matrix.M33) / Math.PI * 180;
        //     // y = Math.Atan2(matrix.M13, Math.Sqrt(1 - matrix.M13 * matrix.M13)) / Math.PI * 180;
        //     // z = Math.Atan2(-matrix.M12, matrix.M11) / Math.PI * 180;
        // }

        private void CanvasMotionNotifyHandler(object o, MotionNotifyEventArgs args)
        {
            if (_pointerButton == -1) return;
            
            // Left button - rotate
            if (_pointerButton == 1 && _projections.Active < (int)Projection.Isometric)
            {
                _xAngle.Value = (_xAngle.Value + 360 - (args.Event.Y - _pointerPos.Y) / _canvas.Window.Height * 360) % 360;
                _yAngle.Value = (_yAngle.Value + 360 + (args.Event.X - _pointerPos.X) / _canvas.Window.Width * 360) % 360;
                
                // var currentRotation = Matrix4x4.CreateRotationX((float)(_xAngle.Value * Math.PI / 180)) * 
                //                       Matrix4x4.CreateRotationY((float)(_yAngle.Value * Math.PI / 180)) * 
                //                       Matrix4x4.CreateRotationZ((float)(_zAngle.Value * Math.PI / 180));
                //
                // Vector3 axis = new((float) (args.Event.X - _pointerPos.X), (float) (args.Event.Y - _pointerPos.Y), 0);
                // float angle = (float)(axis.Length() / 180 * Math.PI);
                // axis = Vector3.Normalize(new Vector3(-axis.Y, -axis.X, 0));
                // var rotation = Matrix4x4.CreateFromAxisAngle(axis, angle);
                // currentRotation *= rotation;
                // MatrixToAngles(currentRotation, out var x, out var y, out var z);
                // _xAngle.Value = x;
                // _yAngle.Value = y;
                // _zAngle.Value = z;
                // CalculateWorldMatrix();
            }
            // Right button - translate
            else if (_pointerButton == 3)
            {
                _xShift.Value += (args.Event.X - _pointerPos.X) / _viewMatrix.M11;
                _yShift.Value -= (args.Event.Y - _pointerPos.Y) / _viewMatrix.M11;
            }

            _pointerPos.X = (float)args.Event.X;
            _pointerPos.Y = (float)args.Event.Y;
            _canvas.QueueDraw();
        }

        [UI] private Adjustment _M11; [UI] private Adjustment _M12; [UI] private Adjustment _M13; [UI] private Adjustment _M14;
        [UI] private Adjustment _M21; [UI] private Adjustment _M22; [UI] private Adjustment _M23; [UI] private Adjustment _M24;
        [UI] private Adjustment _M31; [UI] private Adjustment _M32; [UI] private Adjustment _M33; [UI] private Adjustment _M34;
        [UI] private Adjustment _M41; [UI] private Adjustment _M42; [UI] private Adjustment _M43; [UI] private Adjustment _M44;
        
        private void CreateMatrixView()
        {
            _M11.ValueChanged += (o, args) => {_worldMatrix.M11 = (float)_M11.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M12.ValueChanged += (o, args) => {_worldMatrix.M12 = (float)_M12.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M13.ValueChanged += (o, args) => {_worldMatrix.M13 = (float)_M13.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M14.ValueChanged += (o, args) => {_worldMatrix.M14 = (float)_M14.Value; TransformToWorld(); _canvas.QueueDraw();};
            _M21.ValueChanged += (o, args) => {_worldMatrix.M21 = (float)_M21.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M22.ValueChanged += (o, args) => {_worldMatrix.M22 = (float)_M22.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M23.ValueChanged += (o, args) => {_worldMatrix.M23 = (float)_M23.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M24.ValueChanged += (o, args) => {_worldMatrix.M24 = (float)_M24.Value; TransformToWorld(); _canvas.QueueDraw();};
            _M31.ValueChanged += (o, args) => {_worldMatrix.M31 = (float)_M31.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M32.ValueChanged += (o, args) => {_worldMatrix.M32 = (float)_M32.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M33.ValueChanged += (o, args) => {_worldMatrix.M33 = (float)_M33.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M34.ValueChanged += (o, args) => {_worldMatrix.M34 = (float)_M34.Value; TransformToWorld(); _canvas.QueueDraw();};
            _M41.ValueChanged += (o, args) => {_worldMatrix.M41 = (float)_M41.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M42.ValueChanged += (o, args) => {_worldMatrix.M42 = (float)_M42.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M43.ValueChanged += (o, args) => {_worldMatrix.M43 = (float)_M43.Value; TransformToWorld(); _canvas.QueueDraw();}; 
            _M44.ValueChanged += (o, args) => {_worldMatrix.M44 = (float)_M44.Value; TransformToWorld(); _canvas.QueueDraw();};
        }

        private void UpdateMatrixView()
        {
            _M11.Value = _worldMatrix.M11; _M12.Value = _worldMatrix.M12; _M13.Value = _worldMatrix.M13; _M14.Value = _worldMatrix.M14;
            _M21.Value = _worldMatrix.M21; _M22.Value = _worldMatrix.M22; _M23.Value = _worldMatrix.M23; _M24.Value = _worldMatrix.M24;
            _M31.Value = _worldMatrix.M31; _M32.Value = _worldMatrix.M32; _M33.Value = _worldMatrix.M33; _M34.Value = _worldMatrix.M34;
            _M41.Value = _worldMatrix.M41; _M42.Value = _worldMatrix.M42; _M43.Value = _worldMatrix.M43; _M44.Value = _worldMatrix.M44;
        }
    }
}