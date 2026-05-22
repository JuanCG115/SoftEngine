using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SoftEngine
{
    public partial class Form1 : Form
    {
        private Device device;
        private Mesh[] meshes;
        private Camera camera = new Camera();
        private Texture currentTexture; 

        private ToolStrip mainToolStrip;
        private ToolStripLabel menuLabel;
        private ToolStripButton btnTexture1;
        private ToolStripButton btnTexture2;
        private ToolStripButton btnTexture3;

        public Form1()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            this.Width = 656;
            this.Height = 540;
            this.Text = "SoftEngine 3D";
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeTextureMenu();

            device = new Device(640, 480);

            camera.Position = new SharpDX.Vector3(0, 0, 15.0f);
            camera.Target = SharpDX.Vector3.Zero;

            try
            {
                meshes = new Mesh[] { device.LoadOBJFromFile("monkey.obj") };

                currentTexture = new Texture("texture1.png");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al inicializar los recursos: " + ex.Message);
                meshes = new Mesh[0];
            }

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
            Application.Idle += CompositionTarget_Rendering;
        }

        private void InitializeTextureMenu()
        {
            mainToolStrip = new ToolStrip();
            mainToolStrip.BackColor = Color.FromArgb(240, 240, 240);

            menuLabel = new ToolStripLabel(" Select Texture: ");
            menuLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            btnTexture1 = new ToolStripButton("Texture 1 (Default)");
            btnTexture1.Click += (s, e) => ChangeTexture("texture1.png");

            btnTexture2 = new ToolStripButton("Texture 2 (Pattern 2)");
            btnTexture2.Click += (s, e) => ChangeTexture("texture2.png");

            btnTexture3 = new ToolStripButton("Texture 3 (Pattern 3)");
            btnTexture3.Click += (s, e) => ChangeTexture("texture3.png");

            mainToolStrip.Items.Add(menuLabel);
            mainToolStrip.Items.Add(new ToolStripSeparator());
            mainToolStrip.Items.Add(btnTexture1);
            mainToolStrip.Items.Add(btnTexture2);
            mainToolStrip.Items.Add(btnTexture3);

            this.Controls.Add(mainToolStrip);
        }

        private void ChangeTexture(string filename)
        {
            try
            {
                currentTexture = new Texture(filename);
                this.Focus(); 
                this.Invalidate(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load texture '{filename}': " + ex.Message);
            }
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs? e)
        {
            this.Invalidate();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            float cameraSpeed = 0.5f;
            float rotationSpeed = 0.05f;

            if (e.KeyCode == Keys.W)
                camera.Position = new SharpDX.Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z - cameraSpeed);
            if (e.KeyCode == Keys.S)
                camera.Position = new SharpDX.Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z + cameraSpeed);

            foreach (var mesh in meshes)
            {
                if (e.KeyCode == Keys.Left)
                    mesh.Rotation = new SharpDX.Vector3(mesh.Rotation.X, mesh.Rotation.Y - rotationSpeed, mesh.Rotation.Z);
                if (e.KeyCode == Keys.Right)
                    mesh.Rotation = new SharpDX.Vector3(mesh.Rotation.X, mesh.Rotation.Y + rotationSpeed, mesh.Rotation.Z);
                if (e.KeyCode == Keys.Up)
                    mesh.Rotation = new SharpDX.Vector3(mesh.Rotation.X - rotationSpeed, mesh.Rotation.Y, mesh.Rotation.Z);
                if (e.KeyCode == Keys.Down)
                    mesh.Rotation = new SharpDX.Vector3(mesh.Rotation.X + rotationSpeed, mesh.Rotation.Y, mesh.Rotation.Z);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            device.Clear(40, 40, 40, 255);

            device.Render(camera, meshes, currentTexture);

            byte[] buffer = device.GetBuffer();
            using (Bitmap bmp = new Bitmap(640, 480, PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                                                  ImageLockMode.WriteOnly, bmp.PixelFormat);

                Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                bmp.UnlockBits(bmpData);

                int topOffset = mainToolStrip.Height;
                e.Graphics.DrawImage(bmp, 8, topOffset);
            }
        }
    }
}