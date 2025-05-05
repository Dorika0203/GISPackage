public static class GeodeticConverter<T> where T : struct, IConvertible
{
    // WGS84 constants
    private static readonly double a = 6378137.0;                 // 반장축
    private static readonly double f = 1 / 298.257223563;         // 편평률
    private static readonly double e2 = f * (2 - f);              // 이심률 제곱
    private static readonly double b = a * (1 - f);               // 반단축

    // LLA to ECEF
    public static (T X, T Y, T Z) LLAtoECEF(T latDeg, T lonDeg, T altMeters)
    {
        double lat = ToDouble(latDeg) * Math.PI / 180.0;
        double lon = ToDouble(lonDeg) * Math.PI / 180.0;
        double alt = ToDouble(altMeters);

        double sinLat = Math.Sin(lat);
        double cosLat = Math.Cos(lat);
        double sinLon = Math.Sin(lon);
        double cosLon = Math.Cos(lon);

        double N = a / Math.Sqrt(1 - e2 * sinLat * sinLat);

        double x = (N + alt) * cosLat * cosLon;
        double y = (N + alt) * cosLat * sinLon;
        double z = (N * (1 - e2) + alt) * sinLat;

        return (FromDouble<T>(x), FromDouble<T>(y), FromDouble<T>(z));
    }

    // ECEF to LLA
    public static (T LatitudeDeg, T LongitudeDeg, T AltitudeMeters) ECEFtoLLA(T xT, T yT, T zT)
    {
        double x = ToDouble(xT);
        double y = ToDouble(yT);
        double z = ToDouble(zT);

        double eps = 1e-11;
        double ePrime2 = (a * a - b * b) / (b * b);
        double p = Math.Sqrt(x * x + y * y);
        double theta = Math.Atan2(z * a, p * b);

        double sinTheta = Math.Sin(theta);
        double cosTheta = Math.Cos(theta);

        double lat = Math.Atan2(z + ePrime2 * b * sinTheta * sinTheta * sinTheta,
                                p - e2 * a * cosTheta * cosTheta * cosTheta);

        double lon = Math.Atan2(y, x);

        double sinLat = Math.Sin(lat);
        double N = a / Math.Sqrt(1 - e2 * sinLat * sinLat);
        double alt = p / Math.Cos(lat) - N;

        // 변환
        double latDeg = lat * 180.0 / Math.PI;
        double lonDeg = lon * 180.0 / Math.PI;

        return (FromDouble<T>(latDeg), FromDouble<T>(lonDeg), FromDouble<T>(alt));
    }

    // Helpers
    private static double ToDouble(T value) => Convert.ToDouble(value);
    private static U FromDouble<U>(double value) where U : IConvertible => (U)Convert.ChangeType(value, typeof(U));
}