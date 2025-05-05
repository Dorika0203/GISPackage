using OpenTK.Mathematics;
using System.Diagnostics;
using System.Windows;

namespace GISControlWPFGL2
{
    public class Camera
    {
        public Camera(Vector3 position)
        {
            UpdateCamera(position);
        }

        private Vector3 _front = -Vector3.UnitZ;
        private Vector3 _up = Vector3.UnitY;
        private Vector3 _right = Vector3.UnitX;
        private float _fov = MathHelper.PiOver2;

        public Vector3 Position { get; set; } = Vector3.UnitZ * 10;
        public float AspectRatio { get; set; } = 1.0F;
        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;
        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }
        public const float ZoomFactor = 1.1F;
        public const float MinViewR = 1000.0F;
        public const float MaxViewR = 10000000.0F;
        public Vector3 DragStartPosition = Vector3.Zero;
        public Matrix4 DragStartVPMatrix = Matrix4.Zero;
        public Vector3 DragPrevSurface = Vector3.Zero;

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, MinViewR, MaxViewR);
        }
        public void UpdateCamera(Vector3 NewPosition)
        {
            Position = NewPosition;
            _front = Vector3.Normalize(-Position);
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitZ));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        }
    }
}