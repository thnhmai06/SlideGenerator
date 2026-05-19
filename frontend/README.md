# SlideGenerator Design System

A design system for **SlideGenerator** — an end-to-end, automated, template-based presentation generator. SlideGenerator takes a Workbook (`.xlsx` / `.xlsm`) and a PowerPoint template (`.pptx` / `.potx`) and produces filled-in presentations: text replacements, image replacements (with face-detection + saliency-aware crops), batch jobs, and downloadable outputs.

The product is a Tauri-wrapped React app that also runs as a plain Vite web build. The v1 UI lives on `main`; the in-progress source on `develop/main` is mid-refactor and superseded; the **future surface is being themed against `unsloth-studio`**, which the maintainer publishes as a separate design reference. This design system codifies that direction.

## Sources

You don't need access to these to use the system — everything is mirrored locally — but if you do have access, the original repos go deeper:

- **Brand mark (Figma):** SlideGenerator Logo, two frames — `/App-Logo/Start` and `/App-Logo/Finish` (figma nodes `1:13`, `1:39`). Provided as a `.fig` mount. Pseudocode JSX in the mount; PNG renders copied to `assets/`.
- **Theme reference:** [`thnhmai06/unsloth-studio`](https://github.com/thnhmai06/unsloth-studio) on branch `studio`. The single source of truth for color tokens, radii, scrollbars, motion easing, and the shadcn `radix-maia` component style. See `frontend/src/index.css` and `frontend/src/components/ui/`.
- **Product code:** [`thnhmai06/SlideGenerator`](https://github.com/thnhmai06/SlideGenerator).
  - `main` — v1 UI (warm cream + teal, custom CSS variables). Reference for the feature surface (sidebar nav, create-task flow, jobs view, results).
  - `develop/main` — v2 refactor in progress (the actual UI here is **outdated** per the maintainer — pull tokens, copy, and feature shape from it but don't recreate it pixel-for-pixel).
- **Sibling project:** [`thnhmai06/SlideGenerator.Framework`](https://github.com/thnhmai06/SlideGenerator.Framework) — .NET backend framework.

> If you want a more accurate UI kit, read these repos. The kit here is faithful but cosmetic; the canonical interaction behavior lives in `src/features/{create-task,process,results,settings,about}` of the SlideGenerator repo.

## Index

Files at the root of this system:

| File | Purpose |
|---|---|
| `README.md` | This file — product context + content + visual + iconography guidelines. |
| `colors_and_type.css` | Base + semantic CSS variables for colors, type, spacing, radii, motion, shadows. Light is default; opt into dark with `.dark` or `[data-theme="dark"]` on a parent. |
| `components.css` | Class-based primitives — `.sg-btn`, `.sg-input`, `.sg-badge`, `.sg-card`, `.sg-sidebar-item`, etc. Use these instead of redefining component styles. |
| `SKILL.md` | Agent-Skill-compatible front-matter so a downloaded copy of this folder works as a Claude Code skill. |
| `fonts/` | Space Grotesk (400/500/600/700), IBM Plex Mono (400/500), Hellix (400/500/600), Fira Code (variable 300–700). |
| `assets/app-icon.png`, `assets/app-name.png` | Brand mark + wordmark, full-res from Figma. `app-icon.png` is also the favicon/window icon. |
| `preview/` | Design-system specimen cards rendered in the Design System tab. One concept per card. |
| `ui_kits/slidegenerator/` | Canonical Studio UI kit entry — topbar tabs, Studio subtabs, workflow hierarchy, and links to component previews. |

## CONTENT FUNDAMENTALS

SlideGenerator is a **technical desktop tool for power users** — researchers, ops, and student-org admins running batched slide generation. The copywriting reflects that: precise, neutral, abbreviation-heavy where the audience already knows the term.

**Voice:** plain, declarative, factual. No marketing prose. No questions to the user.

**Tone:** patient. Errors are descriptive, not blamy ("Failed to load placeholders." not "Oops! Something went wrong."). Success states are minimal and confirmatory ("Configuration imported successfully.", "Server restarted.").

**Casing:**
- UI labels are **Title Case**: "Create Task", "Image Replacement", "Max Retries", "Save", "Browse".
- Section headings are **Title Case**: "Appearance Settings", "Server Settings".
- Sentence-cased prose runs in tooltips and hints: "Choose your theme", "Max chunks per file", "0 = unlimited".
- Status pills are **Title Case** single words: "Processing", "Paused", "Completed", "Error", "Pending", "Cancelled".

**Person:** **Imperative ("Browse", "Save", "Reset to defaults") for actions; third-person impersonal ("Configuration saved.", "Server restarted.") for confirmations.** No "you", no "we", no "your". The app is a tool, not a teammate.

**Punctuation:** confirmations and errors end with a period. Button labels do not. Hints end with a period.

**Bilingual:** the product ships English + Tiếng Việt (Vietnamese) and labels them by their endonyms in the language picker. New copy must be addable to both `en.ts` and `vi.ts` translation tables.

**Emoji:** essentially **never**. There is exactly one in the entire string table — a single ❤️ in the About page ("Made with ASP.NET, React, Tauri with ❤️."). Don't add more.

**Numbers + units:**
- File extensions in parentheses with type: `PowerPoint template (.pptx, .potx)`, `Data file (.xlsx, .xlsm)`.
- Units in parens, lower-case: `Speed limit (bytes/s)`, `Retry timeout (sec)`, `Max dimension (px)`.
- Zero means unlimited for limit-style inputs: hint `0 = unlimited`.

**Example copy (verbatim from `src/shared/locales/en.ts`):**

> Create Task · Browse · Output folder: · `Select or enter template path...` · Select all · Selected · Total rows · Text Replacement · Image Replacement · Add · Limit reached (max) · Are you sure you want to clear all data? · Cannot connect to local server · Connected to local server · Software to generate PowerPoint slides from spreadsheet data.

## VISUAL FOUNDATIONS

The surface comes from the unsloth-studio theme. SlideGenerator-v2 adopts it wholesale; SlideGenerator-v1 (warm cream + steel-teal) is **deprecated** — do not pull values from it.

### Color

- **Background.** Light: pure white (`oklch(1 0 0)`). Dark: `#1a1b1e` (warm-neutral charcoal, NOT pure black). Cards sit one step lifted: light `oklch(1 0 0)` over the body; dark `#222427` over the body.
- **Foreground.** Light: near-black `oklch(0.27 0 0)`. Dark: `#d4d4d4`. Both biased slightly toward warm; no pure black, no pure white text.
- **Primary (UI accent).** A muted blue: `oklch(0.6929 0.1396 250)` — same value in light + dark. Used on the primary button, focus ring, active sidebar item, sparkline series 1, link color. (Previously a teal-green at hue ~166; hue rotated to ~250 to align the UI accent with the navy/cyan brand mark while keeping the same L/C, so contrast ratios are unchanged.)
- **Brand mark.** The logo itself uses navy `#1a2785` and cyan `#1f7ef0`. The UI primary above is tuned to sit harmoniously with the mark; reserve the raw brand hexes for the mark surfaces themselves (favicons, install splash) and use `--primary` for accent surfaces in product chrome.
- **Status.** Destructive `oklch(0.6368 0.2078 25.33)` (warm red). A productized `slide-success / -warning / -error / -info` set is exposed in `colors_and_type.css` for analytics + status pills.
- **Borders are quiet.** `--border` is `oklch(0.92 0.01 250)` on light, `#2e3035` on dark — designed to read as a hairline against the card, never as a structural rule. Use `ring-1` instead of `border` for cards to keep edges crisp at the 18px radius.

### Type

- **Sans.** Space Grotesk, 400/500/600/700. Used for body, labels, controls.
- **Heading.** Hellix, 400/500/600. Pulled from unsloth-studio. Applied at `h1`–`h4` via `--font-heading`. Slightly more geometric than Space Grotesk and reads better at display sizes (32px+). Falls back to Space Grotesk if missing.
- **Mono.** IBM Plex Mono, 400/500. For file paths, hangfire/job IDs, and replacement-placeholder names in UI chrome.
- **Code.** Fira Code (variable 300–700). Reserved for fenced code blocks — keep ligatures on (`font-feature-settings: "liga" 1, "calt" 1`). Don't use Fira Code for ordinary UI mono — IBM Plex Mono renders the same widths without ligature surprises.
- **Body baseline 14px (`text-base`).** Headings go up to 36px (`text-4xl`). The product UI never goes below 12px except for hint text under inputs.
- **Tracking is slightly negative everywhere.** `--tracking-normal: -0.01em`. Headings tighten further to `-0.02em`. Optical bloom on the Space Grotesk semibold/bold weights pulls characters apart visually — the tightening compensates.
- **Antialiasing.** `-webkit-font-smoothing: antialiased` + `text-rendering: optimizeLegibility` are mandatory at the body level; the heading weights look bloated without them.

### Spacing + layout

- Base unit: `0.25rem` (4px). Spacing tokens go `1/2/3/4/5/6/8/10/12`. No half-units.
- **Two-column app shell.** Left rail 240–260px (sidebar), main content fluid, gap and outer padding both 24px.
- **Form rhythm.** Sections are cards with 20–24px internal padding and 16–20px stack gaps between fields. Two adjacent inputs share a row only when they're tightly related (host + port).
- **Inputs and buttons are 36–40px tall.** Pills: 22px tall (badges) and 20–22px (status chips).

### Background, surface, depth

- **No full-bleed images** anywhere in the app. No hero photography. No video.
- **No repeating patterns or textures.** No grain. No watermarks.
- **No gradients in the chrome.** Two narrow exceptions: status banners (a flat 90° gradient between two semantically-related colors for "Connected"/"Disconnected" toast banners — green→lime, red→orange), and the recharts area-fills which fade the chart-1 color to transparent.
- **No protection gradients** under content. Cards stand on their own ring.
- **Shadows are near-flat.** `--shadow-xs/sm` is sufficient for cards; `--shadow-md/lg` reserved for modals + popovers. The unsloth theme has all default shadow tokens at `0 0 0 / 0` — you should not see drop shadows on hover states. Use `ring-1` color shifts instead.
- **Transparency + blur.** Soft only — `bg-input/30` (the translucent input background) is the canonical pattern. Popovers don't blur. Modal overlays use a flat `rgba(0,0,0,0.5)` scrim, not a backdrop-filter.

### Corner radii

Aggressively rounded. Cards: 18px (`--radius`, 1.1rem). Buttons / inputs / badges / pills: `rounded-4xl` (~32px, effectively a pill at button heights). Modals: 22–26px. The exception is the brand mark icon container, which uses a softer 24% rounded square (~140 of 1018px).

unsloth-studio also opts into `@toolwind/corner-shape` to apply a **squircle** (`corner-squircle`) to cards — a soft superellipse instead of a circular arc. If your stack supports it, use it on top-level surfaces; otherwise, plain `border-radius` is the documented fallback and looks correct.

### Animation, motion, easing

- **Two durations only.** `--duration-fast: 150ms` for hover/press/state, `--duration-normal: 200ms` for layout/route. A `--duration-micro: 100ms` exists for color-only flickers (focus ring snap).
- **Two curves.** `--ease-out-quart: cubic-bezier(0.165, 0.84, 0.44, 1)` for entrances, `--ease-out-cubic: cubic-bezier(0.215, 0.61, 0.355, 1)` for state. (Emil Kowalski curves — pulled from the unsloth `:root`.)
- **No bounces, no overshoots, no springs.** No "boop" effects.
- **Fades + small translates** (4–6px) only. Toasts slide in from top with a translateY(-8px → 0) over 220ms. Modals scale from 0.96 → 1.
- **Respect `prefers-reduced-motion`** — the v1 CSS already does (`animation-duration: 0.01ms`); preserve that.

### Hover / press / focus

- **Hover.** Buttons darken via `bg-primary/80` (the primary color at 80% opacity over the card). Outline buttons brighten the input background from `30%` → `50%`. Sidebar items get `bg-muted` and shift right by 2px (the v1 detail — keep it; it's quiet and works).
- **Press.** No scale. No shadow. Default browser :active works fine; we don't ship a custom :active state.
- **Focus.** Ring 3px, color `var(--ring) / 50` (the primary at 50% opacity), with `var(--ring)` 1px on the border. **Never remove the ring.** Visible focus is non-negotiable; the kit relies on it for keyboard nav.
- **Disabled.** `opacity: 0.5`, `pointer-events: none`. No state-change colors.

### Borders + cards

- Cards use **`ring-1 ring-foreground/10`** (a translucent black ring), not `border`. This keeps edges crisp inside the very rounded 18px shape.
- The exception is form inputs, which use a solid `border` for the focus to attach to.
- Dividers inside cards are 1px solid `--border`, never dashed.

### Imagery

The product has no inline imagery in v2 — replacement preview thumbnails are user-provided photos rendered raw inside a shape-preview frame. The frame is flat: 1px border, 10px radius, neutral background, no shadow. No tone-mapping, no warm/cool bias.

### Layout rules

- **Fixed title bar** when running in Tauri (44px tall, drags the window, shows app icon + window title). Hidden in the web build.
- **Sidebar pins left.** Single rail, no second level. Footer of the sidebar has settings + about as 40px icon buttons.
- **Main content scrolls.** Sidebar does not.
- **Modal overlays cover the title bar.** Z-index of overlay is above the title-bar drag region; the user cannot accidentally drag while interacting.

## ICONOGRAPHY

SlideGenerator uses an **inline SVG icon registry** for the Studio UI kit. The active set lives in `app/studio-data.js` as the `ICON` object and is mirrored in `preview/icons.html` for review. Keep icon shapes stroke-based, 24px viewBox, currentColor, and visually aligned with Hugeicons-style rounded strokes.

`assets/app-icon.png` is used for favicon/window icon usage. The old sidebar/toolbar PNG icon set has been removed.

**Emoji**: as noted in CONTENT FUNDAMENTALS — exactly one ❤️ in About, otherwise zero. Do not introduce emoji into the icon set.

**Unicode chars as icons**: never. There is one `·` separator in process status, otherwise glyphs come from the inline SVG set.

**Substitution policy.** If you need a glyph the inline registry doesn't ship, add it to `ICON` and mirror it in `preview/icons.html`. Use Lucide only as a temporary stroke-compatible reference and flag the substitution:

> Lucide substitute — replace with a Studio SVG icon when available.

Do **not** pull from Heroicons (heavier strokes), Material Icons (fill style mismatch), or any emoji set.

The unsloth-studio reference uses [Hugeicons](https://hugeicons.com/) (`@hugeicons/react`). For SlideGenerator surfaces, match that rounded stroke character through the local inline SVG registry rather than shipping an icon font.
