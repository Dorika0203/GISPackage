using Catfood.Shapefile;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace GISControlWPFGL2
{
    struct Ring
    {
        public int memoryOffset;              // vertex buffer object에서 현재 객체 Ring의 인덱스
        public int vertexOffset;
        public List<Vector3d> listECEF;  // vertex 리스트 (ECEF)
        public List<Vector3d> listLLA;    // 참조용 위경도 리스트
        public Color4 Color;            // ring 색깔
        public Ring()
        {
            memoryOffset = 0;
            vertexOffset = 0;
            listLLA = [];
            listECEF = [];
            Color = Color4.White;
        }
    }

    /// <summary>
    /// Interaction logic for GISControl.xaml
    /// </summary>
    public partial class GISControl : UserControl
    {
        private readonly int Program;
        private readonly int VAO;
        private readonly int VBO;

        private static readonly string VertexShaderSourcePath = "./Shader/shader.vert";
        private static readonly string FragmentShaderSourcePath = "./Shader/shader.frag";

        // Vertex Array Object, Vertex Buffer Object 생성
        private readonly int posLocation;
        private readonly int colorLocation;
        private readonly int modelLocation;
        private readonly int viewLocation;
        private readonly int projLocation;
        private readonly int viewProjLocation;

        private readonly Camera camera;
        private readonly List<Ring> MapRings = [];

        public GISControl()
        {
            InitializeComponent();
            OpenTKControl.Start();
            OpenTKControl.Settings.RenderContinuously = true;
            Program = GL.CreateProgram();

            #region Vertex Shader
            // Vertex 쉐이더 컴파일
            var VertexShaderSource = File.ReadAllText(VertexShaderSourcePath);
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, VertexShaderSource);
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string log = GL.GetShaderInfoLog(vertexShader);
                Debug.WriteLine($"Vertex2D shader compile error: {log}");
            }
            #endregion

            #region Fragment Shader
            // Fragment 쉐이더 컴파일
            var FragmentShaderSource = File.ReadAllText(FragmentShaderSourcePath);
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, FragmentShaderSource);
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string log = GL.GetShaderInfoLog(fragmentShader);
                Debug.WriteLine($"Fragment shader compile error: {log}");
            }
            #endregion

            #region Shader Apply
            // 쉐이더 적용
            GL.AttachShader(Program, vertexShader);
            GL.AttachShader(Program, fragmentShader);
            GL.LinkProgram(Program);
            GL.GetProgram(Program, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string log = GL.GetProgramInfoLog(Program);
                Debug.WriteLine($"Program link error: {log}");
            }
            GL.DetachShader(Program, vertexShader);
            GL.DetachShader(Program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            #endregion

            // 지도(.shp) 읽기 및 ECEF 변환
            foreach (string file in Directory.EnumerateFiles("./shp/gadm41_KOR_shp", "*.shp", SearchOption.AllDirectories))
            {
                if (!file.EndsWith("_0.shp")) continue;
                //if (!file.EndsWith("_0.shp") && !file.EndsWith("_1.shp")) continue;
                Shapefile shapefile = new(file);
                foreach (Shape shape in shapefile)
                {
                    switch (shape.Type)
                    {
                        case ShapeType.Point:
                            Debug.WriteLine(Enum.GetName<ShapeType>(shape.Type));
                            break;
                        case ShapeType.Polygon:
                            if (shape is not ShapePolygon shapePolygon) break;
                            foreach (PointD[] part in shapePolygon.Parts)
                            {
                                Ring ring = new();
                                foreach (PointD point in part)
                                {
                                    ring.listLLA.Add(new Vector3d(point.Y, point.X, 0));
                                    Vector3d ecef = GeodeticConverter.LLAtoECEF((point.Y, point.X, 0));
                                    ring.listECEF.Add(ecef);
                                }
                                // vertex buffer object의 메모리 인덱스 추가
                                if (MapRings.Count > 0)
                                {
                                    ring.memoryOffset = MapRings.Last().memoryOffset + MapRings.Last().listECEF.Count * Unsafe.SizeOf<Vector3d>();
                                    ring.vertexOffset = MapRings.Last().vertexOffset + MapRings.Last().listECEF.Count;
                                }
                                else
                                {
                                    ring.memoryOffset = 0;
                                    ring.vertexOffset = 0;
                                }
                                MapRings.Add(ring);
                            }
                            break;
                        case ShapeType.PolyLine:
                            Debug.WriteLine(Enum.GetName<ShapeType>(shape.Type));
                            break;
                        default:
                            Debug.WriteLine(Enum.GetName<ShapeType>(shape.Type));
                            break;
                    }
                }
            }

            // 지도 데이터에서 읽은 점 중 하나로 카메라 설정
            var posInit = MapRings[0].listLLA[0];
            posInit.Z = 1000000;
            var posInitECEF = GeodeticConverter.LLAtoECEF(posInit);
            camera = new Camera(posInitECEF);

            // Vertex Array Object, Vertex Buffer Object 생성
            posLocation = GL.GetAttribLocation(Program, "vPosition");
            colorLocation = GL.GetUniformLocation(Program, "uColor");
            viewLocation = GL.GetUniformLocation(Program, "uView");
            projLocation = GL.GetUniformLocation(Program, "uProjection");
            viewProjLocation = GL.GetUniformLocation(Program, "uViewProjection");

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            int totalSize = MapRings.Last().memoryOffset + MapRings.Last().listECEF.Count * Unsafe.SizeOf<Vector3d>();
            GL.BufferData(BufferTarget.ArrayBuffer, totalSize, IntPtr.Zero, BufferUsageHint.StaticDraw);

            foreach (var ring in MapRings)
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, ring.memoryOffset, ring.listECEF.Count * Unsafe.SizeOf<Vector3>(), ring.listECEF.ToArray());
            }
            GL.EnableVertexAttribArray(posLocation);
            //GL.VertexAttribPointer(posLocation, 3, VertexAttribPointerType.Double, false, Unsafe.SizeOf<Vector3d>(), 0);
            GL.VertexAttribLPointer(posLocation, 3, VertexAttribDoubleType.Double, Unsafe.SizeOf<Vector3d>(), 0);
            GL.BindVertexArray(0);
        }

        private void OpenTKControl_Render(TimeSpan obj)
        {
            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(Program);

            Matrix4d ViewMatrix = camera.GetViewMatrix();
            GL.UniformMatrix4(viewLocation, false, ref ViewMatrix);

            Matrix4d ProjectionMatrix = camera.GetProjectionMatrix();
            GL.UniformMatrix4(projLocation, false, ref ProjectionMatrix);

            Matrix4d ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
            GL.UniformMatrix4(viewProjLocation, false, ref ViewProjectionMatrix);

            GL.Uniform4(colorLocation, Color4.Red);

            GL.BindVertexArray(VAO);

            foreach (var ring in MapRings)
            {
                GL.DrawArrays(PrimitiveType.LineStrip, ring.vertexOffset, ring.listECEF.Count);
            }
        }

        private void OpenTKControl_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            camera.AspectRatio = OpenTKControl.ActualWidth / OpenTKControl.ActualHeight;
        }

        private void OpenTKControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (camera.DragPrevSurface == Vector3d.Zero) return;
                if (camera.DragStartVPMatrix == Matrix4d.Identity) return;

                var mousePos = e.GetPosition(this);

                Vector3d NDC;
                NDC.X = (mousePos.X - (OpenTKControl.ActualWidth / 2.0)) / (OpenTKControl.ActualWidth / 2.0);
                NDC.Y = -1.0 * (mousePos.Y - (OpenTKControl.ActualHeight / 2.0)) / (OpenTKControl.ActualHeight / 2.0);
                NDC.Z = 0.01;
                var pointOnRay = OpenTKExtension.Unproject3D(NDC, -1, -1, 2, 2, -1, 1, Matrix4d.Invert(camera.DragStartVPMatrix));
                Vector3d rayDirection = pointOnRay - camera.DragStartPosition;
                var surface = GeodeticConverter.FindIntersectionWGS84(camera.DragStartPosition, rayDirection);

                if (surface == Vector3d.Zero) return;

                var vec1 = -camera.DragPrevSurface;
                var vec2 = -surface;
                vec1.Normalize();
                vec2.Normalize();
                var rotAngle = Vector3d.CalculateAngle(vec1, ((Vector3d)vec2));
                var rotAngleDeg = rotAngle * 180.0 / double.Pi;

                if (rotAngleDeg < 0.00001)
                {
                    return;
                }
                var rotMatrix = Matrix4d.CreateFromAxisAngle(Vector3d.Cross(vec2, vec1), rotAngle);
                var newCameraPosition = (new Vector4d(camera.Position, 1.0) * rotMatrix).Xyz;
                var cameraPosPrev = camera.Position;
                camera.UpdateCamera(newCameraPosition);
                camera.DragPrevSurface = surface;
            }
        }

        private void OpenTKControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(this);

            Vector3d NDC;
            NDC.X = (mousePos.X - (OpenTKControl.ActualWidth / 2.0)) / (OpenTKControl.ActualWidth / 2.0);
            NDC.Y = -1.0 * (mousePos.Y - (OpenTKControl.ActualHeight / 2.0)) / (OpenTKControl.ActualHeight / 2.0);
            NDC.Z = 0.01;
            var temp = OpenTKExtension.Unproject3D(NDC, -1, -1, 2, 2, -1, 1, Matrix4d.Invert(camera.GetViewMatrix() * camera.GetProjectionMatrix()));

            Vector3d rayDir = temp - camera.Position;
            var surface = GeodeticConverter.FindIntersectionWGS84(camera.Position, rayDir);
            if (surface == Vector3.Zero) return;

            camera.DragStartVPMatrix = camera.GetViewMatrix() * camera.GetProjectionMatrix();
            camera.DragStartPosition = camera.Position;
            camera.DragPrevSurface = (Vector3)surface;

            // 디버깅용
            //List<Vector3> debugList;
            foreach(var ring in MapRings)
            {
                foreach (var ecef in ring.listECEF)
                {
                    var pos = new Vector4d(ecef, 1);
                    var debug5 = pos * camera.GetViewMatrix() * camera.GetProjectionMatrix();
                    var debug6 = (debug5 / debug5.W).Xyz;
                    var debug7 = new Vector4((float)debug5.X, (float)debug5.Y, (float)debug5.Z, (float)debug5.W);
                    var debug8 = (debug7 / debug7.W).Xyz;
                    var debug9 = debug6 - (Vector3d)debug8;

                    //if(double.Abs(debug9.X) > 1e-7 || double.Abs(debug9.Y) > 1e-7 || double.Abs(debug9.Z) > 1e-7)
                    //{
                    //    Debug.WriteLine($"{debug9}");
                    //}
                    //Debug.WriteLine($"{debug8}");
                    if (float.Abs(debug8.X) < 1 && float.Abs(debug8.Y) < 1 && float.Abs(debug8.Z) < 1)
                    {
                        //Debug.WriteLine($"{debug8}");
                    }
                }
            }
            Debug.WriteLine("DONE");
        }

        private void OpenTKControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            camera.DragStartVPMatrix = Matrix4d.Identity;
            camera.DragStartPosition = Vector3d.Zero;
            camera.DragPrevSurface = Vector3d.Zero;
        }

        private void OpenTKControl_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var posSurf = camera.Position;
            posSurf.Normalize();
            posSurf = posSurf * GeodeticConverter.EarthA;
            var posDelta = camera.Position - posSurf;

            if (e.Delta < 0)
            {
                posDelta *= 1.1;
                if (posDelta.Length > Camera.MaxViewR) { posDelta /= Camera.ZoomFactor; }
            }
            else
            {
                posDelta /= 1.1;
                if (posDelta.Length < Camera.MinViewR) { posDelta *= Camera.ZoomFactor; }
            }
            camera.UpdateCamera(posDelta + posSurf);
        }
    }

}
