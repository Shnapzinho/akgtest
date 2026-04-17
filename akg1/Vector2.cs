using System;

namespace akg1
{
	public struct Vector2
	{
		public float X, Y;

		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		// Сложение векторов
		public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);

		// Вычитание векторов
		public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);

		// Умножение на число (скаляр)
		public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.X * b, a.Y * b);

		// Деление на число
		public static Vector2 operator /(Vector2 a, float b) => new Vector2(a.X / b, a.Y / b);

		// Линейная интерполяция (Lerp) — критически важна для растеризации текстур
		public static Vector2 Lerp(Vector2 v1, Vector2 v2, float t)
		{
			return v1 + (v2 - v1) * t;
		}

		// Нулевой вектор
		public static Vector2 Zero => new Vector2(0, 0);

		public override string ToString() => $"({X}, {Y})";
	}
}