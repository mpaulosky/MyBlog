
## 2025-07-14 — Tailwind Migration Skill Review

Conducted a detailed review of `/home/mpaulosky/.config/squad/.github/skills/tailwind-migration/SKILL.md` against the actual MyBlog project structure.

### Key Learnings

**Project structure confirmed:**
- Source lives under `src/Web/` (not `Web/` as the skill assumes) — every path in the skill is wrong
- Pages follow VSA: `src/Web/Features/BlogPosts/{List,Create,Edit,Delete}/` and `src/Web/Features/UserManagement/`
- No `Counter.razor` or `Weather.razor` — those are default Blazor template pages
- `App.razor` uses Blazor asset fingerprinting: `@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]` — not a plain href
- Bootstrap JS files exist in `wwwroot/lib/bootstrap/dist/js/` but are NOT referenced by `<script>` tags in `App.razor`
- NavMenu uses `<AuthorizeView>` blocks for role-gated links — must be preserved in any rewrite
- `ReconnectModal.razor` + `.razor.css` exist and use Bootstrap classes — not mentioned in skill
- Bootstrap Icons are embedded as inline SVG in `NavMenu.razor.css` background images

**Skill gaps identified (18 findings, 7 Critical):**
- All paths wrong (`./Web/` → `./src/Web/`)
- Reference files (app.css, MainLayout.razor, NavMenu.razor, pages/) do not exist — skill is a stub
- Dynamic theme classes not safelisted → purged in production
- No Blazor form validation CSS (`.valid`, `.invalid`, `.validation-message`)
- Wrong page list (template pages vs actual VSA pages)
- Tailwind v3 vs v4 ambiguity (description says v4+, package.json pins ^3.4.0)
- MSBuild target breaks CI without npm guard
- AuthorizeView guards not mentioned in NavMenu rewrite step

**Priority fixes for skill author:**
1. Fix all paths to `src/Web/`
2. Inline all reference file content directly in SKILL.md
3. Safelist dynamic theme classes in tailwind.config.js
4. Add Blazor validation CSS and preserve AuthorizeView
5. Resolve v3/v4 version ambiguity and fix content glob

**Findings written to:** `.squad/decisions/inbox/aragorn-tailwind-skill-review.md`
