# Prompt: put UnrankedSmurfs branding on the Account Exporter — the app works, it just still looks like the fork it came from

> **Handoff.** Start the session in `/home/lcurva/projects/account-exporter`, then: *"Read BRANDING-HANDOFF-PROMPT.md and do the branding pass."*
>
> Written 2026-09-09, end of the session that forked `Ja-Sa-La/League-Account-Manager` and stripped it to a cosmetics-only exporter. Facts marked ✓ were machine-verified on `master` (tip `03c5cb4`, **pushed, working tree clean**, CI run `34325078402` green: 0 warnings, 0 errors). The functional work is done and shipped; this prompt covers only the visual identity, which was deliberately left out of that commit.

---

## Binding decisions

1. **The exporter is finished functionally. Do not redesign it.** Capture → list → export works and is verified end-to-end against the website's real `AccountProfile::for()` ✓. This is a skin-deep pass: an icon and a colour swap. If you find yourself editing `Export/` or `Lcu/`, you have gone out of scope.
2. **The design system is the reference** — `/home/lcurva/projects/website/.claude/UnrankedSmurfs Design System/` ✓, and specifically `colors_and_type.css`. Gold `#F7B733` primary, `#131313` page, `#181818` surfaces, `#2B2B2B` raised, `#3B3B3D` hairlines, **`#131313` text on gold** (never white). The founder rejected an earlier skindex pass on another repo for ignoring this system; do not repeat that.
3. **The mark is the winged shield, from `logo.svg`.** Do not draw a new one, do not generate one with an image model, do not use an emoji. The vector already exists.
4. **The icon never changes again after this.** Users find the app by its taskbar icon. Pick the crop once and keep it.
5. **The credential guarantee is load-bearing copy.** The README's "What it does not read" section and the in-app privacy panel (`MainWindow.xaml`, the `Grid.Row="2"` border) are the reason a seller trusts this thing. You may restyle them. Do not shorten, soften, or delete them.
6. **This repo is public and MIT, forked from Ja-Sa-La.** `LICENSE.md` carries both copyright lines and the README credits upstream ✓. Leave both intact.

## First ten minutes

1. `git log --oneline -3` — expect `03c5cb4` at tip, clean tree.
2. Read `README.md` (what the tool promises) and `docs/export-format.md` (the contract with the website). Neither should change in this pass.
3. Open `src/UnrankedSmurfs.AccountExporter/Resources/Theme.xaml` — this is where most of the work is.
4. Confirm the toolchain: `which convert inkscape` → both present ✓; `python3 -c "import PIL"` → Pillow 10.2.0 ✓.

## Tooling — read before running anything

- **You cannot build or run this locally.** WPF builds on Windows only, and there is no `dotnet` on this box ✓. **GitHub Actions is your compiler.** Push a branch and read the run: `.github/workflows/build.yml` builds Release and publishes a single-file win-x64 exe on every branch ✓.
  - `gh run list --branch <branch> --limit 3`
  - `gh run watch <id> --exit-status --interval 15`
  - `gh run view <id> --log | grep -E "Build" | grep -iE "warn|error|Build succeeded"`
- **A green build does not mean the window renders.** XAML resource errors are runtime, not compile-time: a `StaticResource` pointing at a key you renamed compiles fine and throws on startup. So **do not rename existing brush keys** — change their `Color` values in place. That is the single highest-risk mistake available in this task.
- `gh` is authenticated as `unrankedsmurfsrocks`, which has access to the `UnrankedSmurfs` org ✓.
- ImageMagick (`convert`), Inkscape, and Pillow are all available for icon generation ✓.

---

## 1. What exists ✓

`src/UnrankedSmurfs.AccountExporter/` — 1,348 lines of C# across 8 files.

```
App.xaml(.cs)            WPF-UI dark theme + Resources/Theme.xaml merged
MainWindow.xaml(.cs)     the whole UI: header, status, privacy panel, grid, actions
Resources/Theme.xaml     brushes + implicit styles  ← most of this pass
Assets/app.ico           upstream's icon, renamed   ← the other part
Export/                  AccountCapture, AccountSnapshot, ExportWriter, RegionNormalizer
Lcu/                     LcuClient, ApiResponseParser, LcuRequestLog (upstream's work)
```

### The two gaps

**Gap 1 — the icon is still Ja-Sa-La's.** `Assets/app.ico` is upstream's LAM icon, renamed during the fork. It is also a technically poor `.ico`: **one single 150×150 frame** ✓. Windows wants 16/20/24/32/40/48/64/128/256 so the taskbar, alt-tab and Explorer each pick a clean size instead of resampling 150 down to 16.

Wired in exactly two places, both in `UnrankedSmurfs.AccountExporter.csproj` ✓:
- line 11 `<ApplicationIcon>Assets\app.ico</ApplicationIcon>`
- line 27 `<Content Include="Assets\app.ico" />`

The `Window` has no `Icon=` attribute, so it inherits `ApplicationIcon`. Replacing the file in place is sufficient — no csproj change needed.

**Gap 2 — the palette is still the fork's blue.** `Theme.xaml` is a generic dark theme with a **blue** accent. Current values vs. the design system ✓:

| Key | Line | Now | Should be | Design-system name |
| --- | --- | --- | --- | --- |
| `AccentBrush` | 10 | `#4DA3FF` | `#F7B733` | `--us-gold-500` |
| `AccentMutedBrush` | 11 | `#354A5F` | `#5A4520`-ish | muted gold, pick against `#131313` |
| `AppBackgroundBrush` | 3 | `#101315` | `#131313` | `--us-bg` |
| `SurfaceBrush` | 4 | `#181C1F` | `#181818` | `--us-bg-bar` |
| `SurfaceRaisedBrush` | 5 | `#202529` | `#2B2B2B` | `--us-bg-raised` |
| `StrokeBrush` | 7 | `#343B40` | `#3B3B3D` | `--us-grey-400` |

Lines 17–25 declare nine `SystemColors.*` brushes with **hard-coded literals, not `StaticResource` references** ✓ — so changing the table above does not touch them. Sweep them in the same pass or the grid and menus stay a different grey from everything else:

- `#202529` on lines 17, 19, 21, 23 → `#2B2B2B` ✓
- **line 24, `HighlightBrushKey`, is `#354A5F` — the fork's blue** ✓. This is the DataGrid row-selection colour. Miss it and selecting a captured account still flashes blue in an otherwise gold app. It should become the muted gold you pick for `AccentMutedBrush`.

### There is no primary button

Every `Button` uses one implicit style (`Theme.xaml`, `TargetType="Button"`): `SurfaceRaisedBrush` background, `TextPrimaryBrush` foreground ✓. So **Capture** and **Export** — the two actions the whole app exists for — currently look identical to **Remove selected**. Gold is what fixes this, and it is the most visible single change in this pass.

---

## 2. The build

### 2.1 The icon

Source: `/home/lcurva/projects/website/public/images/logo.svg` ✓ — the full lockup, 992.13 × 193.81, Illustrator-exported, vector.

**The mark's exact bounds, measured** ✓: render the SVG at 992px wide, and the winged shield occupies **`183x154+17+18`**. The wordmark's "U" begins at x ≈ 211. So:

```bash
SRC="/home/lcurva/projects/website/public/images/logo.svg"
inkscape "$SRC" --export-type=png --export-filename=/tmp/lockup.png --export-width=3968   # 4x
convert /tmp/lockup.png -crop 732x616+68+72 +repage /tmp/mark.png                        # 4x of 183x154+17+18
```

Then, in order:

1. **Square it on padding, do not stretch it.** The mark is 183×154 — taller-than-wide it is not, but it is not square either. Centre it in a square canvas with ~10% breathing room (`-gravity center -extent`).
2. **Decide the ground.** The mark is white wing + gold shield, drawn for a dark page. On a transparent icon it will vanish against a light Explorer background. **Recommended: a `#131313` rounded-square ground**, which matches the app window and reads at 16px. A transparent-background icon is the alternative; if you take it, verify at 16px against both light and dark Windows themes before committing.
3. **Generate a real multi-resolution `.ico`:**
   ```bash
   convert /tmp/mark_square.png -background none \
     \( -clone 0 -resize 256x256 \) \( -clone 0 -resize 128x128 \) \
     \( -clone 0 -resize 64x64   \) \( -clone 0 -resize 48x48   \) \
     \( -clone 0 -resize 40x40   \) \( -clone 0 -resize 32x32   \) \
     \( -clone 0 -resize 24x24   \) \( -clone 0 -resize 20x20   \) \
     \( -clone 0 -resize 16x16   \) -delete 0 \
     src/UnrankedSmurfs.AccountExporter/Assets/app.ico
   ```
4. **Look at it.** Render each frame back out to PNG and actually open them with the Read tool at 16, 32 and 256. The wing is fine linework; if it turns to mush at 16px, **simplify the small frames** — a shield-only crop for ≤24px is a legitimate and common answer. Do not ship a 16px frame you have not looked at.
5. Verify: `identify Assets/app.ico` should list **9 frames**, not 1.

### 2.2 The palette

Edit `Theme.xaml` **values in place**. Change no `x:Key` — see the runtime-error warning above.

Then add a gold primary button. Give it a key (`PrimaryButtonStyle`), do **not** make it the implicit `Button` style, or Remove-selected goes gold too:

```xml
<Style x:Key="PrimaryButtonStyle" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}">
    <Setter Property="Background" Value="{StaticResource AccentBrush}" />
    <Setter Property="BorderBrush" Value="{StaticResource AccentBrush}" />
    <Setter Property="Foreground" Value="#131313" />   <!-- --us-on-gold, never white -->
</Style>
```

Apply it to exactly two buttons in `MainWindow.xaml`: `CaptureButton` and `ExportButton`. Leave `RemoveButton` neutral.

**Watch the disabled state.** Both gold buttons spend most of their life disabled (`ExportButton` until something is captured; `CaptureButton` when the client is not running ✓). WPF-UI's default disabled rendering over a gold background has not been checked — add an explicit disabled trigger that drops to `AccentMutedBrush` with `TextSecondaryBrush` text, so a disabled Export does not read as an enabled one.

### 2.3 Typography — a decision to make, not a default to assume

The design system is **Lato**. `Theme.xaml` currently specifies **Segoe UI Variable** in 12 places ✓.

**Lato is not a Windows system font.** Shipping it means embedding the `.ttf` as a `<Resource>` and referencing it by pack URI, which adds ~500KB and a licensing line to the repo (Lato is SIL OFL — redistributable, but the licence must travel with it).

Recommendation: **stay on Segoe UI Variable for v1** and note the divergence in the README. It is the native Windows UI font, it costs nothing, and a desktop utility looking native is not the same failure as a web page ignoring the brand. Raise it with the founder rather than deciding silently — but if you do embed Lato, embed the OFL text alongside it.

---

## 3. Acceptance

- [ ] `identify Assets/app.ico` lists 9 frames ✓, and you have *looked* at the 16, 32 and 256 renders.
- [ ] No `x:Key` in `Theme.xaml` was renamed or removed.
- [ ] `AccentBrush` is `#F7B733`; background/surface/stroke match the table in §1.
- [ ] Capture and Export are gold with `#131313` text; Remove selected is not.
- [ ] Disabled gold buttons are visibly disabled.
- [ ] CI green, **0 warnings 0 errors** — match the baseline, do not regress it.
- [ ] The privacy panel and the README's "What it does not read" are intact.
- [ ] A screenshot of the running window is attached to the PR **or** the PR says plainly that nobody has run it on Windows yet.

**On that last point:** everything here is verifiable except how the window actually looks. Nobody in this session or the last one has seen this app render — there is no Windows machine in the loop ✓. Say so honestly rather than implying it was seen.

## Out of scope

- **The website import endpoint.** Nothing on unrankedsmurfs.com ingests the exported `.json` yet. That is the other open thread and its own handoff — do not start it here.
- Any change to `Export/`, `Lcu/`, or `docs/export-format.md`.
- A first GitHub Release. Worth doing, but after the icon lands, so v1.0.0 ships with the right artwork.
- `/home/lcurva/projects/website/public/favicon.ico` is **0 bytes** ✓ — a real bug, unrelated to this repo. Mention it to the founder; do not fix it here.
