# Theming Guide

IssueTrackerApp supports dark mode and multiple color schemes with user preference persistence. This guide explains how to use and customize the theming system.

## Quick Start

### Toggle Dark Mode

Click the theme toggle button in the application header to cycle through:

- **Light** - Light background with dark text
- **Dark** - Dark background with light text
- **System** - Follows your operating system preference

### Change Color Scheme

Select from four built-in color schemes:

- **Blue** (default) - Professional blue tones
- **Red** - Warm red/rose tones
- **Green** - Natural green tones
- **Yellow** - Energetic amber/yellow tones

---

## How It Works

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    ThemeProvider.razor                       │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │  Cascading Parameter (provides theme state to children) │ │
│  └──────────────────────────┬──────────────────────────────┘ │
│                             │                                │
│  ┌──────────────────────────▼──────────────────────────────┐ │
│  │              ThemeProvider.razor.cs                      │ │
│  │  - ThemeMode: "light" | "dark" | "system"               │ │
│  │  - ColorScheme: "blue" | "red" | "green" | "yellow"     │ │
│  │  - IsDarkMode: computed from ThemeMode + system pref    │ │
│  └──────────────────────────┬──────────────────────────────┘ │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   JavaScript Interop                         │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │  themeManager.js                                        │ │
│  │  - getThemeMode() / setThemeMode()                      │ │
│  │  - getColorScheme() / setColorScheme()                  │ │
│  │  - shouldUseDarkMode()                                  │ │
│  │  - watchSystemPreference()                              │ │
│  └──────────────────────────┬──────────────────────────────┘ │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      localStorage                            │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │  "theme-mode": "light" | "dark" | "system"              │ │
│  │  "color-scheme": "blue" | "red" | "green" | "yellow"    │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Components

#### ThemeProvider

A cascading component that wraps the application and provides theme state:

```razor
<ThemeProvider>
    <Router AppAssembly="@typeof(App).Assembly">
        <!-- Application content -->
    </Router>
</ThemeProvider>
```

#### ThemeToggle

UI component for users to change theme settings:

```razor
<ThemeToggle />
```

---

## localStorage Keys

The following keys are used to persist user preferences:

| Key | Values | Default | Description |
|-----|--------|---------|-------------|
| `theme-mode` | `"light"`, `"dark"`, `"system"` | `"system"` | Controls dark/light mode |
| `color-scheme` | `"blue"`, `"red"`, `"green"`, `"yellow"` | `"blue"` | Controls primary color palette |

### Clearing Preferences

To reset to defaults, clear localStorage keys:

```javascript
localStorage.removeItem('theme-mode');
localStorage.removeItem('color-scheme');
```

---

## CSS Custom Properties

Color schemes are implemented using CSS custom properties in `src/Web/Styles/app.css`.

### Primary Color Scale

Each color scheme defines a full color scale (50-950):

```css
:root {
    --color-primary-50: oklch(97% 0.01 250);   /* Lightest */
    --color-primary-100: oklch(93% 0.03 250);
    --color-primary-200: oklch(87% 0.06 250);
    --color-primary-300: oklch(78% 0.10 250);
    --color-primary-400: oklch(68% 0.15 250);
    --color-primary-500: oklch(58% 0.19 250);  /* Base */
    --color-primary-600: oklch(50% 0.18 250);
    --color-primary-700: oklch(43% 0.15 250);
    --color-primary-800: oklch(37% 0.12 250);
    --color-primary-900: oklch(32% 0.09 250);
    --color-primary-950: oklch(24% 0.06 250);  /* Darkest */
    --color-primary: var(--color-primary-600);
}
```

### Theme Attribute Selectors

Color schemes are applied via `data-theme` attribute:

```css
[data-theme="red"] {
    --color-primary-500: oklch(58% 0.22 25);
    /* ... other shades */
}

[data-theme="green"] {
    --color-primary-500: oklch(58% 0.18 145);
    /* ... other shades */
}

[data-theme="yellow"] {
    --color-primary-500: oklch(65% 0.19 85);
    /* ... other shades */
}
```

### Dark Mode Adjustment

In dark mode, lighter shades are used as the primary color:

```css
.dark {
    --color-primary: var(--color-primary-400);
}
```

---

## Using Theme Colors in Components

### TailwindCSS Classes

Use the `primary` color in Tailwind classes:

```html
<!-- Background colors -->
<div class="bg-primary-500">Primary background</div>
<div class="bg-primary-100 dark:bg-primary-900">Adaptive background</div>

<!-- Text colors -->
<span class="text-primary-600 dark:text-primary-400">Primary text</span>

<!-- Border colors -->
<div class="border-primary-300">Primary border</div>

<!-- Hover states -->
<button class="bg-primary-500 hover:bg-primary-600">Button</button>
```

### Dark Mode Classes

TailwindCSS v4 dark mode uses the `dark:` variant:

```html
<div class="bg-white dark:bg-gray-900">
    <p class="text-gray-900 dark:text-gray-100">
        Adapts to dark mode
    </p>
</div>
```

---

## Adding a New Color Scheme

### Step 1: Define CSS Variables

Add a new `[data-theme="..."]` block in `src/Web/Styles/app.css`:

```css
/* Purple theme */
[data-theme="purple"] {
    --color-primary-50: oklch(97% 0.01 300);
    --color-primary-100: oklch(93% 0.03 300);
    --color-primary-200: oklch(87% 0.07 300);
    --color-primary-300: oklch(78% 0.12 300);
    --color-primary-400: oklch(68% 0.16 300);
    --color-primary-500: oklch(58% 0.18 300);
    --color-primary-600: oklch(50% 0.16 300);
    --color-primary-700: oklch(43% 0.13 300);
    --color-primary-800: oklch(37% 0.10 300);
    --color-primary-900: oklch(32% 0.08 300);
    --color-primary-950: oklch(24% 0.05 300);
    --color-primary: var(--color-primary-600);
}
```

### Step 2: Update ThemeProvider

Add the new scheme to valid options in `ThemeProvider.razor.cs`:

```csharp
/// <summary>
/// Sets the color scheme and persists it
/// </summary>
/// <param name="scheme">The color scheme: "blue", "red", "green", "yellow", or "purple"</param>
public async Task SetColorSchemeAsync(string scheme)
{
    // ... implementation
}
```

### Step 3: Update ThemeToggle UI

Add the new option to the color scheme selector in `ThemeToggle.razor`:

```razor
<select @onchange="OnColorSchemeChange">
    <option value="blue">Blue</option>
    <option value="red">Red</option>
    <option value="green">Green</option>
    <option value="yellow">Yellow</option>
    <option value="purple">Purple</option>  <!-- New -->
</select>
```

### Step 4: Rebuild CSS

```bash
cd src/Web
npm run css:build
```

---

## OKLCH Color Format

IssueTrackerApp uses [OKLCH](https://oklch.com/) color format for perceptually uniform color scales:

```
oklch(L% C H)
```

- **L** (Lightness): 0-100%
- **C** (Chroma): 0-0.4 (saturation intensity)
- **H** (Hue): 0-360 degrees

### Hue Reference

| Color | Hue Angle |
|-------|-----------|
| Red | 25° |
| Yellow | 85° |
| Green | 145° |
| Blue | 250° |
| Purple | 300° |

---

## JavaScript API Reference

### themeManager Object

```javascript
// Get current theme mode
const mode = themeManager.getThemeMode();  // "light" | "dark" | "system"

// Set theme mode
themeManager.setThemeMode("dark");

// Get current color scheme
const scheme = themeManager.getColorScheme();  // "blue" | "red" | etc.

// Set color scheme
themeManager.setColorScheme("green");

// Check if dark mode should be active
const isDark = themeManager.shouldUseDarkMode();  // boolean

// Watch for system preference changes
themeManager.watchSystemPreference(dotNetRef);
```

---

## Troubleshooting

### Theme Not Persisting

1. Check browser localStorage is enabled
2. Verify no browser extensions blocking storage
3. Check browser console for JavaScript errors

### Colors Not Updating

1. Ensure CSS is rebuilt after changes: `npm run css:build`
2. Clear browser cache
3. Verify `data-theme` attribute is applied to root element

### Dark Mode Flicker on Load

This is expected during server-side prerendering. The JavaScript runs after hydration to apply the correct theme. Consider:

1. Adding a CSS class to hide content until theme loads
2. Using inline script in `<head>` to apply theme before render

### System Preference Not Detected

Check browser support for `prefers-color-scheme` media query:

```javascript
if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
    console.log('System prefers dark mode');
}
```

---

## Browser Support

| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| CSS Custom Properties | ✅ | ✅ | ✅ | ✅ |
| OKLCH Colors | ✅ 111+ | ✅ 113+ | ✅ 15.4+ | ✅ 111+ |
| prefers-color-scheme | ✅ | ✅ | ✅ | ✅ |
| localStorage | ✅ | ✅ | ✅ | ✅ |

For older browsers without OKLCH support, consider adding fallback HSL values.
