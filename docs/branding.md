# Branding

The exporter's visual identity, and how to reproduce it.

## Palette

The app takes its colours from the UnrankedSmurfs design system
(`colors_and_type.css` in the website repo). They live in exactly one place —
`src/UnrankedSmurfs.AccountExporter/Resources/Theme.xaml` — as brush values.

| Brush key | Value | Design-system token |
| --- | --- | --- |
| `AppBackgroundBrush` | `#131313` | `--us-bg` |
| `SurfaceBrush` | `#181818` | `--us-bg-bar` |
| `SurfaceRaisedBrush` | `#2B2B2B` | `--us-bg-raised` |
| `SurfaceHoverBrush` | `#363638` | above raised, just under the hairline |
| `StrokeBrush` | `#3B3B3D` | `--us-grey-400` |
| `TextPrimaryBrush` | `#E4E4E4` | `--us-text` (87% white) flattened |
| `TextSecondaryBrush` | `#BDC3C7` | `--us-grey-200` |
| `AccentBrush` | `#F7B733` | `--us-gold-500` |
| `AccentMutedBrush` | `#5A4520` | gold muted into `--us-bg` |
| `SuccessBrush` | `#6AC259` | `--us-green` |
| `WarningBrush` | `#FFD580` | `--us-gold-400` |
| `DangerBrush` | `#C0392B` | `--us-red-200` |

Headings are pure white; body copy is the 87% grey. Text on gold is `#131313`,
never white.

Two rules that are easy to break and expensive to notice:

- **Never rename a brush key.** XAML resource lookup is a runtime concern. A
  `{StaticResource AccentBrush}` pointing at a key somebody renamed compiles
  with zero warnings and throws when the window is constructed. Change the
  `Color`, keep the `x:Key`. `ThemeTests` and `WindowSmokeTests` exist to make
  that failure land in CI instead of on a user's launch.
- **The nine `SystemColors.*` brushes are hard-coded literals, not references.**
  Recolouring `AccentBrush` does not touch them. `HighlightBrushKey` is the
  DataGrid's row-selection fill and has to be moved by hand, or selecting a
  captured account flashes the wrong colour in an otherwise gold app.

`PrimaryButtonStyle` is the gold button, applied to `CaptureButton` and
`ExportButton` only. It carries its own `ControlTemplate` rather than
inheriting WPF-UI's, because both buttons spend most of their life disabled and
how the library renders "disabled" over a gold background could not be checked
from here — WPF neither builds nor runs on Linux.

## The icon

`src/UnrankedSmurfs.AccountExporter/Assets/app.ico` is the winged shield from
the website's `logo.svg`, on a `#131313` rounded-square ground. It is wired
through `<ApplicationIcon>` in the csproj; the window has no `Icon=` attribute
and inherits it.

**The mark's bounds in `logo.svg`, rendered at 992px wide, are
`121x154+17+18`.** The lockup's ink runs are `17–137` (the mark), a gap, then
`162–225` (the "U" of the wordmark). An earlier note in this repo recorded the
mark as 183 wide; that number would have cropped part of the "U" into the icon.
Measure with an alpha column scan rather than by eye if it ever needs redoing.

Frames are tiered, because the wing is fine linework that turns to mush when a
single master is resampled to 16px:

| Frames | Mark height | Corner radius | Extra |
| --- | --- | --- | --- |
| 256, 128 | 78% of canvas | 18% | — |
| 64, 48, 40 | 85% | 16% | — |
| 32, 24, 20, 16 | 92% | 13% | `-unsharp 0x0.6+0.9+0.02` |

```bash
SRC=/path/to/website/public/images/logo.svg
inkscape "$SRC" --export-type=png --export-filename=lockup8x.png --export-width=7936
convert lockup8x.png -crop 968x1232+136+144 +repage mark.png     # 8x of 121x154+17+18

# one master per tier, e.g. the small one:
convert -size 1024x1024 xc:none -fill '#131313' \
  -draw 'roundrectangle 0,0 1023,1023 133,133' ground.png
convert mark.png -resize x942 m.png
convert ground.png m.png -gravity center -composite tight.png

convert tight.png -filter Lanczos -resize 16x16 -unsharp 0x0.6+0.9+0.02 -depth 8 PNG32:f16.png
# ...one per size, then:
convert f256.png f128.png f64.png f48.png f40.png f32.png f24.png f20.png f16.png app.ico
```

Verify with `identify app.ico` — **nine frames**, every one `alpha=True`. The
file it replaced had a single 150×150 frame, which left Windows resampling one
bitmap down to 16px for the taskbar.

Windows caches icons aggressively. A rebuilt exe may keep showing the old icon
in Explorer until the shell icon cache is cleared; that is not a build failure.
