using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace akg1
{
	public class ObjParser
	{
		public List<Vector3> Vertices = new List<Vector3>();
		public List<Vector3> Normals = new List<Vector3>();
		public List<Vector2> TextureCoords = new List<Vector2>();

		public List<int[]> Faces = new List<int[]>();
		public List<int[]> UVFaces = new List<int[]>();

		public void Load(string path)
		{
			// 1. Проверяем, существует ли файл
			if (!File.Exists(path))
			{
				System.Windows.Forms.MessageBox.Show("Файл не найден по пути: " + Path.GetFullPath(path));
				return;
			}

			Vertices.Clear(); Faces.Clear(); Normals.Clear();
			TextureCoords.Clear(); UVFaces.Clear();

			// 2. Читаем файл построчно
			foreach (var line in File.ReadLines(path))
			{
				string trimmed = line.Trim();
				if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

				// Разделяем по пробелам или табуляциям
				var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2) continue;

				if (parts[0] == "v") // Вершина
				{
					Vertices.Add(new Vector3(
						float.Parse(parts[1], CultureInfo.InvariantCulture),
						float.Parse(parts[2], CultureInfo.InvariantCulture),
						float.Parse(parts[3], CultureInfo.InvariantCulture)));
				}
				else if (parts[0] == "vt") // Текстурная координата
				{
					TextureCoords.Add(new Vector2(
						float.Parse(parts[1], CultureInfo.InvariantCulture),
						float.Parse(parts[2], CultureInfo.InvariantCulture)));
				}
				else if (parts[0] == "f") // Грань
				{
					int[] face = new int[parts.Length - 1];
					int[] uvFace = new int[parts.Length - 1];

					for (int i = 1; i < parts.Length; i++)
					{
						var subParts = parts[i].Split('/');

						// Индекс вершины
						int vIdx = int.Parse(subParts[0]);
						face[i - 1] = vIdx > 0 ? vIdx - 1 : Vertices.Count + vIdx;

						// Индекс текстуры (если есть)
						if (subParts.Length > 1 && !string.IsNullOrEmpty(subParts[1]))
						{
							int vtIdx = int.Parse(subParts[1]);
							uvFace[i - 1] = vtIdx > 0 ? vtIdx - 1 : TextureCoords.Count + vtIdx;
						}
					}
					Faces.Add(face);
					UVFaces.Add(uvFace);
				}
			}

			ComputeNormals();
		}

		private void ComputeNormals()
		{
			if (Vertices.Count == 0) return;
			Vector3[] vn = new Vector3[Vertices.Count];
			foreach (var f in Faces)
			{
				if (f.Length < 3) continue;
				Vector3 v0 = Vertices[f[0]], v1 = Vertices[f[1]], v2 = Vertices[f[2]];
				Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).Normalize();
				foreach (var idx in f) if (idx >= 0 && idx < vn.Length) vn[idx] = vn[idx] + normal;
			}
			foreach (var v in vn) Normals.Add(v.Normalize());
		}
	}
}