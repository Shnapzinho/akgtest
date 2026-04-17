using System;

namespace akg1
{
	public class Matrix4x4
	{
		public float[,] M = new float[4, 4];

		public static Matrix4x4 Identity()
		{
			var res = new Matrix4x4();
			for (int i = 0; i < 4; i++) res.M[i, i] = 1;
			return res;
		}

		public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
		{
			var res = new Matrix4x4();
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < 4; j++)
					for (int k = 0; k < 4; k++)
						res.M[i, j] += a.M[i, k] * b.M[k, j];
			return res;
		}

		public Vector3 MultiplyPoint(Vector3 v)
		{
			float x = v.X * M[0, 0] + v.Y * M[0, 1] + v.Z * M[0, 2] + M[0, 3];
			float y = v.X * M[1, 0] + v.Y * M[1, 1] + v.Z * M[1, 2] + M[1, 3];
			float z = v.X * M[2, 0] + v.Y * M[2, 1] + v.Z * M[2, 2] + M[2, 3];
			float w = v.X * M[3, 0] + v.Y * M[3, 1] + v.Z * M[3, 2] + M[3, 3];
			return new Vector3(x / w, y / w, z / w);
		}

		public static Matrix4x4 CreateTranslation(float tx, float ty, float tz)
		{
			var m = Identity();
			m.M[0, 3] = tx; m.M[1, 3] = ty; m.M[2, 3] = tz;
			return m;
		}

		public static Matrix4x4 CreateScale(float sx, float sy, float sz)
		{
			var m = Identity();
			m.M[0, 0] = sx; m.M[1, 1] = sy; m.M[2, 2] = sz;
			return m;
		}

		public static Matrix4x4 CreateRotationX(float angle)
		{
			var m = Identity();
			float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
			m.M[1, 1] = c; m.M[1, 2] = -s;
			m.M[2, 1] = s; m.M[2, 2] = c;
			return m;
		}

		public static Matrix4x4 CreateRotationY(float angle)
		{
			var m = Identity();
			float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
			m.M[0, 0] = c; m.M[0, 2] = s;
			m.M[2, 0] = -s; m.M[2, 2] = c;
			return m;
		}

		public static Matrix4x4 CreateRotationZ(float angle)
		{
			var m = Identity();
			float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
			m.M[0, 0] = c; m.M[0, 1] = -s;
			m.M[1, 0] = s; m.M[1, 1] = c;
			return m;
		}

		public static Matrix4x4 CreateLookAt(Vector3 eye, Vector3 target, Vector3 up)
		{
			Vector3 zAxis = (eye - target).Normalize();
			Vector3 xAxis = Vector3.Cross(up, zAxis).Normalize();
			Vector3 yAxis = Vector3.Cross(zAxis, xAxis);
			var m = Identity();
			m.M[0, 0] = xAxis.X; m.M[0, 1] = xAxis.Y; m.M[0, 2] = xAxis.Z; m.M[0, 3] = -Vector3.Dot(xAxis, eye);
			m.M[1, 0] = yAxis.X; m.M[1, 1] = yAxis.Y; m.M[1, 2] = yAxis.Z; m.M[1, 3] = -Vector3.Dot(yAxis, eye);
			m.M[2, 0] = zAxis.X; m.M[2, 1] = zAxis.Y; m.M[2, 2] = zAxis.Z; m.M[2, 3] = -Vector3.Dot(zAxis, eye);
			return m;
		}

		public static Matrix4x4 CreatePerspective(float fov, float aspect, float znear, float zfar)
		{
			var m = new Matrix4x4();
			float tanHalfFov = (float)Math.Tan(fov / 2);
			m.M[0, 0] = 1 / (aspect * tanHalfFov);
			m.M[1, 1] = 1 / tanHalfFov;
			m.M[2, 2] = zfar / (znear - zfar);
			m.M[2, 3] = (znear * zfar) / (znear - zfar);
			m.M[3, 2] = -1;
			return m;
		}

		public static Matrix4x4 CreateViewport(float width, float height)
		{
			var m = Identity();
			m.M[0, 0] = width / 2; m.M[0, 3] = width / 2;
			m.M[1, 1] = -height / 2; m.M[1, 3] = height / 2;
			return m;
		}
	}
}