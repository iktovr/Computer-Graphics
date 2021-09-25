using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        [UI] private ComboBoxText _colors = null;
        [UI] private ComboBoxText _models = null;

        private Matrix4x4 _worldMatrix;
        private Matrix4x4 _viewMatrix;
        private List<Vertex> _vertices;
        private List<Polygon> _polygons;
        
        private Vector2 _pointerPos;
        private int _pointerButton = -1;
        private bool _fillPolygons = true;

        private readonly Cairo.Color BACKGROUND_COLOR = new Cairo.Color(1, 0.98, 0.94);
        private readonly Cairo.Color LINE_COLOR = new Cairo.Color(0, 0, 0);
        
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
            _models.Active = 1;
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
                // В методах Transform вектор - строка, поэтому сдвиг на 4 строке
                _viewMatrix.M41 = args.Allocation.Width / 2f;
                _viewMatrix.M42 = args.Allocation.Height / 2f;
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
                _canvas.QueueDraw();
            };

            _zBuffer.Toggled += (o, args) => { TransformToWorld(); _canvas.QueueDraw(); };
            _wireframe.Toggled += (o, args) => { _canvas.QueueDraw(); };

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

            _models.Changed += (o, args) =>
            {
                if (_models.ActiveText == "Cube")
                    PrimitiveForms.Cube(ref _vertices, ref _polygons);
                else if (_models.ActiveText == "Octahedron")
                    PrimitiveForms.Octahedron(ref _vertices, ref _polygons);
                TransformToWorld();
                _canvas.QueueDraw();
            };
            
            _xAngle.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _yAngle.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _zAngle.ValueChanged += (o, args) => { CalculateWorldMatrix(); };            
            _xScale.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _xScale.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _xScale.ValueChanged += (o, args) => { CalculateWorldMatrix(); };            
            _xShift.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _xShift.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
            _xShift.ValueChanged += (o, args) => { CalculateWorldMatrix(); };
        }

        private void CalculateWorldMatrix()
        {
            _worldMatrix = Matrix4x4.CreateRotationX((float)(_xAngle.Value * Math.PI / 180)) *
                           Matrix4x4.CreateRotationY((float)(_yAngle.Value * Math.PI / 180)) *
                           Matrix4x4.CreateRotationZ((float)(_zAngle.Value * Math.PI / 180));
            
            _worldMatrix.M41 = (float)_xShift.Value;
            _worldMatrix.M42 = (float)_yShift.Value;
            _worldMatrix.M43 = (float)_zShift.Value;
            
            _worldMatrix *= Matrix4x4.CreateScale((float) _xScale.Value, (float) _yScale.Value, (float) _zScale.Value);
            
            TransformToWorld();
            _canvas.QueueDraw();
        }

        private void TransformToWorld()
        {
            foreach (var vertex in _vertices)
            {
                vertex.PointInWorld = Vector4.Transform(vertex.Point, _worldMatrix);
            }

            Matrix4x4.Invert(_worldMatrix, out var normalMatrix);
            normalMatrix = Matrix4x4.Transpose(normalMatrix);
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
            
        }

        private void SetStandardColor()
        {
            foreach (var polygon in _polygons)
            {
                polygon.Color = Polygon.StandardColor;
            }
        }

        private void CanvasDrawnHandler(object o, DrawnArgs args)
        {
            var cr = args.Cr;
            cr.SetSourceColor(BACKGROUND_COLOR);
            cr.Paint();
            
            cr.SetSourceColor(LINE_COLOR);
            var viewDirection = new Vector4(0, 0, 1, 0);
            foreach (var polygon in _polygons)
            {
                if (Vector4.Dot(polygon.NormalInWorld, viewDirection) < 0)
                    continue;
                
                cr.SetSourceColor(LINE_COLOR);
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
            }
        }

        private void CanvasMotionNotifyHandler(object o, MotionNotifyEventArgs args)
        {
            if (_pointerButton == -1) return;
            
            // Left button - rotate
            if (_pointerButton == 1)
            {
                _xAngle.Value = (_xAngle.Value + 360 + (args.Event.Y - _pointerPos.Y) / _canvas.Window.Height * 360) %
                                360;
                _yAngle.Value = (_yAngle.Value + 360 + (args.Event.X - _pointerPos.X) / _canvas.Window.Width * 360) %
                                360;
            }
            // Right button - translate
            else if (_pointerButton == 3)
            {
                _xShift.Value += (args.Event.X - _pointerPos.X) / _xScale.Value;
                _yShift.Value -= (args.Event.Y - _pointerPos.Y) / _yScale.Value;
            }

            _pointerPos.X = (float)args.Event.X;
            _pointerPos.Y = (float)args.Event.Y;
            _canvas.QueueDraw();
        }
    }
}