using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cairo;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;
using CG;

using Extensions;
using Gdk;
using Primitives;
using Window = Gtk.Window;

namespace lab3
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
        [UI] private ComboBoxText _models = null;
        [UI] private ComboBoxText _projections = null;
        [UI] private Adjustment _sidesX = null;
        [UI] private Adjustment _sidesY = null;
        [UI] private Adjustment _radius = null;
        [UI] private Adjustment _height = null;
        [UI] private Adjustment _p = null;
        [UI] private Adjustment _materialR = null; [UI] private Adjustment _materialG = null; [UI] private Adjustment _materialB = null;
        [UI] private Adjustment _kaR = null; [UI] private Adjustment _kaG = null; [UI] private Adjustment _kaB = null;
        [UI] private Adjustment _kdR = null; [UI] private Adjustment _kdG = null; [UI] private Adjustment _kdB = null;
        [UI] private Adjustment _ksR = null; [UI] private Adjustment _ksG = null; [UI] private Adjustment _ksB = null;
        [UI] private Adjustment _ambientR = null; [UI] private Adjustment _ambientG = null; [UI] private Adjustment _ambientB = null;
        [UI] private Adjustment _pointR = null; [UI] private Adjustment _pointG = null; [UI] private Adjustment _pointB = null;
        [UI] private Adjustment _lightX = null; [UI] private Adjustment _lightY = null; [UI] private Adjustment _lightZ = null;
        [UI] private Adjustment _k = null;
        [UI] private CheckButton _showPointLight = null;
        [UI] private ComboBoxText _shading = null;
        [UI] private CheckButton _showVertexNormals = null;

        private Matrix4x4 _worldMatrix;
        private Matrix4x4 _viewMatrix;
        private Mesh _object;
        private Material _material;
        private AmbientLight _ambientLight;
        private PointLight _pointLight;
        private CairoSurface _surface;

        private Vector2 _pointerPos;
        private int _pointerButton = -1;
        private bool _fillPolygons = true;
        private FileChooserDialog _fileChooser;

        private readonly Cairo.Color BACKGROUND_COLOR = new Cairo.Color(0, 0, 0);
        private readonly Cairo.Color LINE_COLOR = new Cairo.Color(1, 0.98, 0.94);
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
            _viewMatrix = new Matrix4x4(
                1, 0, 0, 0,
                0, -1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
            _object = new Mesh();
            _material = new Material(new Vector3( 0.84f, 0.43f, 0.4f), new Vector3(0.3f, 0.3f, 0.3f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), 1);
            _ambientLight = new AmbientLight(new Vector3(1, 1, 1));
            _pointLight = new PointLight(new Vector3(1, 1, 1), new Vector4(0, 3, 6, 1), 0.7f);
            
            _sidesX.Value = 30;
            _sidesY.Value = 20;
            _height.Value = 2;
            _radius.Value = 1;
            
            _models.Active = (int)Model.Cylinder;
            _projections.Active = (int)Projection.None;
            _shading.Active = (int)Shading.Gouraud;
            CalculateWorldMatrix();
            
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
            _k.Value = _pointLight.K;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += (o, args) =>
            {
                Application.Quit();
            };

            _surface = new CairoSurface(_canvas);
            _canvas.Events |= EventMask.ScrollMask | EventMask.PointerMotionMask | EventMask.ButtonPressMask |
                              EventMask.ButtonReleaseMask;
            _canvas.Drawn += CanvasDrawnHandler;
            _canvas.SizeAllocated += (o, args) =>
            {
                float maxDistX = 0, maxDistY = 0;
                foreach (var vertex in _object.Vertices)
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
            _showVertexNormals.Toggled += (o, args) => { _canvas.QueueDraw(); };

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
                        TransformToWorld();
                        _canvas.QueueDraw();
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

            _sidesX.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    TransformToWorld();
                    _canvas.QueueDraw();
                }
            };
            _sidesY.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    TransformToWorld();
                    _canvas.QueueDraw();
                }
            };
            _radius.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    TransformToWorld();
                    _canvas.QueueDraw();
                }
            };
            _height.ValueChanged += (_, _) =>
            {
                if (_models.Active == (int)Model.Cylinder)
                {
                    PrimitiveForms.Prism((int)_sidesX.Value, (int)_sidesY.Value, (float)_height.Value, (float)_radius.Value, _object, _material);
                    TransformToWorld();
                    _canvas.QueueDraw();
                }
            };
            
            _shading.RemoveAll();
            _shading.Append(Shading.None.ToString(), "None");
            _shading.Append(Shading.Flat.ToString(), "Flat");
            _shading.Append(Shading.Gouraud.ToString(), "Gouraud");
            _shading.Changed += (_, _) => { _canvas.QueueDraw(); };

            _p.ValueChanged += (_, _) => { _material.P = (float) _p.Value; _canvas.QueueDraw(); };
            _materialR.ValueChanged += (_, _) => { _material.Color.X = (float) _materialR.Value; _canvas.QueueDraw(); };
            _materialG.ValueChanged += (_, _) => { _material.Color.Y = (float) _materialG.Value; _canvas.QueueDraw(); };
            _materialB.ValueChanged += (_, _) => { _material.Color.Z = (float) _materialB.Value; _canvas.QueueDraw(); };
            _kaR.ValueChanged += (_, _) => { _material.Ka.X = (float) _kaR.Value; _canvas.QueueDraw(); };
            _kaG.ValueChanged += (_, _) => { _material.Ka.Y = (float) _kaG.Value; _canvas.QueueDraw(); };
            _kaB.ValueChanged += (_, _) => { _material.Ka.Z = (float) _kaB.Value; _canvas.QueueDraw(); };
            _kdR.ValueChanged += (_, _) => { _material.Kd.X = (float) _kdR.Value; _canvas.QueueDraw(); };
            _kdG.ValueChanged += (_, _) => { _material.Kd.Y = (float) _kdG.Value; _canvas.QueueDraw(); };
            _kdB.ValueChanged += (_, _) => { _material.Kd.Z = (float) _kdB.Value; _canvas.QueueDraw(); };
            _ksR.ValueChanged += (_, _) => { _material.Ks.X = (float) _ksR.Value; _canvas.QueueDraw(); };
            _ksG.ValueChanged += (_, _) => { _material.Ks.Y = (float) _ksG.Value; _canvas.QueueDraw(); };
            _ksB.ValueChanged += (_, _) => { _material.Ks.Z = (float) _ksB.Value; _canvas.QueueDraw(); };
            _ambientR.ValueChanged += (_, _) => { _ambientLight.Intensity.X = (float) _ambientR.Value; _canvas.QueueDraw(); };
            _ambientG.ValueChanged += (_, _) => { _ambientLight.Intensity.Y = (float) _ambientG.Value; _canvas.QueueDraw(); };
            _ambientB.ValueChanged += (_, _) => { _ambientLight.Intensity.Z = (float) _ambientB.Value; _canvas.QueueDraw(); };
            _pointR.ValueChanged += (_, _) => { _pointLight.Intensity.X = (float) _pointR.Value; _canvas.QueueDraw(); };
            _pointG.ValueChanged += (_, _) => { _pointLight.Intensity.Y = (float) _pointG.Value; _canvas.QueueDraw(); };
            _pointB.ValueChanged += (_, _) => { _pointLight.Intensity.Z = (float) _pointB.Value; _canvas.QueueDraw(); };
            _lightX.ValueChanged += (_, _) => { _pointLight.Point.X = (float) _lightX.Value; _canvas.QueueDraw(); };
            _lightY.ValueChanged += (_, _) => { _pointLight.Point.Y = (float) _lightY.Value; _canvas.QueueDraw(); };
            _lightZ.ValueChanged += (_, _) => { _pointLight.Point.Z = (float) _lightZ.Value; _canvas.QueueDraw(); };
            _k.ValueChanged += (_, _) => { _pointLight.K = (float) _k.Value; _canvas.QueueDraw(); };
            _showPointLight.Toggled += (_, _) => { _canvas.QueueDraw(); };
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
            var normalMatrix = TransposeInvert(_worldMatrix);
            
            foreach (var vertex in _object.Vertices)
            {
                vertex.PointInWorld = Vector4.Transform(vertex.Point, _worldMatrix);
                vertex.NormalInWorld = Vector4.Transform(vertex.Normal, normalMatrix);
            }

            foreach (var polygon in _object.Polygons)
            {
                polygon.NormalInWorld = Vector4.Transform(polygon.Normal, normalMatrix);
            }
            
            if (_zBuffer.Active)
                _object.Polygons = _object.Polygons.OrderBy((p) => p.Vertices.Select((v) => v.PointInWorld.Z).Max()).ToList();
        }

        private Vector2 TransformToView(Vector4 point)
        {
            Vector4 dot = Vector4.Transform(point, _viewMatrix);
            return new Vector2(dot.X, dot.Y);
        }

        private readonly Vector4 _viewDirection = new Vector4(0, 0, -1, 0);

        private Vector3 GetPointColor(Vector4 point, Vector4 normal, Material material, float maxZ)
        {
            Vector3 ambient = _ambientLight.Intensity * material.Ka;
            
            Vector4 toLight = Vector4.Normalize(Vector4.Transform(_pointLight.Point, _worldMatrix) - point);
            Vector3 diffuse = _pointLight.Intensity * material.Kd * Math.Max(Vector4.Dot(toLight, Vector4.Normalize(normal)), 0);

            Vector4 reflect = Vector4.Normalize(2 * Vector4.Dot(toLight, normal) * normal - toLight);
            Vector4 toViewer = Vector4.Normalize(-_viewDirection);
            Vector3 specular = (Vector4.Dot(toLight, normal) > 0 ? 1 : 0) * _pointLight.Intensity * material.Ks *
                               (float)Math.Pow(Math.Max(Vector4.Dot(reflect, toViewer), 0), material.P);
            
            return material.Color * (ambient + (diffuse + specular) / (maxZ - point.Z + _pointLight.K));
        }
            
        private const int NormalLength = 20;
        
        private void CanvasDrawnHandler(object o, DrawnArgs args)
        {
            var cr = args.Cr;
            cr.Antialias = Antialias.None;
            cr.LineJoin = LineJoin.Bevel; // чинит острые концы у линий при маленьком увеличении
            
            cr.SetSourceColor(BACKGROUND_COLOR);
            cr.Paint();

            float maxZ = _object.Vertices.Select((a) => a.PointInWorld.Z).Max();
                
            if (_shading.Active == (int) Shading.Gouraud)
                _surface.BeginUpdate(cr);    
            
            foreach (var polygon in _object.Polygons)
            {
                if (Vector4.Dot(polygon.NormalInWorld, _viewDirection) > 0 && _hideInvisible.Active)
                    continue;

                Vector4 center = polygon.Vertices[0].PointInWorld;
                cr.MoveTo(TransformToView(polygon.Vertices[0].PointInWorld));
                for (int i = 1; i < polygon.Vertices.Length; ++i)
                {
                    center += polygon.Vertices[i].PointInWorld;
                    cr.LineTo(TransformToView(polygon.Vertices[i].PointInWorld));
                }
                center /= polygon.Vertices.Length;
                cr.ClosePath();
                cr.SetSourceColor(LINE_COLOR);
                
                if (_fillPolygons)
                {
                    if (_shading.Active == (int) Shading.None)
                    {
                        cr.SetSourceRGB(polygon.Material.Color.X, polygon.Material.Color.Y, polygon.Material.Color.Z);
                        cr.Fill();
                    }
                    else if (_shading.Active == (int) Shading.Flat)
                    {
                        var color = GetPointColor(center, polygon.NormalInWorld, polygon.Material, maxZ);
                        cr.SetSourceRGB(color.X, color.Y, color.Z);
                        cr.Fill();
                    }
                    else if (_shading.Active == (int) Shading.Gouraud)
                    {
                        cr.NewPath();
                        List<Vector3> colors = new();
                        foreach (Vertex vertex in polygon.Vertices)
                        {
                            colors.Add(GetPointColor(vertex.PointInWorld, vertex.NormalInWorld, polygon.Material, maxZ));
                        }

                        var point1 = TransformToView(polygon.Vertices[0].PointInWorld);
                        for (int i = 1; i < polygon.Vertices.Length - 1; ++i)
                        {
                            var point2 = TransformToView(polygon.Vertices[i].PointInWorld);
                            var point3 = TransformToView(polygon.Vertices[i+1].PointInWorld);
                            _surface.DrawTriangle(colors[0], point1, colors[i], point2, colors[i+1], point3);
                        }
                    }
                }
            }
            if (_shading.Active == (int) Shading.Gouraud)
                _surface.EndUpdate();

            foreach (var polygon in _object.Polygons)
            {
                if (Vector4.Dot(polygon.NormalInWorld, _viewDirection) > 0 && _hideInvisible.Active)
                    continue;
                
                Vector4 center = polygon.Vertices[0].PointInWorld;
                cr.MoveTo(TransformToView(polygon.Vertices[0].PointInWorld));
                for (int i = 1; i < polygon.Vertices.Length; ++i)
                {
                    center += polygon.Vertices[i].PointInWorld;
                    cr.LineTo(TransformToView(polygon.Vertices[i].PointInWorld));
                }
                center /= polygon.Vertices.Length;
                cr.ClosePath();
                cr.SetSourceColor(LINE_COLOR);

                if (_wireframe.Active)
                    cr.Stroke();
                else
                    cr.NewPath();

                if (_showNormals.Active)
                {
                    cr.Save();
                    cr.SetSourceColor(NORMAL_COLOR);
                    
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
            
            if (_showVertexNormals.Active)
            {
                cr.SetSourceColor(NORMAL_COLOR);
                foreach (Vertex vertex in _object.Vertices)
                {
                    bool visible = false;
                    foreach (Polygon polygon in vertex.Polygons)
                    {
                        if (Vector4.Dot(_viewDirection, polygon.NormalInWorld) <= 0)
                        {
                            visible = true;
                            break;
                        }
                    }

                    if (!visible)
                        continue;
                    
                    cr.MoveTo(TransformToView(vertex.PointInWorld));
                    var viewNormal = TransformToView(vertex.NormalInWorld);
                    if (viewNormal.Length() > NormalLength)
                    {
                        viewNormal = Vector2.Normalize(viewNormal) * NormalLength;
                    }
                    cr.RelLineTo(viewNormal);
                    cr.Stroke();
                }
            }
            
            if (_showPointLight.Active)
            {
                cr.SetSourceRGB(1, 1, 1);
                Vector2 point = TransformToView(Vector4.Transform(_pointLight.Point, _worldMatrix));
                cr.Arc(point.X, point.Y, 5, 0, 2 * Math.PI);
                cr.ClosePath();
                cr.Fill();
                cr.Stroke();
            }
        }

        private static void MatrixToAngles(Matrix4x4 matrix, out double x, out double y, out double z)
        {
            x = Math.Atan2(matrix.M23, matrix.M33) / Math.PI * 180;
            y = Math.Atan2(-matrix.M13, Math.Sqrt(1 - matrix.M13 * matrix.M13)) / Math.PI * 180;
            z = Math.Atan2(matrix.M12, matrix.M11) / Math.PI * 180;
        }

        private void CanvasMotionNotifyHandler(object o, MotionNotifyEventArgs args)
        {
            if (_pointerButton == -1) return;
            
            // Left button - rotate
            if (_pointerButton == 1 && _projections.Active < (int)Projection.Isometric)
            {
                // _xAngle.Value = (_xAngle.Value + 360 - (args.Event.Y - _pointerPos.Y) / _canvas.Window.Height * 360) % 360;
                // _yAngle.Value = (_yAngle.Value + 360 + (args.Event.X - _pointerPos.X) / _canvas.Window.Width * 360) % 360;
                
                var currentRotation = Matrix4x4.CreateRotationX((float)(_xAngle.Value * Math.PI / 180)) * 
                                      Matrix4x4.CreateRotationY((float)(_yAngle.Value * Math.PI / 180)) * 
                                      Matrix4x4.CreateRotationZ((float)(_zAngle.Value * Math.PI / 180));
                
                Vector3 axis = new((float) (args.Event.X - _pointerPos.X), (float) (args.Event.Y - _pointerPos.Y), 0);
                float angle = (float)(axis.Length() / 180 * Math.PI);
                axis = Vector3.Normalize(new Vector3(axis.Y, axis.X, 0));
                var rotation = Matrix4x4.CreateFromAxisAngle(axis, angle);
                currentRotation *= rotation;
                MatrixToAngles(currentRotation, out var x, out var y, out var z);
                _xAngle.Value = x;
                _yAngle.Value = y;
                _zAngle.Value = z;
                CalculateWorldMatrix();
            }
            // Right button - translate
            else if (_pointerButton == 3)
            {
                _xShift.Value += (args.Event.X - _pointerPos.X) / _viewMatrix.M11;
                _yShift.Value -= (args.Event.Y - _pointerPos.Y) / _viewMatrix.M11;
            }
            // Middle button - translate light point
            else if (_pointerButton == 2)
            {
                _lightX.Value += (args.Event.X - _pointerPos.X) / _viewMatrix.M11;
                _lightY.Value -= (args.Event.Y - _pointerPos.Y) / _viewMatrix.M11;
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