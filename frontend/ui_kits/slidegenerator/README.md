# SlideGenerator Studio UI Kit

The canonical UI kit entry is `index.html`. It hosts the Studio surface: topbar tabs (`Recipes`, `Studio`), Studio subtabs (`Cấu hình`, `Đang chạy`, `Kết quả`), the workflow hierarchy, configuration snapshot, log viewer, and local expand/collapse interactions.

## What this is

A static Studio prototype powered by the shared files in `../../app/`:

- `../../app/styles.css` — Studio-specific theme, layout, controls, hierarchy, and log styles.
- `../../app/studio-data.js` — mock recipes, workflow data, and inline SVG icon registry.
- `../../app/studio-app.js` — static interaction logic for tabs, theme, configuration, workflow controls, hierarchy expansion, row logs, and log copying.

The old React/Babel sidebar UI kit was removed. New SlideGenerator UI kit work should start from Studio, not the removed `*.jsx` screens.

## Component Links

The `Components` menu in the topbar links to the design-system specimens:

- `../../preview/icons.html`
- `../../preview/sidebar-items.html`
- `../../preview/buttons.html`
- `../../preview/cards.html`
- `../../preview/inputs.html`
- `../../preview/colors-primary.html`
- `../../preview/type-scale.html`
- `../../preview/motion.html`

These are normal HTML links so the kit works when opened directly from disk.

## What's Mocked

- No real recipe manager is wired behind the `Recipes` tab.
- File browsing, opening folders, and opening recipes use mock alerts or sample paths.
- Workflow progress, workbook data, worksheet data, row status, and logs are static mock data.
- Pause, resume, stop, delete, theme, and expand/collapse interactions are client-side only.
- Copy log uses `navigator.clipboard` when available and falls back to a temporary textarea.

## Canonical Entry

Use `ui_kits/slidegenerator/index.html` for the Studio UI kit.
