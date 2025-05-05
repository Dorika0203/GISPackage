using OpenTK.Mathematics;

namespace GISControlWPFGL2
{
    public static class GeodeticConverter
    {
        // WGS84 constants
        private static readonly double a = 6378137.0;                 // 반장축
        private static readonly double f = 1 / 298.257223563;         // 편평률
        private static readonly double e2 = f * (2 - f);              // 이심률 제곱
        private static readonly double b = a * (1 - f);               // 반단축

        // LLA to ECEF
        public static (double, double, double) LLAtoECEF(Vector3d LLA)
        {
            double lat = LLA.X * Math.PI / 180.0;
            double lon = LLA.Y * Math.PI / 180.0;
            double alt = LLA.Z;

            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            double sinLon = Math.Sin(lon);
            double cosLon = Math.Cos(lon);

            double N = a / Math.Sqrt(1 - e2 * sinLat * sinLat);

            double x = (N + alt) * cosLat * cosLon;
            double y = (N + alt) * cosLat * sinLon;
            double z = (N * (1 - e2) + alt) * sinLat;

            return (x, y, z);
        }

        // ECEF to LLA
        public static (double, double, double) ECEFtoLLA(Vector3d ecef)
        {
            double ePrime2 = (a * a - b * b) / (b * b);
            double p = Math.Sqrt(ecef.X * ecef.X + ecef.Y * ecef.Y);
            double theta = Math.Atan2(ecef.Z * a, p * b);

            double sinTheta = Math.Sin(theta);
            double cosTheta = Math.Cos(theta);

            double lat = Math.Atan2(ecef.Z + ePrime2 * b * sinTheta * sinTheta * sinTheta,
                                    p - e2 * a * cosTheta * cosTheta * cosTheta);

            double lon = Math.Atan2(ecef.Y, ecef.X);

            double sinLat = Math.Sin(lat);
            double N = a / Math.Sqrt(1 - e2 * sinLat * sinLat);
            double alt = p / Math.Cos(lat) - N;

            // 변환
            double latDeg = lat * 180.0 / Math.PI;
            double lonDeg = lon * 180.0 / Math.PI;

            return (latDeg, lonDeg, alt);
        }

        public static Vector3d FindIntersectionWGS84(Vector3d cameraPos, Vector3d direction)
        {
            double ox = cameraPos.X, oy = cameraPos.Y, oz = cameraPos.Z;
            double dx = direction.X, dy = direction.Y, dz = direction.Z;

            double A = (dx * dx + dy * dy) / (a * a) + (dz * dz) / (b * b);
            double B = 2.0 * ((ox * dx + oy * dy) / (a * a) + (oz * dz) / (b * b));
            double C = (ox * ox + oy * oy) / (a * a) + (oz * oz) / (b * b) - 1.0;

            double discriminant = B * B - 4 * A * C;

            if (discriminant < 0)
            {
                // No intersection
                return Vector3d.Zero;
            }

            double sqrtDisc = Math.Sqrt(discriminant);
            double t1 = (-B - sqrtDisc) / (2 * A);
            double t2 = (-B + sqrtDisc) / (2 * A);

            // 먼저 만나는 점을 획득한다. 카메라에서 레이저가 나가서 지표면과 처음 닿은 지점이 지도 위치임. 더 뒤에 있는 해는 뚫고 나간 곳임
            double t = Math.Min(t1, t2);
            if (t < 0) return Vector3d.Zero;
            return cameraPos + (float)t * direction;
        }
    }
}