using Catfood.Shapefile;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace GISControlWPFGL2
{
    struct Ring
    {
        public int memoryOffset;              // vertex buffer object에서 현재 객체 Ring의 인덱스
        public int vertexOffset;
        public List<Vector3> listECEF;  // vertex 리스트 (ECEF)
        public List<Vector2> listLL;    // 참조용 위경도 리스트
        public Color4 Color;            // ring 색깔
        public Ring()
        {
            memoryOffset = 0;
            vertexOffset = 0;
            listLL = new List<Vector2>();
            listECEF = new List<Vector3>();
            Color = Color4.White;
        }
    }

    /// <summary>
    /// Interaction logic for GISControl.xaml
    /// </summary>
    public partial class GISControl : UserControl
    {
        private int Program;
        private int VAO;
        private int VBO;

        private static readonly string VertexShaderSourcePath = "./Shader/shader.vert";
        private static readonly string FragmentShaderSourcePath = "./Shader/shader.frag";

        // Vertex Array Object, Vertex Buffer Object 생성
        private int posLocation;
        private int colorLocation;
        private int modelLocation;
        private int viewLocation;
        private int projLocation;

        private Camera camera;
        List<Ring> MapRings = new List<Ring>();

        public GISControl()
        {
            InitializeComponent();
            OpenTKControl.Start();
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
            foreach (string file in Directory.EnumerateFiles("./shp", "*.shp", SearchOption.AllDirectories))
            {
                Shapefile shapefile = new Shapefile(file);
                foreach (Shape shape in shapefile)
                {
                    switch (shape.Type)
                    {
                        case ShapeType.Point:
                            Debug.WriteLine(Enum.GetName<ShapeType>(shape.Type));
                            break;
                        case ShapeType.Polygon:
                            ShapePolygon shapePolygon = shape as ShapePolygon;
                            foreach (PointD[] part in shapePolygon.Parts)
                            {
                                Ring ring = new Ring();
                                foreach (PointD point in part)
                                {
                                    ring.listLL.Add(new Vector2((float)point.Y, (float)point.X));
                                    var ecef = GeodeticConverter<double>.LLAtoECEF(point.Y, point.X, 0);
                                    ring.listECEF.Add(new Vector3((float)ecef.X, (float)ecef.Y, (float)ecef.Z));
                                }
                                // vertex buffer object의 메모리 인덱스 추가
                                if(MapRings.Count > 0)
                                {
                                    ring.memoryOffset = MapRings.Last().memoryOffset + MapRings.Last().listECEF.Count * Unsafe.SizeOf<Vector3>();
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
            var tmpLL = MapRings[0].listLL[0];
            var tmpECEF = GeodeticConverter<float>.LLAtoECEF(tmpLL.X, tmpLL.Y, 5000000f);
            camera = new Camera(tmpECEF, 1);

            // Vertex Array Object, Vertex Buffer Object 생성
            posLocation = GL.GetAttribLocation(Program, "vPosition");
            colorLocation = GL.GetUniformLocation(Program, "uColor");
            modelLocation = GL.GetUniformLocation(Program, "uModel");
            viewLocation = GL.GetUniformLocation(Program, "uView");
            projLocation = GL.GetUniformLocation(Program, "uProjection");

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            int totalSize = MapRings.Last().memoryOffset + MapRings.Last().listECEF.Count * Unsafe.SizeOf<Vector3>();
            GL.BufferData(BufferTarget.ArrayBuffer, totalSize, IntPtr.Zero, BufferUsageHint.StaticDraw);

            foreach (var ring in MapRings)
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, ring.memoryOffset, ring.listECEF.Count * Unsafe.SizeOf<Vector3>(), ring.listECEF.ToArray());
            }
            GL.EnableVertexAttribArray(posLocation);
            GL.VertexAttribPointer(posLocation, 3, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vector3>(), 0);
            GL.BindVertexArray(0);
        }

        private void OpenTKControl_Render(TimeSpan obj)
        {
            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(Program);
            //GL.LineWidth(2);

            Matrix4 ModelMatrix = Matrix4.Identity;
            GL.UniformMatrix4(modelLocation, false, ref ModelMatrix);
            Matrix4 ViewMatrix = camera.GetViewMatrix();
            GL.UniformMatrix4(viewLocation, false, ref ViewMatrix);
            Matrix4 ProjectionMatrix = camera.GetProjectionMatrix();
            GL.UniformMatrix4(projLocation, false, ref ProjectionMatrix);
            GL.Uniform4(colorLocation, Color4.White);

            GL.BindVertexArray(VAO);

            foreach (var ring in MapRings)
            {
                GL.DrawArrays(PrimitiveType.LineStrip, ring.vertexOffset, ring.listECEF.Count);
            }
        }

        private void OpenTKControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
        }

        private void OpenTKControl_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            camera.AspectRatio = (float)(OpenTKControl.ActualWidth / OpenTKControl.ActualHeight);
        }
    }

}
