# PR #295 Review Fix Decisions

**Agent:** Legolas  
**Date:** 2026-05-07  
**Branch:** squad/291-input-css-fine-tuning  

## Decisions Made

### `.container-card` gains `mx-auto px-4`

The `.container-card` utility was bare `max-w-7xl` with no centering or padding. Aligned it to match the app's shared layout pattern (consistent with how `nav` wraps `mx-auto max-w-7xl px-4`). Any future page wrapper that adopts `.container-card` will automatically center and pad correctly.

### `.btn-secondary` is a solid blue, not an outline button

The comment said "outline style" but the implementation was always solid blue fill. Chose to fix the comment (not the style) to preserve existing UX. The solid blue secondary button is the canonical pattern going forward.

### `PageHeadingComponent` falls back to `<h1>` on unknown `Level`

Added a `default` switch arm that renders `<h1>`. Predictable output over silent empty rendering.

### All four button variants use fixed palettes (no theme tokens)

All of `.btn-primary` (green), `.btn-secondary` (blue), `.btn-warning` (amber), `.btn-destructive` (red) use fixed Tailwind colour classes. None follow the user's colour-theme switch. The history.md learning entry from Issue #292 incorrectly stated that primary/secondary used `var(--primary-*)` tokens — corrected.
