using System;
using System.Numerics;
using Gtk;
using Gdk;
using SharpGL;
using UI = Gtk.Builder.ObjectAttribute;

using static Extensions.Extensions;
using Primitives;
using Window = Gtk.Window;

namespace cw
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
        [UI] private CheckButton _drawNormals = null;
        [UI] private CheckButton _fillPolygons = null;
        [UI] private CheckButton _animation = null;
        [UI] private CheckButton _drawAxies = null;
        [UI] private ComboBoxText _shading = null;
        [UI] private CheckButton _drawPoints = null;
        [UI] private CheckButton _drawSurface = null;
        [UI] private CheckButton _snapping = null;
        [UI] private Adjustment _gridSize = null;
        [UI] private Adjustment _uCount = null;
        [UI] private Adjustment _vCount = null;
        [UI] private Adjustment _p = null;
        [UI] private Adjustment _materialR = null; [UI] private Adjustment _materialG = null; [UI] private Adjustment _materialB = null;
        [UI] private Adjustment _kaR = null; [UI] private Adjustment _kaG = null; [UI] private Adjustment _kaB = null;
        [UI] private Adjustment _kdR = null; [UI] private Adjustment _kdG = null; [UI] private Adjustment _kdB = null;
        [UI] private Adjustment _ksR = null; [UI] private Adjustment _ksG = null; [UI] private Adjustment _ksB = null;
        [UI] private Adjustment _ambientR = null; [UI] private Adjustment _ambientG = null; [UI] private Adjustment _ambientB = null;
        [UI] private Adjustment _pointR = null; [UI] private Adjustment _pointG = null; [UI] private Adjustment _pointB = null;
        [UI] private Adjustment _lightX = null; [UI] private Adjustment _lightY = null; [UI] private Adjustment _lightZ = null;
        [UI] private Adjustment _attenuation = null;
        [UI] private CheckButton _showPointLight = null;
        [UI] private Button _loadPreset = null;
        [UI] private Button _savePreset = null;
        private FileChooserDialog _loadPresetDialog;
        private FileChooserDialog _savePresetDialog;
        [UI] private Grid _weightsGrid = null;
        private Adjustment[,] _weightsAdjs;
        [UI] private Grid _pointsGrid = null;
        private Adjustment[,,] _pointsAdjs;

        private Mesh _object;
        private NurbsSurface4x4 _surface;
        private Material _material;
        private Camera _camera;
        private AmbientLight _ambientLight;
        private PointLight _pointLight;

        private bool _modelChanged = true;
        private bool _lightPosChanged = true;
        private bool _clipSpaceChanged = true;
        private uint _startTime;
        private Vector2 _pointerPos;
        private int _pointerButton = -1;
        private int _pointId = -1;
        private Vector3 _movement;

        private readonly Vector3 BackgroundColor = new(0, 0, 0);
        private readonly Vector3 LineColor = new(1, 0.98f, 0.94f);
        private readonly Vector3 NormalColor = new (0, 1, 0);

        private enum Shading
        {
            None,
            Gouraud,
            Phong,
            BlinnPhong
        }
        
        public MainWindow() : this(new Builder("MainWindow.glade"))
        {
            _object = new Mesh();
            _surface = new NurbsSurface4x4();
            _camera = new Camera(new Vector3(0, 5, 10), new Vector3(0, 0, 0), new Vector3(0, 1, 0), 1, (float)(45f / 180f * Math.PI), 1, 100);
            _cameraZ.Value = 2;
            _cameraFOV.Value = 45;
            _cameraNearPlane.Value = 1;
            _cameraFarPlane.Value = 100;
            _material = new Material(new Vector3( 1, 0.5f, 0.2f), new Vector3(0.3f, 0.3f, 0.3f), new Vector3(0.7f, 0.7f, 0.7f), new Vector3(0.7f, 0.7f, 0.7f), 10);
            _ambientLight = new AmbientLight(new Vector3(1, 1, 1));
            _pointLight = new PointLight(new Vector3(1, 1, 1), new Vector3(0, 3, 0), 0.05f);

            _shading.Active = (int) Shading.Phong;

            _p.Value = _material.P;
            _materialR.Value = _material.Color.X;
            _materialG.Value = _material.Color.Y;
            _materialB.Value = _material.Color.Z;
            _kaR.Value = _material.Ka.X;
            _kaG.Value = _material.Ka.Y;
            _kaB.Value = _material.Ka.Z;
            _kdR.Value = _material.Kd.X;
            _kdG.Value = _material.Kd.Y;
            _kdB.Value = _material.Kd.Z;
            _ksR.Value = _material.Ks.X;
            _ksG.Value = _material.Ks.Y;
            _ksB.Value = _material.Ks.Z;
            _ambientR.Value = _ambientLight.Intensity.X;
            _ambientG.Value = _ambientLight.Intensity.Y;
            _ambientB.Value = _ambientLight.Intensity.Z;
            _pointR.Value = _pointLight.Intensity.X;
            _pointG.Value = _pointLight.Intensity.Y;
            _pointB.Value = _pointLight.Intensity.Z;
            _lightX.Value = _pointLight.Point.X;
            _lightY.Value = _pointLight.Point.Y;
            _lightZ.Value = _pointLight.Point.Z;
            _attenuation.Value = _pointLight.Attenuation;

            SetWeightsControls();
            SetPointsControls();
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += (o, args) =>
            {
                Application.Quit();
            };
            
            #region GLArea events
            _glArea.Events |= EventMask.ScrollMask | EventMask.PointerMotionMask | EventMask.ButtonPressMask |
                              EventMask.ButtonReleaseMask;
            _glArea.Realized += GlInit;
            _glArea.SizeAllocated += (o, args) =>
            {
                _camera.AspectRatio = (float) args.Allocation.Width / args.Allocation.Height;
            };
            _glArea.ButtonPressEvent += GlAreaButtonPressHandler;
            _glArea.ButtonReleaseEvent += GlAreaButtonReleaseHandler;
            _glArea.MotionNotifyEvent += GlAreaMotionNotifyHandler;
            _glArea.ScrollEvent += GlAreaScrollHandler;
            #endregion
            
            #region Camera controls
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
                _clipSpaceChanged = true;
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
                _clipSpaceChanged = true;
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
                _clipSpaceChanged = true;
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
                _clipSpaceChanged = true;
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
                _clipSpaceChanged = true;
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
                _clipSpaceChanged = true;
            };
            _cameraFOV.ValueChanged += (_, _) =>
            {
                _camera.FOV = (float) (_cameraFOV.Value / 180 * Math.PI);
                _clipSpaceChanged = true;
            };
            _cameraNearPlane.ValueChanged += (_, _) =>
            {
                _camera.NearPlane = (float) _cameraNearPlane.Value;
                _clipSpaceChanged = true;
            };
            _cameraFarPlane.ValueChanged += (_, _) =>
            {
                _camera.FarPlane = (float) _cameraFarPlane.Value;
                _clipSpaceChanged = true;
            };

            _rollLeft.Clicked += (_, _) =>
            {
                _camera.Up = Vector3.Normalize(Vector3.Transform(_camera.Up,
                    Matrix4x4.CreateFromAxisAngle(_camera.Position - _camera.Target, (float) Math.PI / 180)));
                _clipSpaceChanged = true;
            };
            _rollRight.Clicked += (_, _) =>
            {
                _camera.Up = Vector3.Normalize(Vector3.Transform(_camera.Up,
                    Matrix4x4.CreateFromAxisAngle(_camera.Position - _camera.Target, (float) -Math.PI / 180)));
                _clipSpaceChanged = true;
            };
            #endregion

            #region Object controls
            _xAngle.ValueChanged += (_, _) => { _object.Rotation.X = (float) (_xAngle.Value / 180 * Math.PI); _clipSpaceChanged = true; };
            _yAngle.ValueChanged += (_, _) => { _object.Rotation.Y = (float) (_yAngle.Value / 180 * Math.PI); _clipSpaceChanged = true; };
            _zAngle.ValueChanged += (_, _) => { _object.Rotation.Z = (float) (_zAngle.Value / 180 * Math.PI); _clipSpaceChanged = true; };
            _xScale.ValueChanged += (_, _) => { _object.Scale.X = (float) _xScale.Value; _clipSpaceChanged = true; };
            _yScale.ValueChanged += (_, _) => { _object.Scale.Y = (float) _yScale.Value; _clipSpaceChanged = true; };
            _zScale.ValueChanged += (_, _) => { _object.Scale.Z = (float) _zScale.Value; _clipSpaceChanged = true; };
            _xShift.ValueChanged += (_, _) => { _object.Origin.X = (float) _xShift.Value; _clipSpaceChanged = true; };
            _yShift.ValueChanged += (_, _) => { _object.Origin.Y = (float) _yShift.Value; _clipSpaceChanged = true; };
            _zShift.ValueChanged += (_, _) => { _object.Origin.Z = (float) _zShift.Value; _clipSpaceChanged = true; };
            #endregion
            
            #region Material and light controls
            _p.ValueChanged += (_, _) => { _material.P = (float) _p.Value; };
            _materialR.ValueChanged += (_, _) => { _material.Color.X = (float) _materialR.Value; _modelChanged = true; };
            _materialG.ValueChanged += (_, _) => { _material.Color.Y = (float) _materialG.Value; _modelChanged = true; };
            _materialB.ValueChanged += (_, _) => { _material.Color.Z = (float) _materialB.Value; _modelChanged = true; };
            _kaR.ValueChanged += (_, _) => { _material.Ka.X = (float) _kaR.Value; };
            _kaG.ValueChanged += (_, _) => { _material.Ka.Y = (float) _kaG.Value; };
            _kaB.ValueChanged += (_, _) => { _material.Ka.Z = (float) _kaB.Value; };
            _kdR.ValueChanged += (_, _) => { _material.Kd.X = (float) _kdR.Value; };
            _kdG.ValueChanged += (_, _) => { _material.Kd.Y = (float) _kdG.Value; };
            _kdB.ValueChanged += (_, _) => { _material.Kd.Z = (float) _kdB.Value; };
            _ksR.ValueChanged += (_, _) => { _material.Ks.X = (float) _ksR.Value; };
            _ksG.ValueChanged += (_, _) => { _material.Ks.Y = (float) _ksG.Value; };
            _ksB.ValueChanged += (_, _) => { _material.Ks.Z = (float) _ksB.Value; };
            _ambientR.ValueChanged += (_, _) => { _ambientLight.Intensity.X = (float) _ambientR.Value; };
            _ambientG.ValueChanged += (_, _) => { _ambientLight.Intensity.Y = (float) _ambientG.Value; };
            _ambientB.ValueChanged += (_, _) => { _ambientLight.Intensity.Z = (float) _ambientB.Value; };
            _pointR.ValueChanged += (_, _) => { _pointLight.Intensity.X = (float) _pointR.Value; };
            _pointG.ValueChanged += (_, _) => { _pointLight.Intensity.Y = (float) _pointG.Value; };
            _pointB.ValueChanged += (_, _) => { _pointLight.Intensity.Z = (float) _pointB.Value; };
            _lightX.ValueChanged += (_, _) => { _pointLight.Point.X = (float) _lightX.Value; _lightPosChanged = true; };
            _lightY.ValueChanged += (_, _) => { _pointLight.Point.Y = (float) _lightY.Value; _lightPosChanged = true; };
            _lightZ.ValueChanged += (_, _) => { _pointLight.Point.Z = (float) _lightZ.Value; _lightPosChanged = true; };
            _attenuation.ValueChanged += (_, _) => { _pointLight.Attenuation = (float) _attenuation.Value; };
            #endregion
            
            #region Other controls
            _uCount.ValueChanged += (_, _) => _modelChanged = true;
            _vCount.ValueChanged += (_, _) => _modelChanged = true;

            _snapping.Activated += (_, _) => _movement = Vector3.Zero;
            _gridSize.ValueChanged += (_, _) => _movement = Vector3.Zero;

            _shading.RemoveAll();
            _shading.Append(Shading.None.ToString(), "None");
            _shading.Append(Shading.Gouraud.ToString(), "Gouraud");
            _shading.Append(Shading.Phong.ToString(), "Phong");
            _shading.Append(Shading.BlinnPhong.ToString(), "Blinn-Phong");
            
            var filter = new FileFilter();
            filter.Name = "NURBS";
            filter.AddPattern("*.nurbs");
            _loadPresetDialog = new FileChooserDialog("Choose preset", (Window) this.Toplevel, 
                FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
            _loadPresetDialog.AddFilter(filter);
            _savePresetDialog = new FileChooserDialog("Save preset", (Window) this.Toplevel, 
                FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
            _savePresetDialog.AddFilter(filter);
            
            _loadPreset.Clicked += (_, _) =>
            {
                _loadPresetDialog.SetCurrentFolder(".");
                ResponseType response = (ResponseType) _loadPresetDialog.Run();
                if (response == ResponseType.Accept)
                {
                    _surface.LoadFromFile(_loadPresetDialog.Filename);
                    for (int i = 0; i < 4; ++i)
                    {
                        for (int j = 0; j < 4; ++j)
                        {
                            _pointsAdjs[i, j, 0].Value = _surface.Points[i, j].X;
                            _pointsAdjs[i, j, 1].Value = _surface.Points[i, j].Y;
                            _pointsAdjs[i, j, 2].Value = _surface.Points[i, j].Z;
                            _weightsAdjs[i, j].Value = _surface.Weights[i, j];
                        }
                    }
                    _modelChanged = true;
                }
                _loadPresetDialog.Hide();
            };
            _savePreset.Clicked += (_, _) =>
            {
                _savePresetDialog.SetCurrentFolder(".");
                ResponseType response = (ResponseType) _savePresetDialog.Run();
                if (response == ResponseType.Accept)
                {
                    _surface.SaveToFile(_savePresetDialog.Filename);
                }
                _savePresetDialog.Hide();
            };
            #endregion

            var provider = new CssProvider();
            provider.LoadFromData(ReadFromRes("cw.MainWindow.css"));
            StyleContext.AddProviderForScreen(Screen.Default, provider, StyleProviderPriority.User);
        }

        private void GlInit(object sender, EventArgs args)
        {
            var glArea = sender as GLArea;
            var gl = new OpenGL();
            glArea.MakeCurrent();
            
            var frame_clock = glArea.Context.Window.FrameClock;
            frame_clock.Update += (_, _) => glArea.QueueRender();
            frame_clock.BeginUpdating();

            gl.FrontFace(OpenGL.GL_CCW);
            
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.DepthFunc(OpenGL.GL_LESS);
            
            gl.Enable(OpenGL.GL_BLEND);
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
            
            var vertices = Array.Empty<float>();
            var elements = Array.Empty<uint>();

            uint[] buffers = new uint[7];
            uint[] arrays = new uint[4];
            gl.GenBuffers(buffers.Length, buffers);
            gl.GenVertexArrays(arrays.Length, arrays);
            
            uint meshVbo = buffers[0], meshVio = buffers[1];
            uint meshVao = arrays[0];
            gl.BindVertexArray(meshVao);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, meshVbo);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, meshVio);
            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 9 * sizeof(float), IntPtr.Zero);
            gl.VertexAttribPointer(1, 3, OpenGL.GL_FLOAT, false, 9 * sizeof(float), (IntPtr)(3 * sizeof(float)));
            gl.VertexAttribPointer(2, 3, OpenGL.GL_FLOAT, false, 9 * sizeof(float), (IntPtr)(6 * sizeof(float)));
            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            gl.EnableVertexAttribArray(2);

            uint lightVao = arrays[1];
            uint lightVbo = buffers[2];
            gl.BindVertexArray(lightVao);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, lightVbo);
            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
            gl.EnableVertexAttribArray(0);

            uint surfaceVao = arrays[2];
            uint pointsVbo = buffers[3];
            uint pointsColorsVbo = buffers[4];
            uint surfaceVio = buffers[5];
            gl.BindVertexArray(surfaceVao);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, pointsVbo);
            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
            gl.EnableVertexAttribArray(0);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, pointsColorsVbo);
            gl.VertexAttribPointer(1, 3, OpenGL.GL_FLOAT, false, 0, IntPtr.Zero);
            gl.EnableVertexAttribArray(1);
            gl.BufferData(OpenGL.GL_ARRAY_BUFFER, new float[] {
                1, 0, 0, 1, 0.375f, 0, 1, 0.75f, 0, 0.875f, 1, 0, 
                0.5f, 1, 0, 0.125f, 1, 0, 0, 1, 0.25f, 0, 1, 0.625f, 
                0, 1, 1, 0, 0.625f, 1, 0, 0.25f, 1, 0.125f, 0, 1, 
                0.5f, 0, 1, 0.875f, 0, 1, 1, 0, 0.75f, 1, 0, 0.375f},
                OpenGL.GL_STATIC_DRAW);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, surfaceVio);
            gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, new uint[] { 
                0, 1, 2, 3, 7, 6, 5, 4, 8, 9, 10, 11, 15, 14, 13, 12,
                8, 4, 0, 1, 5, 9, 13, 14, 10, 6, 2, 3, 7, 11, 15},
                OpenGL.GL_STATIC_DRAW);

            uint axiesVao = arrays[3];
            uint axiesVbo = buffers[6];
            gl.BindVertexArray(axiesVao);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, axiesVbo);
            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 6 * sizeof(float), IntPtr.Zero);
            gl.VertexAttribPointer(1, 3, OpenGL.GL_FLOAT, false, 6 * sizeof(float), (IntPtr)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(0);
            gl.EnableVertexAttribArray(1);
            gl.BufferData(OpenGL.GL_ARRAY_BUFFER, new float[] {
                0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0,
                0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0,
                0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1}, 
                OpenGL.GL_STATIC_DRAW);

            gl.BindVertexArray(0);
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
            gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
            
            Shader baseShader = new Shader(gl, "cw.base.vert", "cw.base.frag");
            Shader normalsShader = new Shader(gl, "cw.base.vert", "cw.base.frag", "cw.normals.glsl");
            Shader phongShader = new Shader(gl, "cw.base.vert", "cw.phong.frag");
            Shader gouraudShader = new Shader(gl, "cw.gouraud.vert", "cw.base.frag");
            Shader axiesShader = new Shader(gl, "cw.axies.vert", "cw.base.frag");

            gl.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1);

            glArea.Render += (_, _) =>
            {
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                var projMatrix = _camera.GetProjectionMatrix();
                var viewMatrix = _camera.GetViewMatrix();
                // autoscale
                viewMatrix = AutoScale() * viewMatrix;
                var modelMatrix = _object.GetModelMatrix();
                
                gl.UseProgram(baseShader.Id);
                baseShader.SetMatrix4(gl, "model", modelMatrix);
                baseShader.SetMatrix4(gl, "view", viewMatrix);
                baseShader.SetMatrix4(gl, "proj", projMatrix);
                baseShader.SetInt(gl, "useSingleColor", 0);
                baseShader.SetInt(gl, "animate", 0);
                gl.UseProgram(phongShader.Id);
                phongShader.SetInt(gl, "animate", 0);

                if (!_animation.Active)
                {
                    _startTime = (uint) frame_clock.FrameTime;
                }
                
                if (_modelChanged)
                {
                    _modelChanged = false;
                    gl.BindVertexArray(meshVao);
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, meshVbo);
                    _surface.GenerateMesh(ref _object, (int)_uCount.Value, (int)_vCount.Value, _material);
                    _surface.CalculateClipSpacePoints(modelMatrix, viewMatrix, projMatrix);
                    _object.ToArray(true, true, true, out vertices, out elements);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER, vertices, OpenGL.GL_DYNAMIC_DRAW);
                    gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, elements, OpenGL.GL_DYNAMIC_DRAW);
                    
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, pointsVbo);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER, _surface.ToArray(), OpenGL.GL_DYNAMIC_DRAW);
                    
                    gl.BindVertexArray(0);
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
                }

                if (_lightPosChanged)
                {
                    _lightPosChanged = false;
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, lightVbo);
                    gl.BufferData(OpenGL.GL_ARRAY_BUFFER, _pointLight.Point.ToArray(), OpenGL.GL_DYNAMIC_DRAW);
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
                }
                
                gl.BindVertexArray(meshVao);

                if (_fillPolygons.Active)
                {
                    if (_shading.Active == (int) Shading.None)
                    {
                        gl.UseProgram(baseShader.Id);
                        if (_animation.Active)
                        {
                            baseShader.SetInt(gl, "animate", 1);
                            baseShader.SetUint(gl, "curTime", (uint)frame_clock.FrameTime - _startTime);
                        }
                    }
                    else if (_shading.Active == (int) Shading.Gouraud)
                    {
                        gl.UseProgram(gouraudShader.Id);
                        if (_animation.Active)
                        {
                            gouraudShader.SetInt(gl, "animate", 1);
                            gouraudShader.SetUint(gl, "curTime", (uint)frame_clock.FrameTime - _startTime);
                        }
                        gouraudShader.SetInt(gl, "useSingleColor", 0);
                        gouraudShader.SetMatrix4(gl, "model", modelMatrix);
                        gouraudShader.SetMatrix4(gl, "view", viewMatrix);
                        gouraudShader.SetMatrix4(gl, "proj", projMatrix);
                        gouraudShader.SetVec3(gl, "material.Ka", _material.Ka);
                        gouraudShader.SetVec3(gl, "material.Kd", _material.Kd);
                        gouraudShader.SetVec3(gl, "material.Ks", _material.Ks);
                        gouraudShader.SetFloat(gl, "material.p", _material.P);
                        gouraudShader.SetVec3(gl, "ambientIntensity", _ambientLight.Intensity);
                        gouraudShader.SetVec3(gl, "light.intensity", _pointLight.Intensity);
                        var lightPos = Vector3.Transform(_pointLight.Point, Matrix4x4.Transpose(viewMatrix));
                        gouraudShader.SetVec3(gl, "light.pos", lightPos);
                        gouraudShader.SetFloat(gl, "light.attenuation", _pointLight.Attenuation);
                    }
                    else if (_shading.Active == (int) Shading.Phong || _shading.Active == (int) Shading.BlinnPhong)
                    {
                        gl.UseProgram(phongShader.Id);
                        if (_animation.Active)
                        {
                            phongShader.SetInt(gl, "animate", 1);
                            phongShader.SetUint(gl, "curTime", (uint)frame_clock.FrameTime - _startTime);
                        }
                        phongShader.SetMatrix4(gl, "model", modelMatrix);
                        phongShader.SetMatrix4(gl, "view", viewMatrix);
                        phongShader.SetMatrix4(gl, "proj", projMatrix);
                        phongShader.SetVec3(gl, "material.Ka", _material.Ka);
                        phongShader.SetVec3(gl, "material.Kd", _material.Kd);
                        phongShader.SetVec3(gl, "material.Ks", _material.Ks);
                        phongShader.SetFloat(gl, "material.p", _material.P);
                        phongShader.SetVec3(gl, "ambientIntensity", _ambientLight.Intensity);
                        phongShader.SetVec3(gl, "light.intensity", _pointLight.Intensity);
                        var lightPos = Vector3.Transform(_pointLight.Point, Matrix4x4.Transpose(viewMatrix));
                        phongShader.SetVec3(gl, "light.pos", lightPos);
                        phongShader.SetFloat(gl, "light.attenuation", _pointLight.Attenuation);
                        if (_shading.Active == (int) Shading.BlinnPhong)
                            phongShader.SetInt(gl, "blinn", 1);
                        else
                            phongShader.SetInt(gl, "blinn", 0);
                    }
                    gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_FILL);
                    gl.DrawElements(OpenGL.GL_TRIANGLES, elements.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                }

                if (_wireframe.Active)
                {
                    gl.UseProgram(baseShader.Id);
                    baseShader.SetInt(gl, "useSingleColor", 1);
                    baseShader.SetVec3(gl, "singleColor", LineColor);
                    baseShader.SetInt(gl, "animate", 0);
                    gl.LineWidth(2);
                    gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                    gl.DrawElements(OpenGL.GL_TRIANGLES, elements.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                }
                
                if (_drawNormals.Active)
                {
                    gl.UseProgram(normalsShader.Id);
                    normalsShader.SetMatrix4(gl, "model", modelMatrix);
                    normalsShader.SetMatrix4(gl, "view", viewMatrix);
                    normalsShader.SetMatrix4(gl, "proj", projMatrix);
                    normalsShader.SetInt(gl, "useSingleColor", 1);
                    normalsShader.SetVec3(gl, "singleColor", NormalColor);
                    gl.LineWidth(2);
                    gl.DrawElements(OpenGL.GL_TRIANGLES, elements.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                }

                gl.UseProgram(baseShader.Id);
                baseShader.SetMatrix4(gl, "model", Matrix4x4.Identity);
                baseShader.SetInt(gl, "useSingleColor", 1);
                baseShader.SetInt(gl, "animate", 0);
                
                if (_showPointLight.Active)
                {
                    gl.BindVertexArray(lightVao);
                    baseShader.SetVec3(gl, "singleColor", _pointLight.Intensity);
                    gl.PointSize(10);
                    gl.DrawArrays(OpenGL.GL_POINTS, 0, 1);
                }

                gl.BindVertexArray(surfaceVao);
                baseShader.SetMatrix4(gl, "model", modelMatrix);
                baseShader.SetInt(gl, "useSingleColor", 0);
                
                if (_drawPoints.Active)
                {
                    baseShader.SetVec3(gl, "singleColor", Vector3.UnitY);
                    gl.PointSize(10);
                    gl.DrawArrays(OpenGL.GL_POINTS, 0, 16);
                }

                if (_drawSurface.Active)
                {
                    baseShader.SetVec3(gl, "singleColor", LineColor);
                    gl.LineWidth(2);
                    gl.DrawElements(OpenGL.GL_LINE_STRIP, 31, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                }

                if (_drawAxies.Active)
                {
                    gl.BindVertexArray(axiesVao);
                    gl.DepthFunc(OpenGL.GL_ALWAYS);
                    gl.UseProgram(axiesShader.Id);
                    axiesShader.SetMatrix4(gl, "view", Matrix4x4.CreateScale(Math.Min(1f / _camera.AspectRatio, 1), Math.Min(_camera.AspectRatio, 1), 1) * viewMatrix);
                    axiesShader.SetInt(gl, "useSingleColor", 0);
                    axiesShader.SetFloat(gl, "corner", 0.85f);
                    gl.LineWidth(2);
                    gl.DrawArrays(OpenGL.GL_LINES, 0, 6);
                    gl.DepthFunc(OpenGL.GL_LESS);
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

        private Matrix4x4 AutoScale()
        {
            return Matrix4x4.CreateScale(Math.Min(_camera.AspectRatio, 1), Math.Min(_camera.AspectRatio, 1), 1);
        }
        
        private void GlAreaButtonPressHandler(object o, ButtonPressEventArgs args)
        {
            if (_pointerButton != -1) return;
            _pointerButton = (int)args.Event.Button;
            _pointerPos.X = (float)args.Event.X / _glArea.AllocatedWidth * 2 - 1;
            _pointerPos.Y = 1 - (float)args.Event.Y / _glArea.AllocatedHeight * 2;

            if (_clipSpaceChanged)
            {
                _clipSpaceChanged = false;
                _surface.CalculateClipSpacePoints(_object.GetModelMatrix(), AutoScale() * _camera.GetViewMatrix(), _camera.GetProjectionMatrix());
            }
            // left button on point - move
            if (_pointerButton == 1 && (_drawPoints.Active || _drawSurface.Active))
            {
                _movement = Vector3.Zero;
                _pointId = _surface.FindPoint(_pointerPos);
            }
        }
        
        private void GlAreaButtonReleaseHandler(object o, ButtonReleaseEventArgs args)
        {
            _pointerButton = -1;
            _pointId = -1;
        }

        private void GlAreaScrollHandler(object o, ScrollEventArgs args)
        {
            if (_clipSpaceChanged)
            {
                _clipSpaceChanged = false;
                _surface.CalculateClipSpacePoints(_object.GetModelMatrix(), AutoScale() * _camera.GetViewMatrix(), _camera.GetProjectionMatrix());
            }
            
            Vector2 pointerPos = new Vector2((float) args.Event.X / _glArea.AllocatedWidth * 2 - 1,
                1 - (float) args.Event.Y / _glArea.AllocatedHeight * 2);
            int id = _surface.FindPoint(pointerPos);
            if (id != -1 && (_drawPoints.Active || _drawSurface.Active))
            {
                _modelChanged = true;
                int i = id / 4, j = id % 4;
                if (args.Event.Direction == ScrollDirection.Down && _surface.Weights[i, j] > 0)
                {
                    _surface.Weights[i, j] -= 1;
                }
                else if (args.Event.Direction == ScrollDirection.Up)
                {
                    _surface.Weights[i, j] += 1;
                }

                _weightsAdjs[i, j].Value = _surface.Weights[i, j];
                return;
            }
            
            if (args.Event.Direction == ScrollDirection.Down)
            {
                _cameraFOV.Value += _cameraFOV.StepIncrement;
            }
            else if (args.Event.Direction == ScrollDirection.Up)
            {
                _cameraFOV.Value -= _cameraFOV.StepIncrement;
            }
        }
        
        private void GlAreaMotionNotifyHandler(object o, MotionNotifyEventArgs args)
        {
            if (_pointerButton == -1) return;
            
            Vector2 newPointerPos = new Vector2((float) args.Event.X / _glArea.AllocatedWidth * 2 - 1,
                1 - (float) args.Event.Y / _glArea.AllocatedHeight * 2);
            var right = _camera.GetRightVector();
            
            // Left button
            if (_pointerButton == 1)
            {
                // move control point
                if (_pointId != -1)
                {
                    _modelChanged = true;
                    int i = _pointId / 4;
                    int j = _pointId % 4;
                    if (_snapping.Active)
                    {
                        _movement += _camera.Up * 2 * (newPointerPos.Y - _pointerPos.Y) + 
                             right * 2 * _camera.AspectRatio * (newPointerPos.X - _pointerPos.X);
                        _surface.Points[i, j].X += (float) (_gridSize.Value * Math.Truncate(_movement.X / _gridSize.Value));
                        _surface.Points[i, j].Y += (float) (_gridSize.Value * Math.Truncate(_movement.Y / _gridSize.Value));
                        _surface.Points[i, j].Z += (float) (_gridSize.Value * Math.Truncate(_movement.Z / _gridSize.Value));
                        _movement.X %= (float)_gridSize.Value;
                        _movement.Y %= (float)_gridSize.Value;
                        _movement.Z %= (float)_gridSize.Value;
                    }
                    else
                    {
                        _surface.Points[_pointId / 4, _pointId % 4] += _camera.Up * 2 * _camera.AspectRatio * (newPointerPos.Y - _pointerPos.Y) + 
                                                                       right * 2 * (newPointerPos.X - _pointerPos.X);
                    }

                    _pointsAdjs[i, j, 0].Value = _surface.Points[i, j].X;
                    _pointsAdjs[i, j, 1].Value = _surface.Points[i, j].Y;
                    _pointsAdjs[i, j, 2].Value = _surface.Points[i, j].Z;
                }
                // rotate camera
                else
                {
                    var rotation = Matrix4x4.CreateFromAxisAngle(_camera.Up, 2 * _camera.AspectRatio * (_pointerPos.X - newPointerPos.X)) * 
                                   Matrix4x4.CreateFromAxisAngle(right, 2 * (newPointerPos.Y - _pointerPos.Y));
                    var newPosition = Vector3.Transform(_camera.Position - _camera.Target, rotation);
                    _camera.Position = Vector3.Normalize(newPosition) * (_camera.Position - _camera.Target).Length() + _camera.Target;
                    
                    _cameraX.Value = _camera.Position.X;
                    _cameraY.Value = _camera.Position.Y;
                    _cameraZ.Value = _camera.Position.Z;
                }
            }
            // Right button - translate camera target
            else if (_pointerButton == 3)
            {
                _camera.Target += _camera.Up * 2 * (_pointerPos.Y - newPointerPos.Y) +
                                  right * 2 * _camera.AspectRatio * (_pointerPos.X - newPointerPos.X);
                
                _cameraTargetX.Value = _camera.Target.X;
                _cameraTargetY.Value = _camera.Target.Y;
                _cameraTargetZ.Value = _camera.Target.Z;
            }
            // Middle button - translate light point
            else if (_pointerButton == 2)
            {
                _pointLight.Point += _camera.Up * 2 * (newPointerPos.Y - _pointerPos.Y) + 
                                     right * 2 * _camera.AspectRatio * (newPointerPos.X - _pointerPos.X);
                
                _lightX.Value = _pointLight.Point.X;
                _lightY.Value = _pointLight.Point.Y;
                _lightZ.Value = _pointLight.Point.Z;
            }
            
            _pointerPos.X = newPointerPos.X;
            _pointerPos.Y = newPointerPos.Y;
        }

        private class WeightHandler
        {
            private int _i;
            private int _j;
            private MainWindow _parent;

            public WeightHandler(int i, int j, MainWindow parent)
            {
                _i = i;
                _j = j;
                _parent = parent;
            }

            public void ValueChangedHandler(object o, EventArgs args)
            {
                _parent._surface.Weights[_i, _j] = (float)_parent._weightsAdjs[_i, _j].Value;
                _parent._modelChanged = true;
            }
        }
        
        private void SetWeightsControls()
        {
            _weightsAdjs = new Adjustment[4, 4];
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    var adj = new Adjustment(_surface.Weights[i,j], 0, 100, 1, 10, 10);
                    adj.ValueChanged += new WeightHandler(i, j, this).ValueChangedHandler;
                    _weightsAdjs[i, j] = adj;
                    Box box = new Box(Orientation.Horizontal, 2);
                    Label lbl = new Label(" ⦁ ");
                    lbl.StyleContext.AddClass("l" + i + j);
                    box.PackStart(lbl, false, false, 0);
                    box.PackStart(new SpinButton(adj, 1, 2), false, false, 0);
                    _weightsGrid.Attach(box, i, j, 1, 1);
                }
            }
            _weightsGrid.ShowAll();
        }

        private class PointHandler
        {
            private int _i;
            private int _j;
            private int _coord;
            private MainWindow _parent;

            public PointHandler(int i, int j, int coord, MainWindow parent)
            {
                _i = i;
                _j = j;
                _coord = coord;
                _parent = parent;
            }

            public void ValueChangedHandler(object o, EventArgs args)
            {
                switch (_coord)
                {
                    case 0: 
                        _parent._surface.Points[_i, _j].X = (float)_parent._pointsAdjs[_i, _j, _coord].Value;
                        break;
                    case 1: 
                        _parent._surface.Points[_i, _j].Y = (float)_parent._pointsAdjs[_i, _j, _coord].Value;
                        break;
                    case 2: 
                        _parent._surface.Points[_i, _j].Z = (float)_parent._pointsAdjs[_i, _j, _coord].Value;
                        break; 
                }
                _parent._modelChanged = true;
            }
        }

        private void SetPointsControls()
        {
            _pointsAdjs = new Adjustment[4, 4, 3];
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    var adjX = new Adjustment(_surface.Points[i,j].X, -100, 100, 1, 5, 5);
                    var adjY = new Adjustment(_surface.Points[i,j].Y, -100, 100, 1, 5, 5);
                    var adjZ = new Adjustment(_surface.Points[i,j].Z, -100, 100, 1, 5, 5);
                    adjX.ValueChanged += new PointHandler(i, j, 0, this).ValueChangedHandler;
                    adjY.ValueChanged += new PointHandler(i, j, 1, this).ValueChangedHandler;
                    adjZ.ValueChanged += new PointHandler(i, j, 2, this).ValueChangedHandler;
                    _pointsAdjs[i, j, 0] = adjX; _pointsAdjs[i, j, 1] = adjY; _pointsAdjs[i, j, 2] = adjZ;
                    
                    Box box = new Box(Orientation.Horizontal, 2);
                    Label lbl = new Label(" ⦁ ");
                    lbl.StyleContext.AddClass("l" + i + j);
                    
                    box.PackStart(lbl, false, false, 0);
                    box.PackStart(new SpinButton(adjX, 1, 2), false, false, 0);
                    box.PackStart(new SpinButton(adjY, 1, 2), false, false, 0);
                    box.PackStart(new SpinButton(adjZ, 1, 2), false, false, 0);
                    _pointsGrid.Attach(box, i, j, 1, 1);
                }
            }
            _pointsGrid.ShowAll();
        }
    }
}