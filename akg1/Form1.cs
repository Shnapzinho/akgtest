using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace akg1
{
	public partial class Form1 : Form
	{
		ObjParser model = new ObjParser();
		Renderer renderer = new Renderer();

		float rotX = 0, rotY = 0, rotZ = 0;
		float posX = 0, posY = 0, posZ = 0;
		float modelScale = 1.0f;
		float cameraDist = 5.0f;

		Bitmap backBuffer;
		Stopwatch sw = new Stopwatch();
		bool isRendering = false;

		public Form1()
		{
			InitializeComponent();
			this.DoubleBuffered = true;
			this.KeyPreview = true;
			this.WindowState = FormWindowState.Maximized;

			if (File.Exists("GasTank.obj"))
				model.Load("GasTank.obj");

			// 2. Загружаем текстуры (убедитесь, что файлы лежат в папке с .exe или укажите полный путь)
			try
			{
				if (File.Exists("diffuse.jpg"))
					renderer.DiffuseMap = new Bitmap("diffuse.jpg");

				if (File.Exists("normal.jpg"))
					renderer.NormalMap = new Bitmap("normal.jpg");

				if (File.Exists("specular.jpg"))
					renderer.SpecularMap = new Bitmap("specular.jpg");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при загрузке текстур: " + ex.Message);
			}

			backBuffer = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
			this.KeyDown += Form1_KeyDown;
			this.MouseWheel += (s, e) => cameraDist = Math.Clamp(cameraDist - e.Delta * 0.01f, 0.1f, 100f);

			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 16 };
			timer.Tick += (s, e) => {
				if (isRendering) return;
				isRendering = true;
				sw.Restart();

				if (backBuffer.Width != pictureBox1.Width || backBuffer.Height != pictureBox1.Height)
					backBuffer = new Bitmap(pictureBox1.Width, pictureBox1.Height);

				renderer.Render(backBuffer, model, rotX, rotY, rotZ, posX, posY, posZ, modelScale, cameraDist);

				sw.Stop();
				using (Graphics g = Graphics.FromImage(backBuffer))
				{
					g.DrawString($"FPS: {1000f / Math.Max(sw.ElapsedMilliseconds, 1):F1}", new Font("Arial", 12, FontStyle.Bold), Brushes.Yellow, 10, 10);
				}
				// В таймере в Form1.cs:
				this.Text = $"Вершин: {model.Vertices.Count} | FPS: {1000f / sw.ElapsedMilliseconds:F1}";
				pictureBox1.Image = backBuffer;
				isRendering = false;
			};
			timer.Start();
		}

		private void Form1_KeyDown(object? sender, KeyEventArgs e)
		{
			float step = 0.05f;
			if (e.KeyCode == Keys.Escape) this.Close();
			if (e.KeyCode == Keys.W) rotX -= step;
			if (e.KeyCode == Keys.S) rotX += step;
			if (e.KeyCode == Keys.A) rotY -= step;
			if (e.KeyCode == Keys.D) rotY += step;
			if (e.KeyCode == Keys.Q) rotZ -= step;
			if (e.KeyCode == Keys.E) rotZ += step;
			if (e.KeyCode == Keys.I) renderer.lightDir.Y += step;
			if (e.KeyCode == Keys.K) renderer.lightDir.Y -= step;
			if (e.KeyCode == Keys.J) renderer.lightDir.X -= step;
			if (e.KeyCode == Keys.L) renderer.lightDir.X += step;
			if (e.KeyCode == Keys.U) renderer.lightDir.Z += step;
			if (e.KeyCode == Keys.O) renderer.lightDir.Z -= step;
			renderer.lightDir = renderer.lightDir.Normalize();
			if (e.KeyCode == Keys.Left) posX -= step;
			if (e.KeyCode == Keys.Right) posX += step;
			if (e.KeyCode == Keys.Up) posY += step;
			if (e.KeyCode == Keys.Down) posY -= step;
			if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus) modelScale += 0.1f;
			if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus) modelScale = Math.Max(0.1f, modelScale - 0.1f);
			if (e.KeyCode == Keys.R) { rotX = rotY = rotZ = posX = posY = posZ = 0; modelScale = 1f; cameraDist = 5; }
		}
	}
}