using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace GISControlWPFGL2
{
    struct LIneSeries
    {
        public List<Vector3> Vertices;
        public Color4 Color;
        public LIneSeries() { 
            Vertices = new List<Vector3>();
            Color = new Color4(1, 1, 1, 1);
        }
    }

    class GLProgram
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

        private Camera camera = new Camera(Vector3.UnitZ, 1);

        public void Initialize()
        {
            Program = GL.CreateProgram();

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

            // 지도(.shp) 읽기 및 ECEF 변환
            LIneSeries polygon = new LIneSeries();
            polygon.Vertices.Add(new Vector3(0.5f, 0.5f, 0.0f));
            polygon.Vertices.Add(new Vector3(0.5f, -0.5f, 0.0f));
            polygon.Vertices.Add(new Vector3(-0.5f, -0.5f, 0.0f));
            polygon.Vertices.Add(new Vector3(-0.5f, 0.5f, 0.0f));
            polygon.Color = Color4.Red;

            // Vertex Array Object, Vertex Buffer Object 생성
            posLocation = GL.GetAttribLocation(Program, "vPosition");
            colorLocation = GL.GetUniformLocation(Program, "uColor");
            modelLocation = GL.GetUniformLocation(Program, "uModel");
            viewLocation = GL.GetUniformLocation(Program, "uView");
            projLocation = GL.GetUniformLocation(Program, "uProj");

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, polygon.Vertices.Count * Unsafe.SizeOf<Vector3>(), polygon.Vertices.ToArray(), BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(posLocation);
            GL.VertexAttribPointer(posLocation, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<Vector3>(), 0);

            GL.EnableVertexAttribArray(colorLocation);
            GL.Uniform4(colorLocation, polygon.Color);
        }

        public void Render()
        {
            GL.ClearColor(Color4.Brown);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(Program);
            GL.LineWidth(2);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);

            Matrix4 ModelMatrix = Matrix4.Identity;
            GL.UniformMatrix4(modelLocation, false, ref ModelMatrix);
            Matrix4 ViewMatrix = camera.GetViewMatrix();
            GL.UniformMatrix4(viewLocation, false, ref ViewMatrix);
            Matrix4 ProjectionMatrix = camera.GetProjectionMatrix();
            GL.UniformMatrix4(viewLocation, false, ref ProjectionMatrix);
        }
    }

}
