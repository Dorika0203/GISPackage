using OpenTK.Mathematics;

namespace GISControlWPFGL2
{
    internal static class OpenTKExtension
    {
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
    }
}
