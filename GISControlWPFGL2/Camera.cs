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
        public double AspectRatio { get; set; } = 1.0;
        public Vector3d Front => _front;
        public Vector3d Up => _up;
        public Vector3d Right => _right;
        public double Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1, 90);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }
        public const double ZoomFactor = 1.1;
        public const double MinViewR = 1000.0;
        public const double MaxViewR = 10000000.0;
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
    }
}