---
name: slidegenerator-design
description: Use this skill to generate well-branded interfaces and assets for SlideGenerator — an end-to-end, automated, template-based PowerPoint presentation generator. Contains essential design guidelines, colors, type, fonts, assets, icon system, and the Studio UI kit for prototyping product surfaces, slide outputs, marketing, or production-bound code.
user-invocable: true
---

Read the README.md file within this skill, and explore the other available files.

If creating visual artifacts (slides, mocks, throwaway prototypes, etc), copy assets out and create static HTML files for the user to view. If working on production code, you can copy assets and read the rules here to become an expert in designing with this brand.

If the user invokes this skill without any other guidance, ask them what they want to build or design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_ production code, depending on the need.

## Quick reference

- **Product:** SlideGenerator — Tauri + React desktop app that fills PowerPoint templates from Excel workbooks. Bilingual EN/VI. Power-user tool.
- **Theme:** Mirrors `unsloth-studio` — neutral whites/charcoals, a single muted teal-green primary, very rounded (`1.1rem` cards, pill buttons).
- **Fonts:** Space Grotesk (body), Hellix (headings), IBM Plex Mono (UI mono), Fira Code (code blocks). All in `fonts/`.
- **Brand mark:** Navy + cyan pie + recycle-arrows. `assets/app-icon.png` + `assets/app-name.png`. Don't use brand colors as UI accent.
- **Icons:** Inline SVG set in `app/studio-data.js`, mirrored in `preview/icons.html`. `assets/app-icon.png` is also used for favicon/window icon usage.
- **Voice:** Plain, declarative, imperative for actions. No emoji (one ❤️ exception in About).

## Files

- `README.md` — content + visual + iconography guidelines.
- `colors_and_type.css` — base + semantic CSS variables. Light is default; `.dark` parent flips to dark.
- `components.css` — Button, Input, Badge, Card, Sidebar item primitives as plain CSS classes.
- `fonts/`, `assets/`, `preview/` — webfonts, brand assets + icons, design-system specimen cards.
- `ui_kits/slidegenerator/` — canonical Studio UI kit entry with topbar tabs, Studio subtabs, workflow hierarchy, and component preview links. Read its README for what's mocked.
