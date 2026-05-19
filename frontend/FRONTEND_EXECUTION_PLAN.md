# FRONTEND EXECUTION PLAN
> Auto-generated. Updated after each completed step. Status: [Pending] | [Doing] | [Done] | [Skip] | [Error]

## PHASE 0: SCAFFOLDING

| # | Task | Status |
|---|---|---|
| 0.1 | `npm create vite@latest . -- --template react-ts` (nếu cần force) | [Pending] |
| 0.2 | Install core deps: `@tanstack/react-router @tanstack/router-plugin @tanstack/router-devtools` | [Pending] |
| 0.3 | Install styling: `tailwindcss @tailwindcss/vite` | [Pending] |
| 0.4 | Install UI: `class-variance-authority clsx tailwind-merge` | [Pending] |
| 0.5 | Install state/form: `zustand zundo react-hook-form @hookform/resolvers zod` | [Pending] |
| 0.6 | Install canvas: `@xyflow/react @dagrejs/dagre @dagrejs/graphlib` | [Pending] |
| 0.7 | Install icons/animation: `@hugeicons/react motion next-themes` | [Pending] |
| 0.8 | Install i18n: `i18next react-i18next` | [Pending] |
| 0.9 | Install markdown: `react-markdown remark-gfm` | [Pending] |
| 0.10 | Install dev: `@biomejs/biome @vitejs/plugin-react` | [Pending] |
| 0.11 | Configure `vite.config.ts` (plugins: react + @tailwindcss/vite + @tanstack/router-plugin/vite) | [Pending] |
| 0.12 | `npx shadcn@latest init` (TW4 mode, neutral base) → generates `components.json` + CSS vars in `globals.css` | [Pending] |
| 0.13 | Configure `biome.json` (strict, import ordering) | [Pending] |
| 0.14 | Configure `tsconfig.json` paths `@/*` → `./src/*` | [Pending] |

## PHASE 1: DESIGN SYSTEM + TYPES

| # | Task | Status |
|---|---|---|
| 1.1 | Copy `fonts/` → `public/fonts/`, `assets/app-*.png` → `public/` | [Pending] |
| 1.2 | Merge `colors_and_type.css` + `components.css` tokens vào `src/styles/globals.css` (Tailwind @theme layer) | [Pending] |
| 1.3 | `src/types/domain.ts` — identifiers, enums | [Pending] |
| 1.4 | `src/types/workflow.ts` — GeneratingRequest/Summary/Progress | [Pending] |
| 1.5 | `src/types/recipe.ts` — RecipeEntry, MapNode, Instructions, CommentNodeData, summaries | [Pending] |
| 1.6 | `src/types/settings.ts` — Setting/Network/Performance | [Pending] |
| 1.7 | `src/types/ipc.ts` — Rpc constants | [Pending] |
| 1.8 | Setup i18next: `src/lib/i18n.ts` + locale files `vi.json` + `en.json` per namespace | [Pending] |

## PHASE 2: MOCKS + UTILS

| # | Task | Status |
|---|---|---|
| 2.1 | `src/lib/utils.ts` — cn(), formatDate(), formatRelative() | [Pending] |
| 2.2 | `src/lib/icons.ts` — map hugeicons + inline SVG fallbacks | [Pending] |
| 2.3 | `src/lib/mock-stream.ts` — simulate GeneratingProgress event sequence | [Pending] |
| 2.4 | `src/config/defaults.ts` — defaultGeneratingRequest, defaultSetting | [Pending] |
| 2.5 | `features/recipes/mocks/workbook-summary.mock.ts` | [Pending] |
| 2.6 | `features/recipes/mocks/presentation-summary.mock.ts` | [Pending] |
| 2.7 | `features/recipes/mocks/worksheet-preview.mock.ts` | [Pending] |
| 2.8 | `features/recipes/mocks/recipes.mock.ts` — 5 recipes với full graph JSON | [Pending] |
| 2.9 | `features/studio/mocks/workflows.mock.ts` | [Pending] |
| 2.10 | `features/studio/mocks/progress.mock.ts` | [Pending] |
| 2.11 | `features/studio/mocks/log-entries.mock.ts` | [Pending] |
| 2.12 | `features/settings/mocks/settings.mock.ts` | [Pending] |

## PHASE 3: SHADCN PRIMITIVES + SHARED ATOMS

| # | Task | Status |
|---|---|---|
| 3.1 | `npx shadcn add button card dialog tabs badge input select switch slider dropdown-menu form table separator scroll-area sheet tooltip sonner popover command context-menu` | [Pending] |
| 3.2 | `src/components/status-dot.tsx` | [Pending] |
| 3.3 | `src/components/tab-pill.tsx` — animated sliding indicator | [Pending] |
| 3.4 | `src/components/subtab-bar.tsx` — underline + status dot | [Pending] |
| 3.5 | `src/components/page-header.tsx` | [Pending] |
| 3.6 | `src/components/empty-state.tsx` | [Pending] |

## PHASE 4: APP SHELL + ROUTING

| # | Task | Status |
|---|---|---|
| 4.1 | `src/app/providers/app-providers.tsx` — ThemeProvider + RouterProvider | [Pending] |
| 4.2 | `src/app/routes/__root.tsx` — shell, topbar, outlet | [Pending] |
| 4.3 | `src/components/app-topbar.tsx` — logo + tab pill + theme switcher 3-mode + settings icon | [Pending] |
| 4.4 | `src/app/routes/index.tsx` — redirect → /recipes | [Pending] |
| 4.5 | `src/app/routes/splash.tsx` — animation rồi auto-redirect | [Pending] |
| 4.6 | `src/app/routes/about.tsx` — About page | [Pending] |

## PHASE 5: RECIPES GALLERY

| # | Task | Status |
|---|---|---|
| 5.1 | `src/features/recipes/hooks/use-recipes-store.ts` — Zustand CRUD list | [Pending] |
| 5.2 | `src/features/recipes/components/recipe-card.tsx` | [Pending] |
| 5.3 | `src/features/recipes/components/recipe-import-dialog.tsx` | [Pending] |
| 5.4 | `src/features/recipes/components/recipe-export-button.tsx` | [Pending] |
| 5.5 | `src/app/routes/recipes.tsx` — gallery page | [Pending] |

## PHASE 6: RECIPE EDITOR CANVAS (phức tạp nhất)

| # | Task | Status |
|---|---|---|
| 6.1 | `use-editor-store.ts` — Zustand + zundo, onNodesChange/onEdgesChange/onConnect với applyNodeChanges/applyEdgeChanges/addEdge | [Pending] |
| 6.2 | `use-editor-undo-redo.ts`, `use-validation.ts`, `use-recipe-keyboard.ts` (Ctrl+S/Z/Y) | [Pending] |
| 6.3 | `editor-canvas.tsx` — ReactFlow + BackgroundVariant.Dots + smooth edges + dagre auto-layout | [Pending] |
| 6.4 | Controls: `zoom-bar.tsx` (bottom-left), `validate-panel.tsx` (top-left), `add-node-popover.tsx` (right) | [Pending] |
| 6.5 | `workbook-node.tsx` + `workbook-config-dialog.tsx` (filePath + password + mock "Tải" → spawn WorksheetNodes) | [Pending] |
| 6.6 | `worksheet-node.tsx` (child, auto-spawn) + `worksheet-preview-dialog.tsx` (20-row table) | [Pending] |
| 6.7 | `presentation-node.tsx` + `presentation-config-dialog.tsx` (mock "Tải" → spawn SlideNodes) | [Pending] |
| 6.8 | `slide-node.tsx` (child) + `slide-preview-dialog.tsx` (zoom/pan + TEXT/IMAGE labels) | [Pending] |
| 6.9 | `map-node.tsx` + `map-config-dialog.tsx` (column↔placeholder mapping + ROI section + tooltip GIF) | [Pending] |
| 6.10 | `comment-node.tsx` (markdown, resizable, no header) | [Pending] |
| 6.11 | `editor-header.tsx` (name inline-edit + save status + Import/Export + undo/redo) | [Pending] |
| 6.12 | `breadcrumb-back.tsx` + `unsaved-changes-dialog.tsx` (guard beforeLeave) | [Pending] |
| 6.13 | `recipe-editor-page.tsx` — compose all, `src/app/routes/recipes.$id.tsx` | [Pending] |
| 6.14 | i18n keys cho toàn bộ editor — swap hardcoded strings | [Pending] |

## PHASE 7: STUDIO

| # | Task | Status |
|---|---|---|
| 7.1 | Studio shell + subtab bar (Cấu hình / Đang chạy / Kết quả) + routes | [Pending] |
| 7.2 | Studio Cấu hình: `generating-request-form.tsx` (react-hook-form + Zod) + recipe-selector | [Pending] |
| 7.3 | Studio Đang chạy: `workflow-card.tsx` + `phase-stepper.tsx` + mock stream | [Pending] |
| 7.4 | Studio Đang chạy: `workflow-tree.tsx` + `log-viewer.tsx` trong drawer | [Pending] |
| 7.5 | Studio Kết quả: completed table + filter + actions | [Pending] |

## PHASE 8: SETTINGS

| # | Task | Status |
|---|---|---|
| 8.1 | `settings-dialog.tsx` (820×560, Cmd+,) + icon-nav | [Pending] |
| 8.2 | Network tab (proxy + retry form) | [Pending] |
| 8.3 | Performance tab (5 sliders) | [Pending] |
| 8.4 | Appearance tab (theme 3-mode) + About tab | [Pending] |

## PHASE 9: VERIFY + POLISH

| # | Task | Status |
|---|---|---|
| 9.1 | npm run lint (biome) — pass clean | [Pending] |
| 9.2 | npm run build — no errors | [Pending] |
| 9.3 | Chrome test /splash → /recipes → /recipes/:id → editor flow | [Pending] |
| 9.4 | Chrome test /studio/* + settings dialog + theme switch | [Pending] |
| 9.5 | `MORNING_REPORT.md` | [Pending] |

---

*Last updated: Phase 0 starting*
