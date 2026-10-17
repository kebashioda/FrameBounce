# Frame Bounce

A little something for the Sooners: a Windows screensaver where the Steam Frame and its two controllers drift, spin, and bounce across your screen, bumping into each other along the way.

https://github.com/user-attachments/assets/4649cc08-e2b2-435b-b983-aecec7ab1886

## Download and install

Download the included [Frame Bounce.scr](dist/Frame%20Bounce.scr) on a 64-bit Windows PC with .NET Framework 4.x. In File Explorer, right-click the file and choose **Install** (on Windows 11, it may be under **Show more options**). Select **Frame Bounce** in Windows Screen Saver Settings and adjust its settings there.

The included `.scr` file is not digitally signed.

You can also run it directly from PowerShell without installing it:

```powershell
& '.\dist\Frame Bounce.scr' /s  # Full screen
& '.\dist\Frame Bounce.scr' /c  # Settings
```

Move the mouse, click, or press a key to close the full-screen screen saver. The settings dialog controls object size, movement and rotation speed, and brightness. The three sprites are embedded in the `.scr` file.

## Build from source

On 64-bit Windows with .NET Framework 4.x, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

The script uses the .NET Framework C# compiler included with Windows and writes `dist\Frame Bounce.scr`. The three PNG files in `assets/` are embedded into the binary.

To regenerate those sprites from the transparent cutout in `artwork/`, run `tools\extract-sprites.ps1` before rebuilding. With FFmpeg installed, generate the three 20-second demos and preview images offscreen:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\render-videos.ps1
```

The media script finds `ffmpeg.exe` on `PATH` or in WinGet packages; pass `-FfmpegPath` to specify another location. The settings screenshot is included in `docs/`.

| Directory | Contents |
| --- | --- |
| `src/` | Screen saver and settings source |
| `assets/` | Embedded PNG sprites |
| `artwork/` | Source artwork and transparent cutout |
| `dist/` | Ready-to-run screen saver |
| `docs/` | Screenshots and demo videos |
| `tools/` | Sprite, media, and movement utilities |

## Credits and license

Created by [@kebashioda](https://github.com/kebashioda), with assistance from AI tools during development. The source code is licensed under the [MIT License](LICENSE). This is an unofficial fan project and is not affiliated with Valve.

The sprites are derived from an image in [Notebookcheck's Steam Frame article](https://www.notebookcheck.net/Steam-Frame-launching-anytime-now-as-more-shipments-reach-Valve-s-US-warehouses.1320895.0.html), which credits Valve. The source image and derived artwork, including the sprites embedded in the screen saver and shown in the previews and videos, are excluded from this project's MIT license. Rights in the underlying image remain with their respective owner(s).
