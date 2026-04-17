using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace akg1
{
	public class FastTexture
	{
		public readonly int[] Data;
		public readonly int Width, Height;

		public FastTexture(Bitmap bmp)
		{
			Width = bmp.Width; Height = bmp.Height;
			Data = new int[Width * Height];
			var bd = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
			Marshal.Copy(bd.Scan0, Data, 0, Data.Length);
			bmp.UnlockBits(bd);
		}

		public Vector3 Sample(Vector2 uv)
		{
			float u = uv.X - (float)Math.Floor(uv.X);
			float v = 1.0f - (uv.Y - (float)Math.Floor(uv.Y));

			int x = Math.Clamp((int)(u * (Width - 1)), 0, Width - 1);
			int y = Math.Clamp((int)(v * (Height - 1)), 0, Height - 1);

			int color = Data[y * Width + x];
			return new Vector3(
				((color >> 16) & 0xFF) / 255f,
				((color >> 8) & 0xFF) / 255f,
				(color & 0xFF) / 255f);
		}
	}

	public class Renderer
	{
		private Vector3[]? vView, vScreen, vNorm;
		private float[]? zBuffer;

		public FastTexture? DiffuseMap, NormalMap, SpecularMap;

		public Vector3 lightDir = new Vector3(0.5f, 0.5f, 1.0f).Normalize();
		private float ka = 0.2f, kd = 0.8f, ks = 1.0f, shininess = 40.0f;

		public unsafe void Render(Bitmap bmp, ObjParser model, float angX, float angY, float angZ,
								   float posX, float posY, float posZ, float scale, float cameraDist)
		{
			int w = bmp.Width, h = bmp.Height;
			if (model.Vertices.Count == 0) return;

			if (zBuffer == null || zBuffer.Length != w * h)
			{
				zBuffer = new float[w * h];
				vView = new Vector3[model.Vertices.Count];
				vScreen = new Vector3[model.Vertices.Count];
				vNorm = new Vector3[model.Vertices.Count];
			}
			Array.Fill(zBuffer, float.MaxValue);

			Matrix4x4 modelM = Matrix4x4.CreateTranslation(posX, posY, posZ) *
							   Matrix4x4.CreateRotationY(angY) *
							   Matrix4x4.CreateRotationX(angZ) *
							   Matrix4x4.CreateRotationZ(angX) *
							   Matrix4x4.CreateScale(scale, scale, scale);

			Matrix4x4 viewM = Matrix4x4.CreateLookAt(new Vector3(0, 0, cameraDist), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
			Matrix4x4 projM = Matrix4x4.CreatePerspective((float)Math.PI / 4, (float)w / h, 0.1f, 1000f);
			Matrix4x4 viewportM = Matrix4x4.CreateViewport(w, h);

			Matrix4x4 modelView = viewM * modelM;
			Matrix4x4 projViewp = viewportM * projM;

			Parallel.For(0, model.Vertices.Count, i => {
				vView![i] = modelView.MultiplyPoint(model.Vertices[i]);
				vScreen![i] = projViewp.MultiplyPoint(vView[i]);

				// Нормаль в мировом пространстве (для Model Space normal mapping)
				Vector3 n = model.Normals[i];
				float nx = n.X * modelM.M[0, 0] + n.Y * modelM.M[0, 1] + n.Z * modelM.M[0, 2];
				float ny = n.X * modelM.M[1, 0] + n.Y * modelM.M[1, 1] + n.Z * modelM.M[1, 2];
				float nz = n.X * modelM.M[2, 0] + n.Y * modelM.M[2, 1] + n.Z * modelM.M[2, 2];
				vNorm![i] = new Vector3(nx, ny, nz).Normalize();
			});

			BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
			int* ptr = (int*)data.Scan0;
			int stride = data.Stride / 4;

			Parallel.For(0, h, y => {
				int* row = ptr + (y * stride);
				for (int x = 0; x < w; x++) row[x] = 0;
			});

			for (int fIdx = 0; fIdx < model.Faces.Count; fIdx++)
			{
				var face = model.Faces[fIdx];
				var uvFace = model.UVFaces.Count > fIdx ? model.UVFaces[fIdx] : null;

				for (int i = 1; i < face.Length - 1; i++)
				{
					int i0 = face[0], i1 = face[i], i2 = face[i + 1];
					if (vView![i0].Z > -0.1f || vView[i1].Z > -0.1f || vView[i2].Z > -0.1f) continue;

					Vector3 normal = Vector3.Cross(vView[i1] - vView[i0], vView[i2] - vView[i0]);
					if (Vector3.Dot(normal, vView[i0]) > 0) continue;

					Vector2 uv0 = uvFace != null ? model.TexCoords[uvFace[0]] : Vector2.Zero;
					Vector2 uv1 = uvFace != null ? model.TexCoords[uvFace[i]] : Vector2.Zero;
					Vector2 uv2 = uvFace != null ? model.TexCoords[uvFace[i + 1]] : Vector2.Zero;

					FillTriangle(ptr, vScreen![i0], vScreen[i1], vScreen[i2],
								 vView[i0], vView[i1], vView[i2],
								 vNorm![i0], vNorm[i1], vNorm[i2],
								 uv0, uv1, uv2, w, h, stride);
				}
			}
			bmp.UnlockBits(data);
		}

		private unsafe void FillTriangle(int* ptr, Vector3 p0, Vector3 p1, Vector3 p2,
										  Vector3 v0, Vector3 v1, Vector3 v2,
										  Vector3 n0, Vector3 n1, Vector3 n2,
										  Vector2 uv0, Vector2 uv1, Vector2 uv2, int w, int h, int stride)
		{
			if (p0.Y > p1.Y) { (p0, p1) = (p1, p0); (v0, v1) = (v1, v0); (n0, n1) = (n1, n0); (uv0, uv1) = (uv1, uv0); }
			if (p0.Y > p2.Y) { (p0, p2) = (p2, p0); (v0, v2) = (v2, v0); (n0, n2) = (n2, n0); (uv0, uv2) = (uv2, uv0); }
			if (p1.Y > p2.Y) { (p1, p2) = (p2, p1); (v1, v2) = (v2, v1); (n1, n2) = (n2, n1); (uv1, uv2) = (uv2, uv1); }

			float z0 = -v0.Z, z1 = -v1.Z, z2 = -v2.Z;
			Vector3 n_z0 = n0 / z0, n_z1 = n1 / z1, n_z2 = n2 / z2;
			Vector3 v_z0 = v0 / z0, v_z1 = v1 / z1, v_z2 = v2 / z2;
			Vector2 uv_z0 = uv0 / z0, uv_z1 = uv1 / z1, uv_z2 = uv2 / z2;
			float invZ0 = 1f / z0, invZ1 = 1f / z1, invZ2 = 1f / z2;

			int minY = (int)Math.Max(0, Math.Ceiling(p0.Y));
			int maxY = (int)Math.Min(h - 1, Math.Floor(p2.Y));

			for (int y = minY; y <= maxY; y++)
			{
				bool upper = y < p1.Y;
				float t1 = (y - p0.Y) / (p2.Y - p0.Y);
				float t2 = upper ? (y - p0.Y) / (p1.Y - p0.Y) : (y - p1.Y) / (p2.Y - p1.Y);

				Vector3 sA = Vector3.Lerp(p0, p2, t1), sB = upper ? Vector3.Lerp(p0, p1, t2) : Vector3.Lerp(p1, p2, t2);
				Vector3 nZA = Vector3.Lerp(n_z0, n_z2, t1), nZB = upper ? Vector3.Lerp(n_z0, n_z1, t2) : Vector3.Lerp(n_z1, n_z2, t2);
				Vector3 vZA = Vector3.Lerp(v_z0, v_z2, t1), vZB = upper ? Vector3.Lerp(v_z0, v_z1, t2) : Vector3.Lerp(v_z1, v_z2, t2);
				Vector2 uvZA = Vector2.Lerp(uv_z0, uv_z2, t1), uvZB = upper ? Vector2.Lerp(uv_z0, uv_z1, t2) : Vector2.Lerp(uv_z1, uv_z2, t2);
				float izA = Lerp(invZ0, invZ2, t1), izB = upper ? Lerp(invZ0, invZ1, t2) : Lerp(invZ1, invZ2, t2);

				if (sA.X > sB.X) { (sA, sB) = (sB, sA); (nZA, nZB) = (nZB, nZA); (vZA, vZB) = (vZB, vZA); (uvZA, uvZB) = (uvZB, uvZA); (izA, izB) = (izB, izA); }

				int xStart = (int)Math.Max(0, Math.Ceiling(sA.X)), xEnd = (int)Math.Min(w - 1, Math.Floor(sB.X));
				float dx = sB.X - sA.X;

				for (int x = xStart; x <= xEnd; x++)
				{
					float phi = dx > 0 ? (x - sA.X) / dx : 0;
					float zDepth = sA.Z + (sB.Z - sA.Z) * phi;
					if (zDepth < zBuffer![y * w + x])
					{
						zBuffer[y * w + x] = zDepth;
						float z = 1f / (izA + (izB - izA) * phi);
						Vector2 uv = (uvZA + (uvZB - uvZA) * phi) * z;
						Vector3 pos = (vZA + (vZB - vZA) * phi) * z;
						Vector3 norm = (nZA + (nZB - nZA) * phi) * z;
						norm = norm.Normalize();

						if (NormalMap != null)
						{
							// В Model Space нормаль из текстуры заменяет геометрическую
							norm = (NormalMap.Sample(uv) * 2f - new Vector3(1, 1, 1)).Normalize();
						}

						Vector3 color = DiffuseMap != null ? DiffuseMap.Sample(uv) : new Vector3(0.8f, 0.8f, 0.8f);
						float specIntensity = SpecularMap != null ? SpecularMap.Sample(uv).X : 1.0f;

						ptr[y * stride + x] = CalculatePhong(pos, norm, color, specIntensity);
					}
				}
			}
		}

		private int CalculatePhong(Vector3 pos, Vector3 n, Vector3 diffColor, float specIntensity)
		{
			Vector3 viewDir = (new Vector3(0, 0, 0) - pos).Normalize();
			float dotDN = Math.Max(Vector3.Dot(n, lightDir), 0);
			float ambient = ka;
			float diffuse = kd * dotDN;
			Vector3 reflectDir = (n * (2.0f * dotDN) - lightDir).Normalize();
			float specular = ks * (float)Math.Pow(Math.Max(Vector3.Dot(reflectDir, viewDir), 0), shininess) * specIntensity;

			float r = Math.Clamp((ambient + diffuse) * diffColor.X + specular, 0, 1);
			float g = Math.Clamp((ambient + diffuse) * diffColor.Y + specular, 0, 1);
			float b = Math.Clamp((ambient + diffuse) * diffColor.Z + specular, 0, 1);
			return (255 << 24 | (byte)(r * 255) << 16 | (byte)(g * 255) << 8 | (byte)(b * 255));
		}

		private float Lerp(float a, float b, float t) => a + (b - a) * t;
	}
}