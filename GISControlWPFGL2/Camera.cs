using OpenTK.Mathematics;

namespace GISControlWPFGL2
{
    public class Camera
    {
        public Camera(Vector3d position)
        {
            UpdateCamera(position);
        }

        private Vector3d _front = -Vector3d.UnitZ;
        private Vector3d _up = Vector3d.UnitY;
        private Vector3d _right = Vector3d.UnitX;
        private double _fov = MathHelper.PiOver2;

        public Vector3d Position { get; set; } = Vector3d.UnitZ * 10;
        public double AspectRatio { get; set; } = 1.0F;
        public Vector3d Front => _front;
        public Vector3d Up => _up;
        public Vector3d Right => _right;
        public double Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }
        public const float ZoomFactor = 1.1F;
        public const double Resolution = 1.0;
        public const double MinViewR = 1000.0 / Resolution;
        public const double MaxViewR = 100000000.0 / Resolution;
        public Vector3d DragStartPosition = Vector3d.Zero;
        public Matrix4d DragStartVPMatrix = Matrix4d.Identity;
        public Vector3d DragPrevSurface = Vector3d.Zero;

        public Matrix4d GetViewMatrix()
        {
            return Matrix4d.LookAt(Position, Position + _front, _up);
        }
        public Matrix4d GetProjectionMatrix()
        {
            return Matrix4d.CreatePerspectiveFieldOfView(_fov, AspectRatio, MinViewR, MaxViewR);
        }
        public void UpdateCamera(Vector3d NewPosition)
        {
            Position = NewPosition;
            _front = Vector3d.Normalize(-Position);
            _right = Vector3d.Normalize(Vector3d.Cross(_front, Vector3d.UnitZ));
            _up = Vector3d.Normalize(Vector3d.Cross(_right, _front));
        }

        // OpenTK Extension
        public static Vector3d Unproject3D(Vector3d vector, double x, double y, double width, double height, double minZ, double maxZ, Matrix4d inverseWorldViewProjection)
        {
            double num = (vector.X - x) / width * 2.0 - 1.0;
            double num2 = (vector.Y - y) / height * 2.0 - 1.0;
            double num3 = (vector.Z - minZ) / (maxZ - minZ) * 2.0 - 1.0;
            Vector3d vector2 = Vector3d.Zero;
            vector2.X = num * inverseWorldViewProjection.M11 + num2 * inverseWorldViewProjection.M21 + num3 * inverseWorldViewProjection.M31 + inverseWorldViewProjection.M41;
            vector2.Y = num * inverseWorldViewProjection.M12 + num2 * inverseWorldViewProjection.M22 + num3 * inverseWorldViewProjection.M32 + inverseWorldViewProjection.M42;
            vector2.Z = num * inverseWorldViewProjection.M13 + num2 * inverseWorldViewProjection.M23 + num3 * inverseWorldViewProjection.M33 + inverseWorldViewProjection.M43;
            double num4 = num * inverseWorldViewProjection.M14 + num2 * inverseWorldViewProjection.M24 + num3 * inverseWorldViewProjection.M34 + inverseWorldViewProjection.M44;
            return vector2 / num4;
        }

        public static Matrix4 FromMatrix4d(Matrix4d matrix)
        {
            return new Matrix4((Vector4)matrix.Row0, (Vector4)matrix.Row1, (Vector4)matrix.Row2, (Vector4)matrix.Row3);
        }
    }
}