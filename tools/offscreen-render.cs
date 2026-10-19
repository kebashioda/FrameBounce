// Renders Frame Bounce into an MP4 without showing a window or capturing the desktop.
// Build with tools/render-videos.ps1. This is a media-generation tool, not part of the .scr.
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

internal static class OffscreenRender
{
    const int Width = 1920;
    const int Height = 1080;
    const int Fps = 30;
    const int Seconds = 20;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static object Field(object target, string name)
    {
        return target.GetType().GetField(name, Private).GetValue(target);
    }

    static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, Private).SetValue(target, value);
    }

    static object Call(object target, string name, params object[] args)
    {
        return target.GetType().GetMethod(name, Private).Invoke(target, args);
    }

    static object BodyField(object body, string name)
    {
        return body.GetType().GetField(name).GetValue(body);
    }

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length != 4) throw new ArgumentException("Usage: offscreen-render.exe screensaver.scr ffmpeg.exe output.mp4 speed-percent");
        int speed = int.Parse(args[3]);
        if (speed != 100 && speed != 200 && speed != 400) throw new ArgumentException("Speed must be 100, 200, or 400");
        var assembly = Assembly.LoadFrom(args[0]);
        var saverType = assembly.GetType("FrameBounce.SaverForm", true);
        var settingsType = assembly.GetType("FrameBounce.SaverSettings", true);
        var settings = Activator.CreateInstance(settingsType);
        settingsType.GetField("SizePercent").SetValue(settings, 100);
        settingsType.GetField("SpeedPercent").SetValue(settings, speed);
        settingsType.GetField("BrightnessPercent").SetValue(settings, 50);
        using (var saver = (Forms.Form)Activator.CreateInstance(saverType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { false, settings }, null))
        {
            var bodies = (Array)Field(saver, "bodies");

            SetField(saver, "worldWidth", (double)(Width - 8));
            SetField(saver, "worldHeight", (double)(Height - 8));
            SetField(saver, "leftWall", 0.0);
            saverType.GetMethod("FitAndPlaceBodies", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { bodies, (double)(Width - 8), (double)(Height - 8), new Random(20261017 + speed) });

            var root = (Canvas)Field(saver, "root");
            var scene = (Canvas)Field(saver, "scene");
            root.Width = Width;
            root.Height = Height;
            scene.Width = Width;
            scene.Height = Height;
            foreach (object body in bodies)
            {
                var visual = (Image)BodyField(body, "Visual");
                var bodyType = body.GetType();
                double scale = (double)BodyField(body, "Scale");
                double centerX = (double)BodyField(body, "CenterX");
                double centerY = (double)BodyField(body, "CenterY");
                visual.Width = (double)bodyType.GetProperty("Width").GetValue(body, null);
                visual.Height = (double)bodyType.GetProperty("Height").GetValue(body, null);
                Canvas.SetLeft(visual, -centerX * scale);
                Canvas.SetTop(visual, -centerY * scale);
            }
            root.Measure(new Size(Width, Height));
            root.Arrange(new Rect(0, 0, Width, Height));
            root.UpdateLayout();
            SetField(saver, "startingEnergy", (double)Call(saver, "TotalEnergy"));
            SetField(saver, "initialized", true);

            var ffmpeg = new ProcessStartInfo(args[1]);
            ffmpeg.Arguments = "-hide_banner -loglevel error -y -f rawvideo -pixel_format bgra -video_size 1920x1080 -framerate 30 -i pipe:0 -frames:v 600 -c:v libx264 -preset medium -crf 20 -pix_fmt yuv420p -movflags +faststart -an \"" + args[2] + "\"";
            ffmpeg.UseShellExecute = false;
            ffmpeg.RedirectStandardInput = true;
            using (var encoder = Process.Start(ffmpeg))
            {
                byte[] pixels = new byte[Width * Height * 4];
                var frame = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
                for (int i = 0; i < Seconds * Fps; i++)
                {
                    if (i != 0)
                        for (int tick = 0; tick < 4; tick++) Call(saver, "Advance", 1.0 / 120.0);
                    Call(saver, "UpdateVisuals");
                    frame.Clear();
                    frame.Render(root);
                    frame.CopyPixels(pixels, Width * 4, 0);
                    encoder.StandardInput.BaseStream.Write(pixels, 0, pixels.Length);
                    if (i % 60 == 0) Console.Error.WriteLine("{0}: {1}/{2} frames", speed, i, Seconds * Fps);
                }
                encoder.StandardInput.BaseStream.Close();
                encoder.WaitForExit();
                if (encoder.ExitCode != 0) throw new Exception("FFmpeg failed with exit code " + encoder.ExitCode);
            }
        }
        return 0;
    }
}
