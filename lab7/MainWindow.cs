using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Gtk;
using Gdk;
using SharpGL;
using UI = Gtk.Builder.ObjectAttribute;

using Primitives;
using Window = Gtk.Window;

namespace lab7
{
    class MainWindow : Window
    {
        [UI] private GLArea _glArea = null;
        [UI] private CheckButton _drawPoints = null;
        [UI] private CheckButton _drawTangents = null;
        [UI] private Adjustment _approximation = null;
        [UI] private Adjustment _tangentFactor = null;

        private CubicSpline _spline;
        
        private bool _modelChanged;
        private Vector2 _pointerPos;
        private Vector2 _pointerPressPos;
        private int _pointId = -1;
        private int _pointerButton = -1;

        private readonly Vector3 BackgroundColor = new(1, 0.98f, 0.94f);
        private readonly Vector3 LineColor = new(0, 0, 0);
        private readonly Vector3 PointColor = new(0, 0, 1);
        private readonly Vector3 TangentColor = new(1, 0, 0);

        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
            _spline = new CubicSpline(new Vector2(-0.2f, 0), new Vector2(0.2f, 0),
                new Vector2(0, 0.5f), new Vector2(0, -0.5f), (float)_tangentFactor.Value);
            _modelChanged = true;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);
            
            DeleteEvent += (_, _) =>
            {
                Application.Quit();
            };
            _glArea.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask |
                              EventMask.ButtonReleaseMask;
            _glArea.Realized += GlAreaInit;
            _glArea.ButtonPressEvent += GlAreaButtonPressHandler;
            _glArea.ButtonReleaseEvent += GlAreaButtonReleaseHandler;
            _glArea.MotionNotifyEvent += GlAreaMotionNotifyHandler;

            _approximation.ValueChanged += (_, _) =>
            {
                _modelChanged = true;
            };
            _tangentFactor.ValueChanged += (_, _) =>
            {
                _modelChanged = true;
                _spline.TangentFactor = (float) _tangentFactor.Value;
            };
        }

        private void GlAreaInit(object sender, EventArgs e)
        {
            var glArea = sender as GLArea;
            var gl = new OpenGL();
            glArea.MakeCurrent();
            
            var frame_clock = glArea.Context.Window.FrameClock;
            frame_clock.Update += (_, _) => glArea.QueueRender();
            frame_clock.BeginUpdating();

            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.DepthFunc(OpenGL.GL_ALWAYS);
            
            gl.Enable(OpenGL.GL_BLEND);
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
            
            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
            
            gl.PointSize(9);
            gl.LineWidth(3);
            
            gl.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1);
            
            var vertices = Array.Empty<float>();

            Shader baseShader = new Shader(gl, "lab7.base.vert", "lab7.base.frag");
            gl.UseProgram(baseShader.Id);
            uint positionLoc = (uint) gl.GetAttribLocation(baseShader.Id, "position");
            gl.UseProgram(0);

            uint[] buffers = new uint[2];
            uint[] arrays = new uint[2];
            gl.GenBuffers(3, buffers);
            uint splineVbo = buffers[0], pointsVbo = buffers[1];
            gl.GenVertexArrays(2, arrays);
            uint splineVao = arrays[0], pointsVao = arrays[1];

            for (int i = 0; i < 2; ++i)
            {
                gl.BindVertexArray(arrays[i]);
                gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, buffers[i]);
                gl.VertexAttribPointer(positionLoc, 2, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
                gl.EnableVertexAttribArray(positionLoc);
            }
            gl.BindVertexArray(0);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);

            glArea.Render += (_, _) =>
            {
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                if (_modelChanged)
                {
                    _modelChanged = false;
                    List<float> verts = new();
                    float dt = 1f / (float)_approximation.Value;
                    for (int i = 0; i < _spline.Points.Count - 1; ++i)
                    {
                        for (float t = 0; t < 1; t += dt)
                        {
                            Vector2 value = _spline.GetValue(i, t);
                            verts.Add(value.X);
                            verts.Add(value.Y);
                        }
                    }
                    verts.Add(_spline.Points.Last().X);
                    verts.Add(_spline.Points.Last().Y);
                    vertices = verts.ToArray();
                    
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, splineVbo);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER, vertices, OpenGL.GL_DYNAMIC_DRAW);
                    
                    float[] points = new float[_spline.Points.Count * 6];
                    for (int i = 0; i < _spline.Points.Count; ++i)
                    {
                        points[i * 2] = _spline.Points[i].X;
                        points[i * 2 + 1] = _spline.Points[i].Y;
                    }

                    for (int i = 0; i < _spline.Points.Count; ++i)
                    {
                        points[_spline.Points.Count*2 + i * 4] = _spline.Points[i].X + _spline.Derivatives[i].X;
                        points[_spline.Points.Count*2 + i * 4 + 1] = _spline.Points[i].Y + _spline.Derivatives[i].Y;
                        points[_spline.Points.Count*2 + i * 4 + 2] = _spline.Points[i].X - _spline.Derivatives[i].X;
                        points[_spline.Points.Count*2 + i * 4 + 3] = _spline.Points[i].Y - _spline.Derivatives[i].Y;
                    }
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, pointsVbo);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER, points, OpenGL.GL_DYNAMIC_DRAW);
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
                }
                
                gl.UseProgram(baseShader.Id);
                
                gl.BindVertexArray(splineVao);
                baseShader.SetVec3(gl, "color", LineColor);
                gl.DrawArrays(OpenGL.GL_LINE_STRIP, 0, vertices.Length / 2);

                if (_drawTangents.Active)
                {
                    gl.BindVertexArray(pointsVao);
                    baseShader.SetVec3(gl, "color", TangentColor);
                    gl.DrawArrays(OpenGL.GL_LINES, _spline.Points.Count, _spline.Points.Count * 2);
                    baseShader.SetVec3(gl, "color", PointColor);
                    gl.DrawArrays(OpenGL.GL_POINTS, _spline.Points.Count, _spline.Points.Count * 2);
                }

                if (_drawPoints.Active)
                {
                    gl.BindVertexArray(pointsVao);
                    baseShader.SetVec3(gl, "color", PointColor);
                    gl.DrawArrays(OpenGL.GL_POINTS, 0, _spline.Points.Count);
                }
                
                gl.BindVertexArray(0);
                gl.UseProgram(0);
            };
            
            glArea.Unrealized += (_, _) => {
                // gl.DeleteBuffers(buffers.Length, buffers);
                // gl.DeleteVertexArrays(arrays.Length, arrays);
                // gl.DeleteProgram(shaderProgram);
            };
        }

        private void GlAreaButtonPressHandler(object o, ButtonPressEventArgs args)
        {
            if (_pointerButton != -1) return;
            _pointerButton = (int)args.Event.Button;
            _pointerPos.X = (float)args.Event.X / _glArea.AllocatedWidth * 2 - 1;
            _pointerPos.Y = 1 - (float)args.Event.Y / _glArea.AllocatedHeight * 2;

            float epsilon = (float)Math.Sqrt(Math.Pow(10f / _glArea.AllocatedWidth, 2) +
                                      Math.Pow(10f / _glArea.AllocatedHeight, 2));

            int id = _spline.FindPoint(_pointerPos, epsilon, _drawPoints.Active, _drawTangents.Active);
            // right button on empty space - add
            if (id == -1)
            {
                if (_pointerButton == 3)
                {
                    _modelChanged = true;
                    _spline.AddPoint(_pointerPos);
                }
            }
            // right button on point - remove
            else if (_pointerButton == 3)
            {
                _modelChanged = true;
                _spline.RemovePoint(id);
            }
            // left button on point - move
            else
            {
                _pointId = id;
            }
        }
        
        private void GlAreaButtonReleaseHandler(object o, ButtonReleaseEventArgs args)
        {
            _pointerButton = -1;
            _pointId = -1;
        }

        private void GlAreaMotionNotifyHandler(object o, MotionNotifyEventArgs args)
        {
            if (_pointId == -1) { return; }

            _modelChanged = true;
            Vector2 newPointerPos = new Vector2((float) args.Event.X / _glArea.AllocatedWidth * 2 - 1,
                1 - (float) args.Event.Y / _glArea.AllocatedHeight * 2);
            
            // control point
            if (_pointId < _spline.Points.Count)
            {
                _spline.Points[_pointId] += newPointerPos - _pointerPos;
            }
            // derivative point
            else if (_pointId < _spline.Points.Count * 2)
            {
                _spline.Derivatives[_pointId - _spline.Points.Count] += newPointerPos - _pointerPos;
            }
            // additional derivative point
            else
            {
                _spline.Derivatives[_pointId - _spline.Points.Count * 2] -= newPointerPos - _pointerPos;
            }

            _pointerPos = newPointerPos;
        }
    }
}
