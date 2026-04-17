using System;

namespace akg1
{
	public struct Vector3
	{
		public float X, Y, Z;
		public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }

		public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.X * b, a.Y * b, a.Z * b);

		// Добавь эти две строки:
		public static Vector3 operator *(float b, Vector3 a) => a * b;
		public static Vector3 operator /(Vector3 a, float b) => new Vector3(a.X / b, a.Y / b, a.Z / b);

		public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(
			a.Y * b.Z - a.Z * b.Y,
			a.Z * b.X - a.X * b.Z,
			a.X * b.Y - a.Y * b.X
		);

		public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
		public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

		public Vector3 Normalize()
		{
			float len = Length();
			return len > 1e-6f ? new Vector3(X / len, Y / len, Z / len) : new Vector3(0, 0, 0);
		}

		public static Vector3 Lerp(Vector3 v1, Vector3 v2, float t) => v1 + (v2 - v1) * t;
	}
}