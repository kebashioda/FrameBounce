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

To regenerate the sprites from the transparent cutout in `artwork/`, run `tools\extract-sprites.ps1` before rebuilding. The settings screenshot and demo videos are included in `docs/`.

| Directory | Contents |
| --- | --- |
| `src/` | Screen saver and settings source |
| `assets/` | Embedded PNG sprites |
| `artwork/` | Source artwork and transparent cutout |
| `dist/` | Ready-to-run screen saver |
| `docs/` | Screenshots and demo videos |
| `tools/` | Sprite, media, and movement utilities |

## Credits and license

Frame Bounce is a free, unofficial fan-made screensaver by [@kebashioda](https://github.com/kebashioda). It is not affiliated with, sponsored by, or endorsed by Valve. The source code is licensed under the [MIT License](LICENSE).

The device artwork was adapted from an image published in [Notebookcheck's Steam Frame article](https://www.notebookcheck.net/Steam-Frame-launching-anytime-now-as-more-shipments-reach-Valve-s-US-warehouses.1320895.0.html), which credits Valve. The original image and all derived artwork—including sprites embedded in the screensaver and images shown in previews and videos—are **not** covered by the MIT license. Steam and Steam Frame names and logos belong to Valve.

Valve has confirmed that it is fine with Frame Bounce's proposed non-commercial use of the artwork.
