using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
// УДАЛЕНО using System.Numerics;

namespace akg1
{
	public class ObjParser
	{
		public List<Vector3> Vertices = new List<Vector3>();
		public List<Vector3> Normals = new List<Vector3>();
		public List<Vector2> TexCoords = new List<Vector2>();
		public List<int[]> Faces = new List<int[]>();
		public List<int[]> UVFaces = new List<int[]>();

		public void Load(string path)
		{
			if (!File.Exists(path)) return;
			Vertices.Clear(); Faces.Clear(); Normals.Clear(); TexCoords.Clear(); UVFaces.Clear();

			foreach (var line in File.ReadLines(path))
			{
				var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2) continue;

				if (parts[0] == "v")
					Vertices.Add(new Vector3(float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture)));
				else if (parts[0] == "vt")
					TexCoords.Add(new Vector2(float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture)));
				else if (parts[0] == "f")
				{
					int[] face = new int[parts.Length - 1];
					int[] uvFace = new int[parts.Length - 1];
					for (int i = 1; i < parts.Length; i++)
					{
						var subParts = parts[i].Split('/');
						face[i - 1] = int.Parse(subParts[0]) - 1;

						if (subParts.Length > 1 && !string.IsNullOrEmpty(subParts[1]))
							uvFace[i - 1] = int.Parse(subParts[1]) - 1;
					}
					Faces.Add(face);
					UVFaces.Add(uvFace);
				}
			}
			ComputeNormals();
		}

		private void ComputeNormals()
		{
			if (Normals.Count > 0) return; // Если в файле уже были нормали vn
			Vector3[] vn = new Vector3[Vertices.Count];
			foreach (var f in Faces)
			{
				Vector3 normal = Vector3.Cross(Vertices[f[1]] - Vertices[f[0]], Vertices[f[2]] - Vertices[f[0]]).Normalize();
				foreach (var idx in f) vn[idx] = vn[idx] + normal;
			}
			foreach (var v in vn) Normals.Add(v.Normalize());
		}
	}
}