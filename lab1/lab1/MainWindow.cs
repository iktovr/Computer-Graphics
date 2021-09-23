using System;
using System.Collections.Generic;
using System.Numerics;
using Cairo;
using Gdk;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace lab1
{
    class MainWindow : Window
    {
        [UI] private DrawingArea _canvas = null;
        [UI] private Adjustment _a = null;
        [UI] private Adjustment _angle = null;
        [UI] private Adjustment _stepsCount = null;
        [UI] private Adjustment _shiftX = null;
        [UI] private Adjustment _shiftY = null;
        [UI] private Adjustment _scale = null;
        [UI] private Adjustment _rCenterX = null;
        [UI] private Adjustment _rCenterY = null;
        
        private List<Vector2> _dots;
        private Vector2 _pointerPos;
        private int _pointerButton = -1;
        private Vector2 _canvasSize;

        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
            _dots = new List<Vector2>();
            CalculateDots();
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;

            _canvas.Events |= EventMask.ScrollMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask;
            _canvas.Drawn += CanvasOnDrawn;
            _canvas.SizeAllocated += (o, args) =>
            {
                double maxDistX = 0, maxDistY = 0;
                foreach (var dot in _dots)
                {
                    var tDot = TransformDot(dot, false, false);
                    maxDistX = Math.Max(maxDistX, Math.Abs(tDot.X));
                    maxDistY = Math.Max(maxDistY, Math.Abs(tDot.Y));
                }

                _scale.Value = Math.Min(args.Allocation.Width / maxDistX / 2, args.Allocation.Height / maxDistY / 2);
                _canvasSize.X = args.Allocation.Width;
                _canvasSize.Y = args.Allocation.Height;
                _canvas.QueueDraw();
            };
            
            _canvas.ScrollEvent += (o, args) =>
            {
                if (args.Event.Direction == ScrollDirection.Down)
                {
                    _scale.Value -= _scale.StepIncrement;
                }
                else if (args.Event.Direction == ScrollDirection.Up)
                {
                    _scale.Value += _scale.StepIncrement;
                }
            };
            
            _canvas.ButtonPressEvent += (o, args) =>
            {
                if (_pointerButton != -1) return;
                _pointerButton = (int)args.Event.Button;
                _pointerPos.X = (float)args.Event.X;
                _pointerPos.Y = (float)args.Event.Y;
            };
            
            _canvas.MotionNotifyEvent += (o, args) =>
            {
                if (_pointerButton == -1) return;
                
                if (_pointerButton == 1)
                {
                    _shiftX.Value -= (args.Event.X - _pointerPos.X) / _scale.Value;
                    _shiftY.Value += (args.Event.Y - _pointerPos.Y) / _scale.Value;
                }
                else if (_pointerButton == 3)
                {
                    Vector2 center = TransformDot(new Vector2((float)_rCenterX.Value, (float)_rCenterY.Value), false);
                    Vector2 pos1 = _pointerPos - center;
                    Vector2 pos2 = new Vector2((float)args.Event.X, (float)args.Event.Y) - center;
                    _angle.Value = (360 + _angle.Value - Math.Sign(pos1.X*pos2.Y - pos1.Y*pos2.X) *
                                    Math.Truncate(Math.Acos(Vector2.Dot(pos1, pos2) / pos1.Length() / pos2.Length()) /
                                        Math.PI * 180)) % 360;
                }

                _pointerPos.X = (float)args.Event.X;
                _pointerPos.Y = (float)args.Event.Y;
            };
            
            _canvas.ButtonReleaseEvent += (o, args) =>
            {
                _pointerButton = -1;
            };

            _a.ValueChanged += (_, _) =>
            {
                CalculateDots();
                _canvas.QueueDraw();
            };
            _stepsCount.ValueChanged += (_, _) =>
            {
                CalculateDots();
                _canvas.QueueDraw();
            };
            _scale.ValueChanged += (_, _) => { _canvas.QueueDraw(); };
            _angle.ValueChanged += (_, _) => { _canvas.QueueDraw(); };
            _shiftX.ValueChanged += (_, _) => { _canvas.QueueDraw(); };
            _shiftY.ValueChanged += (_, _) => { _canvas.QueueDraw(); };
            _rCenterX.ValueChanged += (_, _) => { _canvas.QueueDraw(); };
            _rCenterY.ValueChanged += (_, _) => { _canvas.QueueDraw(); };
        }

        private void CalculateDots()
        {
            _dots.Clear();
            
            float start = (float) -_a.Value, end = (float) _a.Value;
            float d = (end - start) / (float) _stepsCount.Value;
            for (float x = start; x <= end; x += d)
            {
                float t = 0.5f * (float) (-Math.Pow(_a.Value, 2) - 2 * x * x +
                           Math.Sqrt(Math.Pow(_a.Value, 4) + 8 * Math.Pow(_a.Value, 2) * x * x));
                if (Math.Abs(t) < 1e-6) t = 0;
                _dots.Add(new Vector2(x, (float) Math.Sqrt(t)));
                _dots.Insert(0, new Vector2(x, (float) -Math.Sqrt(t)));
            }
            _dots.Add(_dots[0]);
        }
        
        private Vector2 TransformDot(Vector2 dot, bool scale = true, bool shiftToCenter = true, bool shiftXY = true, bool rotate = true, bool fixY = true)
        {
            var scaleMatrix = Matrix3x2.Identity * (float)_scale.Value;
            Vector2 center = new Vector2((float)_rCenterX.Value, (float)_rCenterY.Value);
            float cos = (float) Math.Cos(_angle.Value * Math.PI / 180);
            float sin = (float) Math.Sin(_angle.Value * Math.PI / 180);
            var rotationMatrix = new Matrix3x2(cos, sin, -sin, cos, 0, 0);
            var shift = new Vector2((float) -_shiftX.Value, (float) -_shiftY.Value);

            if (rotate) dot = Vector2.Transform(dot - center, rotationMatrix) + center;
            if (shiftXY) dot += shift;
            if (scale) dot = Vector2.Transform(dot, scaleMatrix);
            if (fixY) dot.Y *= -1;
            if (shiftToCenter) dot += _canvasSize * 0.5f;
            return dot;
        }
        
        private void CanvasOnDrawn(object o, DrawnArgs args)
        {
            var cr = args.Cr;
            cr.Antialias = Antialias.Subpixel;
            
            DrawAxis(cr);
            DrawRotationCenter(cr);
            
            cr.SetSourceRGB(0, 0, 0);
            cr.MoveTo(TransformDot(_dots[0]));
            foreach (var dot in _dots)
            {
                cr.LineTo(TransformDot(dot));
            }
            cr.Stroke();
        }

        private void DrawRotationCenter(Context cr)
        {
            Vector2 center = TransformDot(new Vector2((float)_rCenterX.Value, (float)_rCenterY.Value));
            cr.SetSourceRGB(1, 0, 0);
            cr.Arc(center.X, center.Y, 5, 0, 2 * Math.PI);
            cr.Fill();
            cr.Stroke();
        }

        private void DrawAxis(Context cr)
        {
            cr.SetSourceRGB(0.6, 0.6, 0.6);

            var halfX = _canvasSize.X / 2 - _shiftX.Value * _scale.Value;
            var halfY = _canvasSize.Y / 2 + _shiftY.Value * _scale.Value;
            cr.MoveTo(0, halfY);
            cr.LineTo(_canvasSize.X, halfY);
            cr.MoveTo(halfX, 0);
            cr.LineTo(halfX, _canvasSize.Y);
            cr.Stroke();

            double size = 8;
            for (var x = halfX + _scale.Value; x < _canvasSize.X; x += _scale.Value)
            {
                cr.Line(x, halfY + size / 2, x, halfY - size / 2);
            }
            
            for (var x = halfX - _scale.Value; x >= 0 ; x -= _scale.Value)
            {
                cr.Line(x, halfY + size / 2, x, halfY - size / 2);
            }
            
            for (var y = halfY + _scale.Value; y < _canvasSize.Y; y += _scale.Value)
            {
                cr.Line(halfX + size / 2, y, halfX - size / 2, y);
            }
            
            for (var y = halfY - _scale.Value; y >= 0 ; y -= _scale.Value)
            {
                cr.Line(halfX + size / 2, y, halfX - size / 2, y);
            }
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}