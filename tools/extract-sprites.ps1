$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$cutoutPath = Join-Path $projectRoot 'artwork\clean-cutout.png'
$assetPath = Join-Path $projectRoot 'assets'

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class FrameBounceSpriteExtractor
{
    public static void Extract(Bitmap source, Rectangle crop, int width, int height, string path)
    {
        if (crop.Right > source.Width || crop.Bottom > source.Height)
            throw new ArgumentOutOfRangeException("crop");

        using (var cropped = new Bitmap(crop.Width, crop.Height, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < crop.Height; y++)
            {
                for (int x = 0; x < crop.Width; x++)
                {
                    Color color = source.GetPixel(crop.X + x, crop.Y + y);
                    // The cutout has dark objects and light details. Invert to the
                    // original white-on-black screen saver style and flatten color noise.
                    int luminance = (54 * color.R + 183 * color.G + 19 * color.B + 128) >> 8;
                    int white = Math.Max(0, Math.Min(255, (220 - luminance) * 255 / 160));
                    cropped.SetPixel(x, y, Color.FromArgb(color.A, white, white, white));
                }
            }

            using (var sprite = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(sprite))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(cropped, new Rectangle(0, 0, width, height),
                    new Rectangle(0, 0, crop.Width, crop.Height), GraphicsUnit.Pixel);
                sprite.Save(path, ImageFormat.Png);
            }
        }
    }
}
'@

$cutout = [System.Drawing.Bitmap]::new($cutoutPath)
try {
    if ($cutout.Width -ne 1448 -or $cutout.Height -ne 1086) {
        throw "Unexpected clean cutout dimensions: $($cutout.Width)x$($cutout.Height)"
    }

    [FrameBounceSpriteExtractor]::Extract($cutout,
        [System.Drawing.Rectangle]::new(28, 352, 299, 502), 218, 369,
        (Join-Path $assetPath 'left-controller.png'))
    [FrameBounceSpriteExtractor]::Extract($cutout,
        [System.Drawing.Rectangle]::new(375, 190, 716, 436), 527, 318,
        (Join-Path $assetPath 'headset.png'))
    [FrameBounceSpriteExtractor]::Extract($cutout,
        [System.Drawing.Rectangle]::new(1134, 352, 300, 502), 218, 369,
        (Join-Path $assetPath 'right-controller.png'))
} finally {
    $cutout.Dispose()
}

Write-Output 'Extracted three sprites from artwork\clean-cutout.png.'
