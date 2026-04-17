# Legolas — Agent History

## 2025-07-19 — Tailwind Migration Skill Review

### What I Learned

**Project structure (confirmed by file system inspection):**
- MyBlog uses VSA: feature pages are under `src/Web/Features/BlogPosts/{Create,Edit,List,Delete}/` and `src/Web/Features/UserManagement/`
- The default Blazor template pages (Home, Counter, Weather, Error, NotFound) DO exist at `src/Web/Components/Pages/` — so the skill's Step 6 table is partially correct, but misses all Feature pages
- `app.css` currently contains Blazor validation class styles (`.valid.modified`, `.invalid`, `.validation-message`) — these must be preserved through any CSS migration
- `NavMenu.razor` uses `<AuthorizeView Roles="Author,Admin">` and `<AuthorizeView Roles="Admin">` — auth-aware nav is already implemented and must be preserved

**Tailwind migration skill gaps (documented in inbox review):**
1. **v3 vs v4 conflict:** Skill header says "v4+" but package.json specifies `^3.4.0` with v3 directives. These are incompatible — one approach must be chosen explicitly.
2. **Content path mismatch:** `tailwind.config.js` content array uses `./Web/Components/**` but the project is under `./src/Web/` and Feature pages are under `./src/Web/Features/` — all feature page classes will be purged.
3. **Reference files missing:** The skill directory contains only `SKILL.md`. Steps 3–6 reference `./references/app.css`, `./references/MainLayout.razor`, `./references/NavMenu.razor`, `./references/pages/` — none of these exist.
4. **Form validation styles lost:** Replacing `app.css` will silently break Blazor's EditForm validation visual feedback unless a `@layer components` block is explicitly added.
5. **AuthorizeView not in NavMenu template:** The skill doesn't mention preserving `<AuthorizeView>` wrappers in the rewritten nav.
6. **Hamburger state management:** Skill doesn't specify CSS-peer approach vs Blazor `@onclick` — pure CSS peer is preferred for layout components.
7. **`.active` class not styled:** Blazor's NavLink adds `.active` automatically; Tailwind has no default for it.
8. **JS interop pattern missing:** `window.setTheme()` / `window.toggleDark()` calls from Blazor need `IJSRuntime` guidance.
9. **Feature pages not in Step 6:** `Create.razor`, `Edit.razor`, `ManageRoles.razor` all have Bootstrap classes that need migration.
10. **`dotnet watch` hot reload:** MSBuild target won't re-run during hot reload — `npm run tw:watch` is mandatory in parallel.
11. **ARIA attributes:** Bootstrap components include ARIA by default; Tailwind is utility-only so all ARIA must be manually added.

**Key Blazor-specific rules learned/confirmed:**
- Blazor `NavLink` emits `active` CSS class on current route — must style explicitly in Tailwind
- Blazor `EditForm` emits `.valid`, `.invalid`, `.modified`, `.validation-message` — Bootstrap styles these, Tailwind does not
- In Blazor Server, `IJSRuntime` is required for JS calls from `@onclick` handlers; `onclick=""` HTML attribute works for static calls but doesn't integrate with Blazor's event model
- The inline `<script>` for theme init in `App.razor` should come BEFORE `blazor.server.js` to avoid theme flash on load
- Layout components (MainLayout, NavMenu) render before full Blazor hydration — prefer CSS-only or JS-attribute approaches over `@onclick` for non-data-bound interactions
- Blazor Server circuit reconnect does NOT re-run JS in `<script>` tags in component markup, but the static shell's scripts (in `App.razor`) persist

**Filed:** `.squad/decisions/inbox/legolas-tailwind-skill-review.md`

---

## 2025-04-17 — Tailwind CSS v4.2 Migration Complete

### What I Learned

**Successful Tailwind v4 CSS-First Migration:**
- Tailwind v4's CSS-first configuration using `@import "tailwindcss"`, `@source`, `@custom-variant`, and `@theme inline` directives works perfectly with Blazor
- No `tailwind.config.js` file needed - everything configured in `app.css`
- `@source inline("...")` prevents purging of dynamically-applied classes (theme-red, theme-blue, etc.)
- Semantic token system (`bg-surface`, `text-content`, `bg-primary`) backed by CSS custom properties eliminates need for inline styles
- MSBuild integration requires `WorkingDirectory="$(MSBuildProjectDirectory)/../.."` to navigate from `src/Web/` to repository root where `package.json` exists

**Files Successfully Migrated to Tailwind:**
1. **Layout Components:**
   - `MainLayout.razor` - Replaced sidebar layout with vertical flexbox, footer with semantic tokens
   - `NavMenu.razor` - Replaced sidebar with horizontal top bar, pure CSS hamburger (peer checkbox), theme switcher with 4 colors, dark mode toggle
   - `ReconnectModal.razor.css` - Updated hardcoded colors to semantic tokens (`var(--color-primary)`, `var(--color-surface)`, etc.)

2. **Feature Pages:**
   - `Features/BlogPosts/List/Index.razor` - Migrated table with striping, alerts, buttons
   - `Features/BlogPosts/Create/Create.razor` - Migrated form controls, labels, buttons, alerts
   - `Features/BlogPosts/Edit/Edit.razor` - Migrated form controls, added concurrency conflict alert styling
   - `Features/BlogPosts/Delete/ConfirmDeleteDialog.razor` - Replaced Bootstrap modal with Tailwind fixed overlay + centered dialog
   - `Features/UserManagement/ManageRoles.razor` - Migrated table, outline button variants (green/red borders)

3. **Template Pages:**
   - `Components/Pages/Counter.razor` - Migrated button
   - `Components/Pages/Weather.razor` - Migrated table with semantic tokens

**Bootstrap Class Mappings Applied:**
- `btn btn-primary` → `px-4 py-2 rounded font-medium text-white bg-primary hover:bg-primary-hover transition`
- `btn btn-secondary` → `px-4 py-2 rounded font-medium border border-edge text-content hover:bg-surface transition`
- `btn btn-danger` → `px-4 py-2 rounded font-medium bg-red-600 text-white hover:bg-red-700 transition`
- `btn btn-sm` → `px-3 py-1 text-sm` (size variant)
- `btn-outline-success` → `border border-green-600 text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20`
- `btn-outline-danger` → `border border-red-600 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20`
- `form-control` → `w-full rounded border border-edge bg-surface text-content px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary`
- `form-label` → `block text-sm font-medium text-content mb-1`
- `alert alert-danger` → `rounded-lg border border-red-300 bg-red-50 text-red-700 px-4 py-3 text-sm` with `role="alert"`
- `alert alert-warning` → `rounded-lg border border-yellow-300 bg-yellow-50 text-yellow-700 px-4 py-3 text-sm`
- `table` → `w-full text-sm text-left text-content` wrapped in `rounded-lg shadow bg-surface overflow-hidden`
- `table-striped` → `odd:bg-canvas even:bg-surface` on `<tr>` elements
- `modal` → `fixed inset-0 z-[100] flex items-center justify-center bg-black/50` with centered dialog

**Theme System Implementation:**
- 4 color themes (red, blue, green, yellow) implemented via CSS custom properties on `body.theme-{color}`
- Dark mode via `html.dark` class with separate CSS variable values
- Theme state stored in localStorage ("darkMode", "colorTheme")
- JavaScript in `App.razor` runs before Blazor hydration to prevent flash
- Blazor components call via IJSRuntime: `JS.InvokeVoidAsync("setTheme", "theme-red")` and `JS.InvokeVoidAsync("toggleDark")`
- No inline `style=""` attributes used - all styling via Tailwind utility classes backed by CSS variables

**Modal Pattern for Blazor:**
- Fixed overlay: `fixed inset-0 z-[100] flex items-center justify-center bg-black/50`
- Dialog: `rounded-lg shadow-xl p-6 max-w-md w-full mx-4 bg-surface border border-edge`
- Click overlay to close: `@onclick="OnCancel"` on overlay with `@onclick:stopPropagation="true"` on dialog
- Conditional render: `@if (IsVisible)` - entire overlay+dialog structure

**Build Configuration:**
- MSBuild target `BuildTailwind` runs before `Build` target (skipped when `CI=true`)
- For hot reload during development: must run `npm run tw:watch` in parallel terminal
- MSBuild target only runs on full builds, not incremental hot reloads
- Fixed `WorkingDirectory` to navigate from project directory to repo root

**Cleanup Completed:**
- Deleted `src/Web/Components/Layout/MainLayout.razor.css`
- Deleted `src/Web/Components/Layout/NavMenu.razor.css`
- Kept `ReconnectModal.razor.css` but updated colors to semantic tokens
- Deleted `src/Web/wwwroot/lib/bootstrap/` directory
- Removed Bootstrap CSS link from `App.razor`, added Tailwind CSS link
- Verified no Bootstrap classes remain in Razor files (final grep confirmed)

**Build Verification:**
- `dotnet build src/Web/Web.csproj` succeeded
- Tailwind CSS compiled successfully (28KB output)
- No Razor compilation errors
- All semantic token classes properly recognized by Tailwind compiler

**Challenges and Solutions:**
- MSBuild `WorkingDirectory` initially used `$(SolutionDir)` which was undefined - fixed by using `$(MSBuildProjectDirectory)/../..` for relative path
- Modal backdrop required `@onclick:stopPropagation="true"` on dialog to prevent closing when clicking dialog content
- Outline button variants required explicit dark mode hover states (`dark:hover:bg-green-900/20`) for visibility

**Key Success Factors:**
- CSS-first approach with semantic tokens eliminated all inline styles
- Pure CSS peer checkbox for mobile nav (no Blazor state) works reliably through reconnects
- Theme JavaScript before Blazor hydration prevents flash
- All AuthorizeView roles preserved exactly as specified
- ARIA attributes added where Bootstrap provided them automatically
- Build integration works for full builds, developer hot reload workflow documented

**Filed:** `.squad/decisions/inbox/legolas-tailwind-migration-complete.md`

---

## 2025-07-19 — CSS Folder Restructure (wwwroot/css/)

## Learnings

**CSS output folder moved to wwwroot/css/:**
- `app.css` (source) and `tailwind.css` (compiled output) both moved from `wwwroot/` root to `wwwroot/css/`
- The `@source` directives in `app.css` are relative to the CSS file location — moving one folder deeper requires changing `"../Components/**"` to `"../../Components/**"` and `"../Features/**"` to `"../../Features/**"`
- `App.razor` link tag changes from `href="tailwind.css"` to `href="css/tailwind.css"` (no leading slash needed — Blazor resolves from base href)
- `App.razor` `@Assets["app.css"]` also changes to `@Assets["css/app.css"]` for fingerprinting
- `package.json` scripts at repo root must update both `-i` and `-o` paths
- `.gitignore` entry must also be updated to `src/Web/wwwroot/css/tailwind.css`
- `dotnet build` confirmed: Tailwind CLI compiled 64ms, build succeeded with 0 warnings

**wwwroot/lib/ was NOT empty — it contained full Bootstrap distribution:**
- `lib/bootstrap/dist/css/` had ~20 Bootstrap CSS files (full, minified, RTL, grid, reboot, utilities)
- `lib/bootstrap/dist/js/` had ~10 Bootstrap JS files (bundle, ESM, regular)
- These were installed by libman and were stale after migration — deleted in this session
- `rm -rf src/Web/wwwroot/lib/` removed all 60+ files in one operation

**SKILL.md updates applied:**
- All `wwwroot/app.css` / `wwwroot/tailwind.css` references updated to `wwwroot/css/`
- @source directives in Step 3 template updated to `../../` depth
- Step 8 cleanup now includes `rm -rf src/Web/wwwroot/lib/` after Bootstrap removal
- Files table updated with correct paths and lib/ entry changed to full directory

**Filed:** `.squad/decisions/inbox/legolas-css-folder-restructure.md`

