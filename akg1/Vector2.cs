namespace akg1
{
	public struct Vector2
	{
		public float X, Y;
		public Vector2(float x, float y) { X = x; Y = y; }

		public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
		public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
		public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.X * b, a.Y * b);

		public static Vector2 Lerp(Vector2 v1, Vector2 v2, float t)
		{
			return v1 + (v2 - v1) * t;
		}
	}
}