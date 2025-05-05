using OpenTK.Mathematics;

namespace GISControlWPFGL2
{
    public class Camera
    {
        // Those vectors are directions pointing outwards from the camera to define how it rotated.
        private Vector3 _front = -Vector3.UnitZ;
        private Vector3 _up = Vector3.UnitY;
        private Vector3 _right = Vector3.UnitX;

        // The field of view of the camera (radians)
        private float _fov = MathHelper.PiOver2;

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            _front = Vector3.Normalize(-Position);
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitX));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
            AspectRatio = aspectRatio;

            var lla = GeodeticConverter<float>.ECEFtoLLA(position.X, position.Y, position.Z);
            PositionLLA = new Vector3(GeodeticConverter<float>.ECEFtoLLA(lla.LatitudeDeg, lla.LongitudeDeg, lla.AltitudeMeters));
        }
        public Camera()
        {
            Position = Vector3.UnitZ * 10;
            _front = Vector3.Normalize(-Position);
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
            AspectRatio = 1.0F;
        }

        // The position of the camera
        public Vector3 Position { get; set; }
        public Vector3 PositionLLA { get; set; }

        // This is simply the aspect ratio of the viewport, used for the projection matrix.
        public float AspectRatio { get; set; }

        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;

        // The field of view (FOV) is the vertical angle of the camera view.
        // This has been discussed more in depth in a previous tutorial,
        // but in this tutorial, you have also learned how we can use this to simulate a zoom feature.
        // We convert from degrees to radians as soon as the property is set to improve performance.
        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        // Get the projection matrix using the same method we have used up until this point
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 1f, 50000000f);
        }
    }
}