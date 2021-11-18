using System;
using System.Numerics;
using Gtk;
using Gdk;
using SharpGL;
using UI = Gtk.Builder.ObjectAttribute;

using static Extensions.Extensions;
using Primitives;
using Window = Gtk.Window;

namespace lab4_5
{
    class MainWindow : Window
    {
        [UI] private GLArea _glArea = null;
        [UI] private Adjustment _xAngle = null;
        [UI] private Adjustment _yAngle = null;
        [UI] private Adjustment _zAngle = null;
        [UI] private Adjustment _xScale = null;
        [UI] private Adjustment _yScale = null;
        [UI] private Adjustment _zScale = null;
        [UI] private Adjustment _xShift = null;
        [UI] private Adjustment _yShift = null;
        [UI] private Adjustment _zShift = null;
        [UI] private Adjustment _cameraX = null;
        [UI] private Adjustment _cameraY = null;
        [UI] private Adjustment _cameraZ = null;
        [UI] private Adjustment _cameraTargetX = null;
        [UI] private Adjustment _cameraTargetY = null;
        [UI] private Adjustment _cameraTargetZ = null;
        [UI] private Adjustment _cameraFOV = null;
        [UI] private Adjustment _cameraNearPlane = null;
        [UI] private Adjustment _cameraFarPlane = null;
        [UI] private Button _rollLeft = null;
        [UI] private Button _rollRight = null;
        [UI] private CheckButton _wireframe = null;
        // [UI] private CheckButton _showNormals = null;
        [UI] private ComboBoxText _models = null;
        [UI] private ComboBoxText _shading = null;
        [UI] private Adjustment _sidesX = null;
        [UI] private Adjustment _sidesY = null;
        [UI] private Adjustment _radius = null;
        [UI] private Adjustment _height = null;
        // [UI] private Adjustment _p = null;
        [UI] private Adjustment _materialR = null; [UI] private Adjustment _materialG = null; [UI] private Adjustment _materialB = null;
        // [UI] private Adjustment _kaR = null; [UI] private Adjustment _kaG = null; [UI] private Adjustment _kaB = null;
        // [UI] private Adjustment _kdR = null; [UI] private Adjustment _kdG = null; [UI] private Adjustment _kdB = null;
        // [UI] private Adjustment _ksR = null; [UI] private Adjustment _ksG = null; [UI] private Adjustment _ksB = null;
        // [UI] private Adjustment _ambientR = null; [UI] private Adjustment _ambientG = null; [UI] private Adjustment _ambientB = null;
        // [UI] private Adjustment _pointR = null; [UI] private Adjustment _pointG = null; [UI] private Adjustment _pointB = null;
        // [UI] private Adjustment _lightX = null; [UI] private Adjustment _lightY = null; [UI] private Adjustment _lightZ = null;
        // [UI] private Adjustment _attenuation = null;
        // [UI] private CheckButton _showPointLight = null;

        private Mesh _object;
        private Material _material;
        private Camera _camera;
        // private AmbientLight _ambientLight;
        // private PointLight _pointLight;

        private bool _modelChanged;
        private Vector2 _pointerPos;
        private int _pointerButton = -1;
        private FileChooserDialog _fileChooser;

        private readonly Vector3 BACKGROUND_COLOR = new(0, 0, 0);
        private readonly Vector3 LINE_COLOR = new(1, 0.98f, 0.94f);
        private readonly Vector3 NORMAL_COLOR = new (0, 1, 0);
        
        private enum Model
        {
            Cylinder,
            Cube,
            Octahedron,
            Obj
        }

        private enum Shading
        {
            None,
            Flat,
            Gouraud
        }
        
        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
            _object = new Mesh();
            _camera = new Camera(new Vector3(0, 0, -2), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 1, (float)(45f / 180f * Math.PI), 1, 100);
            _cameraZ.Value = -2;
            _cameraFOV.Value = 45;
            _cameraNearPlane.Value = 1;
            _cameraFarPlane.Value = 100;
            _material = new Material(new Vector3( 1, 0.5f, 0.2f), new Vector3(0.3f, 0.3f, 0.3f), new Vector3(0.7f, 0.7f, 0.7f), new Vector3(0.7f, 0.7f, 0.7f), 1);
            // _ambientLight = new AmbientLight(new Vector3(1, 1, 1));
            // _pointLight = new PointLight(new Vector3(1, 1, 1), new Vector4(0, 1, 2.5f, 1), 0.05f);

            _sidesX.Value = 30;
            _sidesY.Value = 20;
            _height.Value = 2;
            _radius.Value = 1;

            _models.Active = (int) Model.Cylinder;
            _shading.Active = (int)Shading.None;

            // _p.Value = _material.P;
            _materialR.Value = _material.Color.X;
            _materialG.Value = _material.Color.Y;
            _materialB.Value = _material.Color.Z;
            // _kaR.Value = _material.Ka.X;
            // _kaG.Value = _material.Ka.Y;
            // _kaB.Value = _material.Ka.Z;
            // _kdR.Value = _material.Kd.X;
            // _kdG.Value = _material.Kd.Y;
            // _kdB.Value = _material.Kd.Z;
            // _ksR.Value = _material.Ks.X;
            // _ksG.Value = _material.Ks.Y;
            // _ksB.Value = _material.Ks.Z;
            // _ambientR.Value = _ambientLight.Intensity.X;
            // _ambientG.Value = _ambientLight.Intensity.Y;
            // _ambientB.Value = _ambientLight.Intensity.Z;
            // _pointR.Value = _pointLight.Intensity.X;
            // _pointG.Value = _pointLight.Intensity.Y;
            // _pointB.Value = _pointLight.Intensity.Z;
            // _lightX.Value = _pointLight.Point.X;
            // _lightY.Value = _pointLight.Point.Y;
            // _lightZ.Value = _pointLight.Point.Z;
            // _attenuation.Value = _pointLight.Attenuation;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += (o, args) =>
            {
                Application.Quit();
            };

            _glArea.Events |= EventMask.ScrollMask | EventMask.PointerMotionMask | EventMask.ButtonPressMask |
                              EventMask.ButtonReleaseMask;
            _glArea.Realized += GLInit;
            _glArea.SizeAllocated += (o, args) =>
            {
                _camera.AspectRatio = (float) args.Allocation.Width / args.Allocation.Height;
            };
            _glArea.ButtonPressEvent += (o, args) =>
            {
                if (_pointerButton != -1) return;
                _pointerButton = (int)args.Event.Button;
                _pointerPos.X = (float)args.Event.X;
                _pointerPos.Y = (float)args.Event.Y;
            };
            _glArea.ButtonReleaseEvent += (o, args) =>
            {
                _pointerButton = -1;
            };
            _glArea.MotionNotifyEvent += GlAreaMotionNotifyHandler;
            _glArea.ScrollEvent += (o, args) =>
            {
                if (args.Event.Direction == ScrollDirection.Down)
                {
                    _cameraFOV.Value += _cameraFOV.StepIncrement;
                }
                else if (args.Event.Direction == ScrollDirection.Up)
                {
                    _cameraFOV.Value -= _cameraFOV.StepIncrement;
                }
            };

            _cameraX.ValueChanged += (_, _) =>
            {
                if ((_camera.Target - new Vector3((float)_cameraX.Value, _camera.Position.Y, _camera.Position.Z)).Length() < 1e-6f)
                {
                    _cameraX.Value = _camera.Position.X;
                    return;
                }
                var right = _camera.GetRightVector();
                _camera.Position.X = (float) _cameraX.Value;
                _camera.Up = Vector3.Normalize(Vector3.Cross(_camera.Position - _camera.Target, right));
            };
            _cameraY.ValueChanged += (_, _) =>
            {
                if ((_camera.Target - new Vector3(_camera.Position.X, (float)_cameraY.Value, _camera.Position.Z)).Length() < 1e-6f)
                {
                    _cameraY.Value = _camera.Position.Y;
                    return;
                }
                var right = _camera.GetRightVector();
                _camera.Position.Y = (float) _cameraY.Value;
                _camera.Up = Vector3.Normalize(Vector3.Cross(_camera.Position - _camera.Target, right));
            };
            _cameraZ.ValueChanged += (_, _) =>
            {
                if ((_camera.Target - new Vector3(_camera.Position.X, _camera.Position.Y, (float)_cameraZ.Value)).Length() < 1e-6f)
                {
                    _cameraZ.Value = _camera.Position.Z;
                    return;
                }
                var right = _camera.GetRightVector();
                _camera.Position.Z = (float) _cameraZ.Value;
                _camera.Up = Vector3.Normalize(Vector3.Cross(_camera.Position - _camera.Target, right));
            };
            _cameraTargetX.ValueChanged += (_, _) =>
            {
                if ((_camera.Position - new Vector3((float)_cameraTargetX.Value, _camera.Target.Y, _camera.Target.Z)).Length() < 1e-6f)
                {
                    _cameraTargetX.Value = _camera.Target.X;
                    return;
                }
                var right = _camera.GetRightVector();
                _camera.Target.X = (float) _cameraTargetX.Value;
                _camera.Up = Vector3.Normalize(Vector3.Cross(_camera.Position - _camera.Target, right));
            };
            _cameraTargetY.ValueChanged += (_, _) =>
            {
                if ((_camera.Position - new Vector3(_camera.Target.X, (float)_cameraTargetY.Value, _camera.Target.Z)).Length() < 1e-6f)
                {
                    _cameraTargetY.Value = _camera.Target.Y;
                    return;
                }
                var right = _camera.GetRightVector();
                _camera.Target.Y = (float) _cameraTargetY.Value;
                _camera.Up = Vector3.Normalize(Vector3.Cross(_camera.Position - _camera.Target, right));
            };
            _cameraTargetZ.ValueChanged += (_, _) =>
            {
                if ((_camera.Position - new Vector3(_camera.Target.X, _camera.Target.Y, (float)_cameraTargetZ.Value)).Length() < 1e-6f)
                {
                    _cameraTargetZ.Value = _camera.Target.Z;
                    return;
                }
                var right = _camera.GetRightVector();
                _camera.Target.Z = (float) _cameraTargetZ.Value;
                _camera.Up = Vector3.Normalize(Vector3.Cross(_camera.Position - _camera.Target, right));
            };
            _cameraFOV.ValueChanged += (_, _) => { _camera.FOV = (float) (_cameraFOV.Value / 180 * Math.PI); };
            _cameraNearPlane.ValueChanged += (_, _) => { _camera.NearPlane = (float) _cameraNearPlane.Value; };
            _cameraFarPlane.ValueChanged += (_, _) => { _camera.FarPlane = (float) _cameraFarPlane.Value; };

            _rollLeft.Clicked += (_, _) =>
            {
                _camera.Up = Vector3.Normalize(Vector3.Transform(_camera.Up,
                    Matrix4x4.CreateFromAxisAngle(_camera.Position - _camera.Target, (float) Math.PI / 180)));
            };
            _rollRight.Clicked += (_, _) =>
            {
                _camera.Up = Vector3.Normalize(Vector3.Transform(_camera.Up,
                    Matrix4x4.CreateFromAxisAngle(_camera.Position - _camera.Target, (float) -Math.PI / 180)));
            };
            
            _xAngle.ValueChanged += (_, _) => { _object.Rotation.X = (float) (_xAngle.Value / 180 * Math.PI); };
            _yAngle.ValueChanged += (_, _) => { _object.Rotation.Y = (float) (_yAngle.Value / 180 * Math.PI); };
            _zAngle.ValueChanged += (_, _) => { _object.Rotation.Z = (float) (_zAngle.Value / 180 * Math.PI); };
            _xScale.ValueChanged += (_, _) => { _object.Scale.X = (float) _xScale.Value; };
            _yScale.ValueChanged += (_, _) => { _object.Scale.Y = (float) _yScale.Value; };
            _zScale.ValueChanged += (_, _) => { _object.Scale.Z = (float) _zScale.Value; };
            _xShift.ValueChanged += (_, _) => { _object.Origin.X = (float) _xShift.Value; };
            _yShift.ValueChanged += (_, _) => { _object.Origin.Y = (float) _yShift.Value; };
            _zShift.ValueChanged += (_, _) => { _object.Origin.Z = (float) _zShift.Value; };
            
            _fileChooser = new FileChooserDialog("Choose .obj file", (Window) this.Toplevel, 
                FileChooserAction.Open, 
                "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            var filter = new FileFilter();
            filter.Name = ".obj";
            filter.AddPattern("*.obj");
            _fileChooser.AddFilter(filter);
            _fileChooser.SetCurrentFolder(".");
            
            _models.RemoveAll();
            _models.Append(Model.Cylinder.ToString(), "Cylinder");
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
                        PrimitiveForms.LoadFromObj(_fileChooser.Filename, _object, _material);
                    }
                    // Destroy ломает программу
                    _fileChooser.Hide();
                    _models.Active = -1;
                }
                else if (_models.Active == (int)Model.Cylinder)
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                else if (_models.Active == (int)Model.Cube)
                    PrimitiveForms.Cube(_object, _material);
                else if (_models.Active == (int)Model.Octahedron)
                    PrimitiveForms.Octahedron(_object, _material);
            
                _modelChanged = true;
            };

            _sidesX.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    _modelChanged = true;
                }
            };
            _sidesY.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    _modelChanged = true;
                }
            };
            _radius.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    _modelChanged = true;
                }
            };
            _height.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    _modelChanged = true;
                }
            };
            
            _shading.RemoveAll();
            _shading.Append(Shading.None.ToString(), "None");
            // _shading.Append(Shading.Flat.ToString(), "Flat");
            // _shading.Append(Shading.Gouraud.ToString(), "Gouraud");

            // _p.ValueChanged += (_, _) => { _material.P = (float) _p.Value; };
            _materialR.ValueChanged += (_, _) => { _material.Color.X = (float) _materialR.Value; };
            _materialG.ValueChanged += (_, _) => { _material.Color.Y = (float) _materialG.Value; };
            _materialB.ValueChanged += (_, _) => { _material.Color.Z = (float) _materialB.Value; };
            // _kaR.ValueChanged += (_, _) => { _material.Ka.X = (float) _kaR.Value; };
            // _kaG.ValueChanged += (_, _) => { _material.Ka.Y = (float) _kaG.Value; };
            // _kaB.ValueChanged += (_, _) => { _material.Ka.Z = (float) _kaB.Value; };
            // _kdR.ValueChanged += (_, _) => { _material.Kd.X = (float) _kdR.Value; };
            // _kdG.ValueChanged += (_, _) => { _material.Kd.Y = (float) _kdG.Value; };
            // _kdB.ValueChanged += (_, _) => { _material.Kd.Z = (float) _kdB.Value; };
            // _ksR.ValueChanged += (_, _) => { _material.Ks.X = (float) _ksR.Value; };
            // _ksG.ValueChanged += (_, _) => { _material.Ks.Y = (float) _ksG.Value; };
            // _ksB.ValueChanged += (_, _) => { _material.Ks.Z = (float) _ksB.Value; };
            // _ambientR.ValueChanged += (_, _) => { _ambientLight.Intensity.X = (float) _ambientR.Value; };
            // _ambientG.ValueChanged += (_, _) => { _ambientLight.Intensity.Y = (float) _ambientG.Value; };
            // _ambientB.ValueChanged += (_, _) => { _ambientLight.Intensity.Z = (float) _ambientB.Value; };
            // _pointR.ValueChanged += (_, _) => { _pointLight.Intensity.X = (float) _pointR.Value; };
            // _pointG.ValueChanged += (_, _) => { _pointLight.Intensity.Y = (float) _pointG.Value; };
            // _pointB.ValueChanged += (_, _) => { _pointLight.Intensity.Z = (float) _pointB.Value; };
            // _lightX.ValueChanged += (_, _) => { _pointLight.Point.X = (float) _lightX.Value; };
            // _lightY.ValueChanged += (_, _) => { _pointLight.Point.Y = (float) _lightY.Value; };
            // _lightZ.ValueChanged += (_, _) => { _pointLight.Point.Z = (float) _lightZ.Value; };
            // _attenuation.ValueChanged += (_, _) => { _pointLight.Attenuation = (float) _attenuation.Value; };
        }

        private void GLInit(object sender, EventArgs args)
        {
            var glArea = sender as GLArea;
            var gl = new OpenGL();
            glArea.MakeCurrent();
            
            var frame_clock = glArea.Context.Window.FrameClock;
            frame_clock.Update += (_, _) => glArea.QueueRender();
            frame_clock.BeginUpdating();

            var vertices = Array.Empty<float>();
            var elements = Array.Empty<ushort>();

            uint[] buffers = new uint[2];
            uint[] arrays = new uint[1];
            gl.GenBuffers(2, buffers);
            uint vbo = buffers[0], vio = buffers[1];
            gl.GenVertexArrays(1, arrays);
            uint vao = arrays[0];
            gl.BindVertexArray(vao);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, vio);
            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, true, 0, IntPtr.Zero);
            gl.EnableVertexAttribArray(0);
            gl.BindVertexArray(0);

            uint vertexShader = gl.CreateShader(OpenGL.GL_VERTEX_SHADER);
            gl.ShaderSource(vertexShader, ReadFromRes("lab4_5.shader.vert"));
            gl.CompileShader(vertexShader);
            uint fragmentShader = gl.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
            gl.ShaderSource(fragmentShader, ReadFromRes("lab4_5.shader.frag"));
            gl.CompileShader(fragmentShader);
            uint shaderProgram = gl.CreateProgram();
            gl.AttachShader(shaderProgram, vertexShader);
            gl.AttachShader(shaderProgram, fragmentShader);
            gl.LinkProgram(shaderProgram);
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);
            
            gl.FrontFace(OpenGL.GL_CCW);
            
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.DepthFunc(OpenGL.GL_LESS);
            
            gl.Enable(OpenGL.GL_BLEND);
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
            
            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.CullFace(OpenGL.GL_BACK);
            
            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
            
            gl.ClearColor(BACKGROUND_COLOR.X, BACKGROUND_COLOR.Y, BACKGROUND_COLOR.Z, 1.0f);

            glArea.Render += (_, _) =>
            {
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                gl.UseProgram(shaderProgram);

                var projMatrix = _camera.GetProjectionMatrix();
                int projLoc = gl.GetUniformLocation(shaderProgram, "proj");
                gl.UniformMatrix4(projLoc, 1, false, projMatrix.ToArray());
                var viewMatrix = _camera.GetViewMatrix();
                // autoscale
                viewMatrix = Matrix4x4.CreateScale(Math.Min(_camera.AspectRatio, 1), Math.Min(_camera.AspectRatio, 1), 1) * viewMatrix;
                int viewLoc = gl.GetUniformLocation(shaderProgram, "view");
                gl.UniformMatrix4(viewLoc, 1, false, viewMatrix.ToArray());
                var modelMatrix = _object.GetModelMatrix();
                int modelLoc = gl.GetUniformLocation(shaderProgram, "model");
                gl.UniformMatrix4(modelLoc, 1, false, modelMatrix.ToArray());
                int colorLoc = gl.GetUniformLocation(shaderProgram, "inColor");

                gl.BindVertexArray(vao);
                
                if (_modelChanged)
                {
                    _modelChanged = false;
                    _object.ToArray(true, false, false, out vertices, out elements);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER, vertices, OpenGL.GL_DYNAMIC_DRAW);
                    gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, elements, OpenGL.GL_DYNAMIC_DRAW);
                }
                
                gl.Uniform4(colorLoc, _material.Color.X, _material.Color.Y, _material.Color.Z, 1);
                gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_FILL);
                gl.DrawElements(OpenGL.GL_TRIANGLES, elements.Length, OpenGL.GL_UNSIGNED_SHORT, IntPtr.Zero);

                if (_wireframe.Active)
                {
                    gl.Uniform4(colorLoc, LINE_COLOR.X, LINE_COLOR.Y, LINE_COLOR.Z, 1);
                    gl.LineWidth(2);
                    gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                    gl.DrawElements(OpenGL.GL_TRIANGLES, elements.Length, OpenGL.GL_UNSIGNED_SHORT, IntPtr.Zero);
                }
                
                gl.BindVertexArray(0);
            };
            
            glArea.Unrealized += (_, _) => {
                // gl.DeleteBuffers(buffers.Length, buffers);
                // gl.DeleteVertexArrays(arrays.Length, arrays);
                // gl.DeleteProgram(shaderProgram);
            };
        }

        private void GlAreaMotionNotifyHandler(object o, MotionNotifyEventArgs args)
        {
            if (_pointerButton == -1) return;
            
            // Left button - rotate camera
            if (_pointerButton == 1)
            {
                var right = _camera.GetRightVector();
                var rotation = Matrix4x4.CreateFromAxisAngle(_camera.Up, -0.02f * (float) (args.Event.X - _pointerPos.X)) * 
                               Matrix4x4.CreateFromAxisAngle(right, -0.02f * (float) (args.Event.Y - _pointerPos.Y));
                var newPosition = Vector3.Transform(_camera.Position - _camera.Target, rotation);
                _camera.Position = Vector3.Normalize(newPosition) * (_camera.Position - _camera.Target).Length() + _camera.Target;
                
                _cameraX.Value = _camera.Position.X;
                _cameraY.Value = _camera.Position.Y;
                _cameraZ.Value = _camera.Position.Z;
            }
            // Right button - translate camera target
            else if (_pointerButton == 3)
            {
                var right = _camera.GetRightVector();
                _camera.Target += _camera.Up * 0.02f * (float) (args.Event.Y - _pointerPos.Y) +
                                  right * -0.02f * (float) (args.Event.X - _pointerPos.X);
                
                _cameraTargetX.Value = _camera.Target.X;
                _cameraTargetY.Value = _camera.Target.Y;
                _cameraTargetZ.Value = _camera.Target.Z;
            }
            // Middle button - translate light point
            // else if (_pointerButton == 2)
            // {
            //     
            // }
            
            _pointerPos.X = (float)args.Event.X;
            _pointerPos.Y = (float)args.Event.Y;
        }
    }
}