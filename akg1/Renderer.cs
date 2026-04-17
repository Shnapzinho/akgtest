using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace akg1
{
	public class Renderer
	{
		private Vector3[]? vView, vScreen, vNorm;
		private Vector2[]? vUV; // Текстурные координаты для каждой вершины
		private float[]? zBuffer;

		// Текстуры
		public Bitmap? DiffuseMap;
		public Bitmap? NormalMap;
		public Bitmap? SpecularMap;

		public Vector3 lightDir = new Vector3(0.5f, 0.5f, 1.0f).Normalize();
		private float ka = 0.3f, kd = 0.8f, ks = 0.5f, shininess = 30.0f;

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
				vUV = new Vector2[model.Vertices.Count];
			}
			Array.Fill(zBuffer, float.MaxValue);

			// Матрицы (используем ваш порядок)
			Matrix4x4 modelM = Matrix4x4.CreateScale(scale, scale, scale) *
				   Matrix4x4.CreateRotationX(angX) *
				   Matrix4x4.CreateRotationY(angY) *
				   Matrix4x4.CreateRotationZ(angZ) *
				   Matrix4x4.CreateTranslation(posX, posY, posZ);

			Matrix4x4 viewM = Matrix4x4.CreateLookAt(new Vector3(0, 0, cameraDist), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
			Matrix4x4 projM = Matrix4x4.CreatePerspective((float)Math.PI / 4, (float)w / h, 0.1f, 1000f);
			Matrix4x4 viewportM = Matrix4x4.CreateViewport(w, h);

			Matrix4x4 modelView = viewM * modelM;
			Matrix4x4 projViewp = viewportM * projM;

			Parallel.For(0, model.Vertices.Count, i => {
				vView![i] = modelView.MultiplyPoint(model.Vertices[i]);
				vScreen![i] = projViewp.MultiplyPoint(vView[i]);
				// Трансформация нормалей в пространство модели/вида (для упрощения - пространство модели)
				Vector3 n = model.Normals[i];
				vNorm![i] = n.Normalize();
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
				var uvFace = model.UVFaces[fIdx];

				for (int i = 1; i < face.Length - 1; i++)
				{
					int i0 = face[0], i1 = face[i], i2 = face[i + 1];
					int uv0 = uvFace[0], uv1 = uvFace[i], uv2 = uvFace[i + 1];

					if (vView![i0].Z > -0.1f || vView[i1].Z > -0.1f || vView[i2].Z > -0.1f) continue;

					FillTriangle(ptr, vScreen![i0], vScreen[i1], vScreen[i2],
								 vView[i0], vView[i1], vView[i2],
								 model.TextureCoords[uv0], model.TextureCoords[uv1], model.TextureCoords[uv2],
								 w, h, stride);
				}
			}
			bmp.UnlockBits(data);
		}

		private unsafe void FillTriangle(int* ptr, Vector3 p0, Vector3 p1, Vector3 p2,
										  Vector3 v0, Vector3 v1, Vector3 v2,
										  Vector2 uv0, Vector2 uv1, Vector2 uv2,
										  int w, int h, int stride)
		{
			// Сортировка по Y
			if (p0.Y > p1.Y) { (p0, p1) = (p1, p0); (v0, v1) = (v1, v0); (uv0, uv1) = (uv1, uv0); }
			if (p0.Y > p2.Y) { (p0, p2) = (p2, p0); (v0, v2) = (v2, v0); (uv0, uv2) = (uv2, uv0); }
			if (p1.Y > p2.Y) { (p1, p2) = (p2, p1); (v1, v2) = (v2, v1); (uv1, uv2) = (uv2, uv1); }

			int minY = (int)Math.Max(0, Math.Ceiling(p0.Y));
			int maxY = (int)Math.Min(h - 1, Math.Floor(p2.Y));

			// Для перспективной коррекции (формула 4.3): подготавливаем атрибуты / Z
			// Используем Z из vView (глубина в пространстве вида)
			float z0 = Math.Abs(v0.Z), z1 = Math.Abs(v1.Z), z2 = Math.Abs(v2.Z);

			for (int y = minY; y <= maxY; y++)
			{
				bool upper = y < p1.Y;
				float t1 = (y - p0.Y) / (p2.Y - p0.Y);
				float t2 = upper ? (y - p0.Y) / (p1.Y - p0.Y) : (y - p1.Y) / (p2.Y - p1.Y);

				Vector3 sA = Vector3.Lerp(p0, p2, t1), sB = upper ? Vector3.Lerp(p0, p1, t2) : Vector3.Lerp(p1, p2, t2);

				// Интерполяция 1/Z и UV/Z для коррекции
				float invZA = (1f / z0) + (1f / z2 - 1f / z0) * t1;
				float invZB = upper ? (1f / z0 + (1f / z1 - 1f / z0) * t2) : (1f / z1 + (1f / z2 - 1f / z1) * t2);

				Vector2 uvZA = (uv0 * (1f / z0)) + (uv2 * (1f / z2) - uv0 * (1f / z0)) * t1;
				Vector2 uvZB = upper ? (uv0 * (1f / z0) + (uv1 * (1f / z1) - uv0 * (1f / z0)) * t2) : (uv1 * (1f / z1) + (uv2 * (1f / z2) - uv1 * (1f / z1)) * t2);

				if (sA.X > sB.X) { (sA, sB) = (sB, sA); (invZA, invZB) = (invZB, invZA); (uvZA, uvZB) = (uvZB, uvZA); }

				int xStart = (int)Math.Max(0, Math.Ceiling(sA.X)), xEnd = (int)Math.Min(w - 1, Math.Floor(sB.X));
				for (int x = xStart; x <= xEnd; x++)
				{
					float phi = (sB.X > sA.X) ? (x - sA.X) / (sB.X - sA.X) : 0;
					float zS = sA.Z + (sB.Z - sA.Z) * phi;

					if (zS < zBuffer![y * w + x])
					{
						zBuffer[y * w + x] = zS;

						float currentInvZ = invZA + (invZB - invZA) * phi;
						Vector2 currentUVZ = uvZA + (uvZB - uvZA) * phi;
						Vector2 uv = currentUVZ * (1f / currentInvZ);

						// ИСПРАВЛЕНИЕ 1: Интерполируем позицию пикселя в пространстве вида
						Vector3 currentPos = vA + (vB - vA) * phi;
						// ИСПРАВЛЕНИЕ 2: Интерполируем нормаль из вершин (для правильного объема)
						Vector3 currentNorm = nA + (nB - nA) * phi;

						ptr[y * stride + x] = CalculatePhongWithTextures(uv, currentPos, currentNorm.Normalize());
					}
				}
			}
		}

		private int CalculatePhongWithTextures(Vector2 uv, Vector3 pos, Vector3 vertexNormal)
		{
			float u = Math.Clamp(uv.X, 0, 0.999f);
			float v = Math.Clamp(1f - uv.Y, 0, 0.999f);

			Vector3 texColor = SampleTexture(DiffuseMap, u, v, new Vector3(0.5f, 0.5f, 0.5f));
			float texSpec = SampleTexture(SpecularMap, u, v, new Vector3(0.5f, 0.5f, 0.5f)).X;

			// ИСПРАВЛЕНИЕ 3: Используем интерполированную нормаль вершин для объема
			// (Карту нормалей пока проигнорируем, так как она другого формата)
			Vector3 n = vertexNormal;

			Vector3 viewDir = (new Vector3(0, 0, 0) - pos).Normalize();
			float dotDN = Math.Max(Vector3.Dot(n, lightDir), 0);

			float ambient = ka;
			float diffuse = kd * dotDN;

			Vector3 reflectDir = (n * (2.0f * dotDN) - lightDir).Normalize();
			float specFactor = (float)Math.Pow(Math.Max(Vector3.Dot(reflectDir, viewDir), 0), shininess);
			float specular = (ks * texSpec) * specFactor;

			// ИСПРАВЛЕНИЕ 4: Чуть больше яркости для диффузки, чтобы логотип горел
			float r = Math.Clamp((ambient + diffuse) * texColor.X + specular, 0, 1);
			float g = Math.Clamp((ambient + diffuse) * texColor.Y + specular, 0, 1);
			float b = Math.Clamp((ambient + diffuse) * texColor.Z + specular, 0, 1);

			return (255 << 24 | (byte)(r * 255) << 16 | (byte)(g * 255) << 8 | (byte)(b * 255));
		}

		private unsafe Vector3 SampleTexture(Bitmap? map, float u, float v, Vector3 defaultColor)
		{
			if (map == null) return defaultColor;

			// Защита от выхода за границы и пересчет в координаты пикселей
			int x = (int)(u * (map.Width - 1));
			int y = (int)(v * (map.Height - 1));

			x = Math.Clamp(x, 0, map.Width - 1);
			y = Math.Clamp(y, 0, map.Height - 1);

			Color c = map.GetPixel(x, y);
			return new Vector3(c.R / 255f, c.G / 255f, c.B / 255f);
		}
	}
}