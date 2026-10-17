using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using PointF = System.Drawing.PointF;
using Rectangle = System.Drawing.Rectangle;

[assembly: AssemblyTitle("Frame Bounce")]
[assembly: AssemblyProduct("Frame Bounce")]

namespace FrameBounce
{
    internal static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr window, out Rectangle rectangle);

        [STAThread]
        private static void Main(string[] args)
        {
            // Read physical monitor bounds and use those same pixels for the scene and collisions.
            SetProcessDpiAwarenessContext(new IntPtr(-4));
            Forms.Application.EnableVisualStyles();
            string mode = args.Length == 0 ? "/s" : args[0].ToLowerInvariant();
            if (mode.StartsWith("/c") || mode.StartsWith("-c"))
            {
                using (SettingsForm settings = new SettingsForm()) settings.ShowDialog();
                return;
            }
            if (mode.StartsWith("/p") || mode.StartsWith("-p"))
            {
                string value = mode.Length > 2 ? mode.Substring(2).TrimStart(':') :
                    (args.Length > 1 ? args[1] : "");
                long number;
                if (!long.TryParse(value, out number)) return;
                IntPtr parent = new IntPtr(number);
                Rectangle bounds;
                if (!GetClientRect(parent, out bounds)) return;
                using (SaverForm form = new SaverForm(true))
                {
                    form.TopLevel = false;
                    form.Bounds = new Rectangle(0, 0, bounds.Width, bounds.Height);
                    SetParent(form.Handle, parent);
                    form.Show();
                    Forms.Application.Run(form);
                }
                return;
            }
            if (!mode.StartsWith("/s") && !mode.StartsWith("-s")) return;
            Forms.Screen[] monitors = Forms.Screen.AllScreens;
            SaverForm[] forms = new SaverForm[monitors.Length];
            for (int i = 0; i < monitors.Length; i++)
            {
                forms[i] = new SaverForm(false);
                forms[i].StartPosition = Forms.FormStartPosition.Manual;
                Rectangle display = monitors[i].Bounds;
                forms[i].Bounds = new Rectangle(display.Left - 4, display.Top - 4,
                    display.Width + 8, display.Height + 8);
                forms[i].Show();
            }
            Forms.Cursor.Hide();
            Forms.Application.Run(forms[0]);
            Forms.Cursor.Show();
        }
    }

    internal sealed class SaverSettings
    {
        public const double BaseSizeScale = 0.70;
        public int SizePercent = 100;
        public int SpeedPercent = 100;
        public int BrightnessPercent = 50;

        public static SaverSettings Load()
        {
            SaverSettings settings = new SaverSettings();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\FrameBounce") ??
                Registry.CurrentUser.OpenSubKey(@"Software\VRBounce"))
            {
                if (key == null) return settings;
                object version = key.GetValue("SettingsVersion");
                if (!(version is int) || (int)version < 2) return settings;
                object size = key.GetValue("SizePercent");
                object speed = key.GetValue("SpeedPercent");
                object brightness = key.GetValue("BrightnessPercent");
                if (size is int)
                {
                    int sizePercent = (int)size;
                    if ((int)version < 3)
                        sizePercent = (int)Math.Round(sizePercent / BaseSizeScale,
                            MidpointRounding.AwayFromZero);
                    settings.SizePercent = Math.Max(50, Math.Min(600, sizePercent));
                }
                if (speed is int) settings.SpeedPercent = Math.Max(25, Math.Min(800, (int)speed));
                if (brightness is int) settings.BrightnessPercent = Math.Max(0, Math.Min(100, (int)brightness));
            }
            return settings;
        }

        public void Save()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\FrameBounce"))
            {
                key.SetValue("SizePercent", SizePercent, RegistryValueKind.DWord);
                key.SetValue("SpeedPercent", SpeedPercent, RegistryValueKind.DWord);
                key.SetValue("BrightnessPercent", BrightnessPercent, RegistryValueKind.DWord);
                key.SetValue("SettingsVersion", 3, RegistryValueKind.DWord);
            }
        }
    }

    internal sealed class SettingsForm : Forms.Form
    {
        private readonly Forms.TrackBar sizeSlider;
        private readonly Forms.TrackBar speedSlider;
        private readonly Forms.TrackBar brightnessSlider;
        private readonly Forms.Label sizeValue;
        private readonly Forms.Label speedValue;
        private readonly Forms.Label brightnessValue;

        public SettingsForm()
        {
            SaverSettings saved = SaverSettings.Load();
            Text = "Frame Bounce Settings";
            FormBorderStyle = Forms.FormBorderStyle.FixedDialog;
            StartPosition = Forms.FormStartPosition.CenterScreen;
            ClientSize = new System.Drawing.Size(480, 490);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            AutoScaleMode = Forms.AutoScaleMode.Dpi;

            Forms.Label sizeLabel = new Forms.Label { Text = "Object size", Left = 24, Top = 20, Width = 320 };
            sizeValue = new Forms.Label { Left = 376, Top = 20, Width = 80, TextAlign = System.Drawing.ContentAlignment.TopRight };
            sizeSlider = new Forms.TrackBar { Left = 20, Top = 48, Width = 440,
                AutoSize = false, Height = 54,
                Minimum = 50, Maximum = 600, TickFrequency = 50, SmallChange = 10,
                LargeChange = 25, Value = saved.SizePercent };
            Forms.Label sizeHint = new Forms.Label { Text = "100% is the default size", Left = 24, Top = 111, Width = 432 };
            Forms.Label firstDivider = new Forms.Label { Left = 24, Top = 141, Width = 432, Height = 2,
                BorderStyle = Forms.BorderStyle.Fixed3D };

            Forms.Label speedLabel = new Forms.Label { Text = "Movement and rotation speed", Left = 24, Top = 163, Width = 340 };
            speedValue = new Forms.Label { Left = 376, Top = 163, Width = 80, TextAlign = System.Drawing.ContentAlignment.TopRight };
            speedSlider = new Forms.TrackBar { Left = 20, Top = 191, Width = 440,
                AutoSize = false, Height = 54,
                Minimum = 25, Maximum = 800, TickFrequency = 100, SmallChange = 10,
                LargeChange = 25, Value = saved.SpeedPercent };
            Forms.Label speedHint = new Forms.Label { Text = "100% is the default speed", Left = 24, Top = 254, Width = 432 };
            Forms.Label secondDivider = new Forms.Label { Left = 24, Top = 284, Width = 432, Height = 2,
                BorderStyle = Forms.BorderStyle.Fixed3D };

            Forms.Label brightnessLabel = new Forms.Label { Text = "Object brightness", Left = 24, Top = 306, Width = 320 };
            brightnessValue = new Forms.Label { Left = 376, Top = 306, Width = 80,
                TextAlign = System.Drawing.ContentAlignment.TopRight };
            brightnessSlider = new Forms.TrackBar { Left = 20, Top = 334, Width = 440,
                AutoSize = false, Height = 54,
                Minimum = 0, Maximum = 100, TickFrequency = 10, SmallChange = 5,
                LargeChange = 10, Value = saved.BrightnessPercent };
            Forms.Label brightnessHint = new Forms.Label { Text = "50% is the default brightness", Left = 24, Top = 397, Width = 432 };

            Forms.Button defaults = new Forms.Button { Text = "Defaults", Left = 24, Top = 442, Width = 100, Height = 32 };
            Forms.Button cancel = new Forms.Button { Text = "Cancel", Left = 252, Top = 442,
                Width = 108, Height = 32, DialogResult = Forms.DialogResult.Cancel };
            Forms.Button ok = new Forms.Button { Text = "OK", Left = 376, Top = 442, Width = 80, Height = 32 };
            sizeSlider.ValueChanged += delegate { UpdateValues(); };
            speedSlider.ValueChanged += delegate { UpdateValues(); };
            brightnessSlider.ValueChanged += delegate { UpdateValues(); };
            defaults.Click += delegate { sizeSlider.Value = 100; speedSlider.Value = 100;
                brightnessSlider.Value = 50; };
            ok.Click += delegate
            {
                new SaverSettings { SizePercent = sizeSlider.Value,
                    SpeedPercent = speedSlider.Value,
                    BrightnessPercent = brightnessSlider.Value }.Save();
                DialogResult = Forms.DialogResult.OK;
                Close();
            };
            Controls.AddRange(new Forms.Control[] { sizeLabel, sizeValue, sizeSlider, sizeHint, firstDivider,
                speedLabel, speedValue, speedSlider, speedHint, secondDivider, brightnessLabel, brightnessValue,
                brightnessSlider, brightnessHint, defaults, cancel, ok });
            AcceptButton = ok;
            CancelButton = cancel;
            UpdateValues();
            ActiveControl = ok;
        }

        private void UpdateValues()
        {
            sizeValue.Text = sizeSlider.Value + "%";
            speedValue.Text = speedSlider.Value + "%";
            brightnessValue.Text = brightnessSlider.Value + "%";
        }
    }

    internal sealed class Body
    {
        public struct EdgePoint
        {
            public double X, Y, Nx, Ny;
            public EdgePoint(double x, double y, double nx, double ny)
            { X = x; Y = y; Nx = nx; Ny = ny; }
        }

        public BitmapSource Source;
        public byte[] Alpha;
        public int SourceWidth, SourceHeight;
        public List<PointF> Contour = new List<PointF>();
        public List<EdgePoint> Boundary = new List<EdgePoint>();
        public Image Visual;
        public RotateTransform Rotation;
        public TranslateTransform Position;
        public double X, Y, Vx, Vy, Angle, Spin, Cos, Sin;
        public double Scale, CenterX, CenterY, Mass, InverseMass, InverseInertia;
        public double Width { get { return SourceWidth * Scale; } }
        public double Height { get { return SourceHeight * Scale; } }

        public void UpdateTrig()
        {
            double radians = Angle * Math.PI / 180.0;
            Cos = Math.Cos(radians);
            Sin = Math.Sin(radians);
        }

        public bool Hit(double x, double y)
        {
            double dx = x - X, dy = y - Y;
            int sx = (int)Math.Floor((dx * Cos + dy * Sin) / Scale + CenterX);
            int sy = (int)Math.Floor((-dx * Sin + dy * Cos) / Scale + CenterY);
            return sx >= 0 && sy >= 0 && sx < SourceWidth && sy < SourceHeight &&
                Alpha[sy * SourceWidth + sx] >= 128;
        }

        private static double Cross(PointF origin, PointF a, PointF b)
        {
            return ((double)a.X - origin.X) * (b.Y - origin.Y) -
                ((double)a.Y - origin.Y) * (b.X - origin.X);
        }

        public void CompactContour()
        {
            if (Contour.Count < 3) return;
            Contour.Sort(delegate(PointF a, PointF b)
            {
                int xOrder = a.X.CompareTo(b.X);
                return xOrder != 0 ? xOrder : a.Y.CompareTo(b.Y);
            });
            List<PointF> hull = new List<PointF>();
            foreach (PointF point in Contour)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            int lowerCount = hull.Count;
            for (int i = Contour.Count - 2; i >= 0; i--)
            {
                PointF point = Contour[i];
                while (hull.Count > lowerCount && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            Contour = hull;
        }

        public void VisibleBounds(out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (PointF point in Contour)
            {
                double x = X + Scale * (point.X * Cos - point.Y * Sin);
                double y = Y + Scale * (point.X * Sin + point.Y * Cos);
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            minX -= 0.5; minY -= 0.5; maxX += 0.5; maxY += 0.5;
        }
    }

    internal sealed class SaverForm : Forms.Form
    {
        private const int EdgePadding = 4;
        private readonly bool preview;
        private readonly SaverSettings settings;
        private readonly ElementHost host;
        private readonly Canvas root;
        private readonly Canvas scene;
        private readonly Body[] bodies = new Body[3];
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly double[] oldX = new double[3], oldY = new double[3], oldAngle = new double[3];
        private double startingEnergy;
        private double lastTime, worldWidth, worldHeight, leftWall;
        private bool initialized, mouseArmed;
        private System.Drawing.Point initialMouse;

        public SaverForm(bool isPreview) : this(isPreview, SaverSettings.Load())
        {
        }

        internal SaverForm(bool isPreview, SaverSettings renderSettings)
        {
            preview = isPreview;
            settings = renderSettings;
            FormBorderStyle = Forms.FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = !preview;
            BackColor = System.Drawing.Color.Black;
            root = new Canvas { Background = Brushes.Black, Focusable = true, ClipToBounds = true };
            scene = new Canvas { Background = Brushes.Black, ClipToBounds = true };
            root.Children.Add(scene);
            host = new ElementHost { Dock = Forms.DockStyle.Fill, Child = root,
                BackColor = System.Drawing.Color.Black };
            Controls.Add(host);
            // 100% is half the original motion rate for both travel and rotation.
            double speed = settings.SpeedPercent / 200.0;
            double scale = SaverSettings.BaseSizeScale * settings.SizePercent / 100.0;
            bodies[0] = ReadBody("left-controller.png", 105 * speed, -84 * speed, 19 * speed, scale);
            bodies[1] = ReadBody("headset.png", -114 * speed, 77 * speed, -13 * speed, scale);
            bodies[2] = ReadBody("right-controller.png", -97 * speed, -90 * speed, -22 * speed, scale);
            startingEnergy = TotalEnergy();
            foreach (Body b in bodies)
            {
                b.Visual = new Image { Source = b.Source, Width = b.Width, Height = b.Height,
                    RenderTransformOrigin = new Point(b.CenterX / b.SourceWidth,
                        b.CenterY / b.SourceHeight), IsHitTestVisible = false,
                    Opacity = settings.BrightnessPercent / 100.0 };
                RenderOptions.SetBitmapScalingMode(b.Visual, BitmapScalingMode.HighQuality);
                b.Rotation = new RotateTransform();
                b.Position = new TranslateTransform();
                TransformGroup motion = new TransformGroup();
                motion.Children.Add(b.Rotation);
                motion.Children.Add(b.Position);
                b.Visual.RenderTransform = motion;
                Canvas.SetLeft(b.Visual, -b.CenterX * b.Scale);
                Canvas.SetTop(b.Visual, -b.CenterY * b.Scale);
                scene.Children.Add(b.Visual);
            }
            root.Loaded += delegate { Configure(); root.Focus(); };
            root.SizeChanged += delegate { Configure(); };
            root.MouseMove += delegate { CheckMouse(); };
            root.MouseDown += delegate { if (!preview) Forms.Application.Exit(); };
            root.PreviewKeyDown += delegate { if (!preview) Forms.Application.Exit(); };
            Shown += delegate { initialMouse = Forms.Cursor.Position; mouseArmed = true; Configure(); };
            Forms.Application.ApplicationExit += OnApplicationExit;
            CompositionTarget.Rendering += OnRendering;
        }

        private static Body ReadBody(string name, double vx, double vy, double spin, double scale)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null) throw new FileNotFoundException("Embedded sprite missing", name);
                BitmapSource decoded = BitmapFrame.Create(stream, BitmapCreateOptions.None,
                    BitmapCacheOption.OnLoad);
                BitmapSource source = new FormatConvertedBitmap(decoded, PixelFormats.Bgra32, null, 0);
                source.Freeze();
                Body b = new Body { Source = source, SourceWidth = source.PixelWidth,
                    SourceHeight = source.PixelHeight, Vx = vx, Vy = vy, Spin = spin, Scale = scale };
                byte[] pixels = new byte[b.SourceWidth * b.SourceHeight * 4];
                source.CopyPixels(pixels, b.SourceWidth * 4, 0);
                b.Alpha = new byte[b.SourceWidth * b.SourceHeight];
                for (int i = 0; i < b.Alpha.Length; i++) b.Alpha[i] = pixels[i * 4 + 3];
                double mass = 0;
                int minX = b.SourceWidth, minY = b.SourceHeight, maxX = -1, maxY = -1;
                for (int y = 0; y < b.SourceHeight; y++)
                for (int x = 0; x < b.SourceWidth; x++)
                {
                    byte alpha = b.Alpha[y * b.SourceWidth + x];
                    mass += alpha / 255.0;
                    if (alpha < 128) continue;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
                if (mass < 1 || maxX < minX || maxY < minY)
                    throw new InvalidDataException("Sprite has no visible mass: " + name);
                // Use the middle of the visible silhouette as the rotation and physics center.
                b.CenterX = (minX + maxX + 1) * 0.5;
                b.CenterY = (minY + maxY + 1) * 0.5;
                double sourceInertia = 0;
                for (int y = 0; y < b.SourceHeight; y++)
                for (int x = 0; x < b.SourceWidth; x++)
                {
                    int p = y * b.SourceWidth + x;
                    double weight = b.Alpha[p] / 255.0;
                    double rx = x + 0.5 - b.CenterX, ry = y + 0.5 - b.CenterY;
                    sourceInertia += weight * (rx * rx + ry * ry);
                    if (b.Alpha[p] < 128) continue;
                    int left = x > 0 && b.Alpha[p - 1] >= 128 ? 1 : 0;
                    int right = x + 1 < b.SourceWidth && b.Alpha[p + 1] >= 128 ? 1 : 0;
                    int top = y > 0 && b.Alpha[p - b.SourceWidth] >= 128 ? 1 : 0;
                    int bottom = y + 1 < b.SourceHeight && b.Alpha[p + b.SourceWidth] >= 128 ? 1 : 0;
                    if (left != 0 && right != 0 && top != 0 && bottom != 0) continue;
                    b.Contour.Add(new PointF((float)rx, (float)ry));
                    double nx = left - right, ny = top - bottom;
                    double normalLength = Math.Sqrt(nx * nx + ny * ny);
                    if (normalLength > 0)
                        b.Boundary.Add(new Body.EdgePoint(rx, ry,
                            nx / normalLength, ny / normalLength));
                }
                b.Mass = mass * scale * scale;
                b.InverseMass = 1.0 / b.Mass;
                b.InverseInertia = 1.0 / (sourceInertia * scale * scale * scale * scale);
                b.CompactContour();
                b.UpdateTrig();
                return b;
            }
        }

        private void Configure()
        {
            if (root.ActualWidth < 1 || root.ActualHeight < 1) return;
            PresentationSource presentation = PresentationSource.FromVisual(root);
            if (presentation == null || presentation.CompositionTarget == null) return;
            double dpiX = presentation.CompositionTarget.TransformToDevice.M11;
            double dpiY = presentation.CompositionTarget.TransformToDevice.M22;
            worldWidth = preview ? 1080 : ClientSize.Width - 2 * EdgePadding;
            worldHeight = preview ? 810 : ClientSize.Height - 2 * EdgePadding;
            leftWall = 0;
            double scaleX = preview ? Math.Min(root.ActualWidth / 1080, root.ActualHeight / 810) : 1 / dpiX;
            double scaleY = preview ? scaleX : 1 / dpiY;
            scene.Width = preview ? worldWidth : ClientSize.Width;
            scene.Height = preview ? worldHeight : ClientSize.Height;
            scene.RenderTransform = new ScaleTransform(scaleX, scaleY);
            Canvas.SetLeft(scene, preview ? (root.ActualWidth - worldWidth * scaleX) * 0.5 : 0);
            Canvas.SetTop(scene, preview ? (root.ActualHeight - worldHeight * scaleY) * 0.5 : 0);
            if (initialized) return;
            FitAndPlaceBodies(bodies, worldWidth, worldHeight,
                new Random(Guid.NewGuid().GetHashCode()));
            foreach (Body b in bodies)
            {
                b.Visual.Width = b.Width;
                b.Visual.Height = b.Height;
                Canvas.SetLeft(b.Visual, -b.CenterX * b.Scale);
                Canvas.SetTop(b.Visual, -b.CenterY * b.Scale);
            }
            startingEnergy = TotalEnergy();
            initialized = true;
            UpdateVisuals();
        }

        private static void FitAndPlaceBodies(Body[] bodies, double width, double height,
            Random random)
        {
            while (!PlaceBodies(bodies, width, height, random))
            {
                foreach (Body body in bodies)
                {
                    body.Scale *= 0.9;
                    body.Mass *= 0.81;
                    body.InverseMass /= 0.81;
                    body.InverseInertia /= 0.6561;
                }
            }
        }

        private static bool PlaceBodies(Body[] bodies, double width, double height, Random random)
        {
            // Keep the full rotated silhouette on screen and leave room between its bounds.
            for (int i = 0; i < bodies.Length; i++)
            {
                Body body = bodies[i];
                body.X = body.Y = 0;
                double minX, minY, maxX, maxY;
                body.VisibleBounds(out minX, out minY, out maxX, out maxY);
                if (maxX - minX > width || maxY - minY > height) return false;
                double xRange = Math.Max(0, width - (maxX - minX));
                double yRange = Math.Max(0, height - (maxY - minY));
                bool placed = false;
                for (int attempt = 0; attempt < 1000 && !placed; attempt++)
                {
                    body.X = -minX + random.NextDouble() * xRange;
                    body.Y = -minY + random.NextDouble() * yRange;
                    double aMinX, aMinY, aMaxX, aMaxY;
                    body.VisibleBounds(out aMinX, out aMinY, out aMaxX, out aMaxY);
                    placed = true;
                    for (int j = 0; j < i; j++)
                    {
                        double bMinX, bMinY, bMaxX, bMaxY;
                        bodies[j].VisibleBounds(out bMinX, out bMinY, out bMaxX, out bMaxY);
                        if (aMinX < bMaxX + 24 && aMaxX > bMinX - 24 &&
                            aMinY < bMaxY + 24 && aMaxY > bMinY - 24)
                        {
                            placed = false;
                            break;
                        }
                    }
                }
                if (!placed) return false;
            }
            return true;
        }

        private void CheckMouse()
        {
            if (preview || !mouseArmed) return;
            System.Drawing.Point now = Forms.Cursor.Position;
            if (Math.Abs(now.X - initialMouse.X) > 8 || Math.Abs(now.Y - initialMouse.Y) > 8)
                Forms.Application.Exit();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!initialized || IsDisposed) return;
            double now = clock.Elapsed.TotalSeconds;
            double remaining = Math.Max(0, Math.Min(0.05, now - lastTime));
            lastTime = now;
            const double step = 1.0 / 120.0;
            while (remaining > 0)
            {
                double dt = Math.Min(step, remaining);
                Advance(dt);
                remaining -= dt;
            }
            UpdateVisuals();
        }

        private void Advance(double dt)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                Body b = bodies[i];
                oldX[i] = b.X; oldY[i] = b.Y; oldAngle[i] = b.Angle;
                b.X += b.Vx * dt;
                b.Y += b.Vy * dt;
                b.Angle = (b.Angle + b.Spin * dt) % 360;
                b.UpdateTrig();
                KeepInside(b);
            }
            for (int i = 0; i < bodies.Length; i++)
            for (int j = i + 1; j < bodies.Length; j++)
            {
                if (!PixelsOverlap(bodies[i], bodies[j])) continue;
                Body a = bodies[i], b = bodies[j];
                double nx, ny, cx, cy;
                FindContact(a, b, out nx, out ny, out cx, out cy);
                double inverseMassSum = a.InverseMass + b.InverseMass;
                bool separated = false;
                for (int iteration = 0; iteration < 24; iteration++)
                {
                    a.X -= nx * a.InverseMass / inverseMassSum;
                    a.Y -= ny * a.InverseMass / inverseMassSum;
                    b.X += nx * b.InverseMass / inverseMassSum;
                    b.Y += ny * b.InverseMass / inverseMassSum;
                    if (!PixelsOverlap(a, b)) { separated = true; break; }
                }
                if (!separated)
                {
                    a.X = oldX[i]; a.Y = oldY[i]; a.Angle = oldAngle[i]; a.UpdateTrig();
                    b.X = oldX[j]; b.Y = oldY[j]; b.Angle = oldAngle[j]; b.UpdateTrig();
                }
                ApplyPairImpulse(a, b, nx, ny, cx, cy);
                KeepInside(a); KeepInside(b);
            }
            ConserveEnergy();
        }

        private void KeepInside(Body b)
        {
            double minX, minY, maxX, maxY;
            b.VisibleBounds(out minX, out minY, out maxX, out maxY);
            if (minX < leftWall)
            {
                b.X += leftWall - minX;
                ApplyWallImpulse(b, 1, 0, -1, 0);
            }
            if (maxX > worldWidth)
            {
                b.X -= maxX - worldWidth;
                ApplyWallImpulse(b, -1, 0, 1, 0);
            }
            if (minY < 0)
            {
                b.Y -= minY;
                ApplyWallImpulse(b, 0, 1, 0, -1);
            }
            if (maxY > worldHeight)
            {
                b.Y -= maxY - worldHeight;
                ApplyWallImpulse(b, 0, -1, 0, 1);
            }
        }

        private static void FindContact(Body a, Body b,
            out double nx, out double ny, out double cx, out double cy)
        {
            nx = ny = cx = cy = 0;
            int count = 0;
            foreach (Body.EdgePoint edge in a.Boundary)
            {
                double x = a.X + a.Scale * (edge.X * a.Cos - edge.Y * a.Sin);
                double y = a.Y + a.Scale * (edge.X * a.Sin + edge.Y * a.Cos);
                if (!b.Hit(x, y)) continue;
                nx += edge.Nx * a.Cos - edge.Ny * a.Sin;
                ny += edge.Nx * a.Sin + edge.Ny * a.Cos;
                cx += x; cy += y; count++;
            }
            foreach (Body.EdgePoint edge in b.Boundary)
            {
                double x = b.X + b.Scale * (edge.X * b.Cos - edge.Y * b.Sin);
                double y = b.Y + b.Scale * (edge.X * b.Sin + edge.Y * b.Cos);
                if (!a.Hit(x, y)) continue;
                nx -= edge.Nx * b.Cos - edge.Ny * b.Sin;
                ny -= edge.Nx * b.Sin + edge.Ny * b.Cos;
                cx += x; cy += y; count++;
            }
            if (count > 0) { cx /= count; cy /= count; }
            else { cx = (a.X + b.X) * 0.5; cy = (a.Y + b.Y) * 0.5; }
            double length = Math.Sqrt(nx * nx + ny * ny);
            if (length < 0.001)
            {
                nx = b.X - a.X; ny = b.Y - a.Y;
                length = Math.Sqrt(nx * nx + ny * ny);
                if (length < 0.001) { nx = 1; ny = 0; length = 1; }
            }
            nx /= length; ny /= length;
            if (nx * (b.X - a.X) + ny * (b.Y - a.Y) < 0)
            { nx = -nx; ny = -ny; }
        }

        private static void ApplyPairImpulse(Body a, Body b,
            double nx, double ny, double cx, double cy)
        {
            double rax = cx - a.X, ray = cy - a.Y;
            double rbx = cx - b.X, rby = cy - b.Y;
            double omegaA = a.Spin * Math.PI / 180.0;
            double omegaB = b.Spin * Math.PI / 180.0;
            double relativeX = b.Vx - omegaB * rby - a.Vx + omegaA * ray;
            double relativeY = b.Vy + omegaB * rbx - a.Vy - omegaA * rax;
            double approach = relativeX * nx + relativeY * ny;
            if (approach >= 0) return;
            double crossA = rax * ny - ray * nx;
            double crossB = rbx * ny - rby * nx;
            double denominator = a.InverseMass + b.InverseMass +
                crossA * crossA * a.InverseInertia + crossB * crossB * b.InverseInertia;
            double impulse = -2 * approach / denominator;
            a.Vx -= impulse * nx * a.InverseMass;
            a.Vy -= impulse * ny * a.InverseMass;
            b.Vx += impulse * nx * b.InverseMass;
            b.Vy += impulse * ny * b.InverseMass;
            a.Spin -= impulse * crossA * a.InverseInertia * 180.0 / Math.PI;
            b.Spin += impulse * crossB * b.InverseInertia * 180.0 / Math.PI;
        }

        private static void ApplyWallImpulse(Body b, double nx, double ny,
            double supportX, double supportY)
        {
            double extreme = double.MinValue;
            foreach (PointF point in b.Contour)
            {
                double x = b.X + b.Scale * (point.X * b.Cos - point.Y * b.Sin);
                double y = b.Y + b.Scale * (point.X * b.Sin + point.Y * b.Cos);
                extreme = Math.Max(extreme, x * supportX + y * supportY);
            }
            double contactX = 0, contactY = 0;
            int count = 0;
            foreach (PointF point in b.Contour)
            {
                double x = b.X + b.Scale * (point.X * b.Cos - point.Y * b.Sin);
                double y = b.Y + b.Scale * (point.X * b.Sin + point.Y * b.Cos);
                if (extreme - x * supportX - y * supportY > 1.0) continue;
                contactX += x; contactY += y; count++;
            }
            if (count == 0) return;
            double rx = contactX / count - b.X, ry = contactY / count - b.Y;
            double omega = b.Spin * Math.PI / 180.0;
            double approach = (b.Vx - omega * ry) * nx + (b.Vy + omega * rx) * ny;
            if (approach >= 0) return;
            double cross = rx * ny - ry * nx;
            double denominator = b.InverseMass + cross * cross * b.InverseInertia;
            double impulse = -2 * approach / denominator;
            b.Vx += impulse * nx * b.InverseMass;
            b.Vy += impulse * ny * b.InverseMass;
            b.Spin += impulse * cross * b.InverseInertia * 180.0 / Math.PI;
        }

        private double TotalEnergy()
        {
            double energy = 0;
            foreach (Body b in bodies)
            {
                double omega = b.Spin * Math.PI / 180.0;
                energy += 0.5 * b.Mass * (b.Vx * b.Vx + b.Vy * b.Vy) +
                    0.5 * omega * omega / b.InverseInertia;
            }
            return energy;
        }

        private void ConserveEnergy()
        {
            double energy = TotalEnergy();
            if (energy <= 0 || Math.Abs(energy - startingEnergy) < startingEnergy * 0.001)
                return;
            double factor = Math.Sqrt(startingEnergy / energy);
            foreach (Body b in bodies)
            {
                b.Vx *= factor; b.Vy *= factor; b.Spin *= factor;
            }
        }

        private static bool PixelsOverlap(Body a, Body b)
        {
            double aminX, aminY, amaxX, amaxY, bminX, bminY, bmaxX, bmaxY;
            a.VisibleBounds(out aminX, out aminY, out amaxX, out amaxY);
            b.VisibleBounds(out bminX, out bminY, out bmaxX, out bmaxY);
            int left = (int)Math.Ceiling(Math.Max(aminX, bminX));
            int top = (int)Math.Ceiling(Math.Max(aminY, bminY));
            int right = (int)Math.Floor(Math.Min(amaxX, bmaxX));
            int bottom = (int)Math.Floor(Math.Min(amaxY, bmaxY));
            if (left > right || top > bottom) return false;
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                    if (a.Hit(x + 0.5, y + 0.5) && b.Hit(x + 0.5, y + 0.5)) return true;
            return false;
        }

        private void UpdateVisuals()
        {
            foreach (Body b in bodies)
            {
                b.Position.X = b.X + (preview ? 0 : EdgePadding);
                b.Position.Y = b.Y + (preview ? 0 : EdgePadding);
                b.Rotation.Angle = b.Angle;
            }
        }

        private void OnApplicationExit(object sender, EventArgs e) { Close(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CompositionTarget.Rendering -= OnRendering;
                Forms.Application.ApplicationExit -= OnApplicationExit;
                host.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
