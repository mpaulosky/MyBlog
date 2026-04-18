// Copyright (c) 2024-2025. MyBlog Project.
// Theme management for color and brightness preferences
// Ported from IssueTrackerApp theme architecture
// SPDX-License-Identifier: MIT

/**
 * Theme Manager - Handles color + brightness theme combinations
 * 4 colors (blue, red, green, yellow) × 2 brightness (light, dark) = 8 themes
 * Stores preferences in localStorage and applies classes to the DOM
 */
window.themeManager = {
	THEMES: [
		'theme-blue-light', 'theme-blue-dark',
		'theme-red-light', 'theme-red-dark',
		'theme-green-light', 'theme-green-dark',
		'theme-yellow-light', 'theme-yellow-dark'
	],

	COLORS: ['blue', 'red', 'green', 'yellow'],
	BRIGHTNESS: ['light', 'dark'],
	STORAGE_KEY: 'tailwind-color-theme',
	DEFAULT_THEME: 'theme-blue-light',

	/**
	 * Gets the current color from localStorage
	 * @returns {string} The current color (blue, red, green, yellow)
	 */
	getColor: function () {
		var theme = this.getCurrentTheme();
		var parts = theme.replace('theme-', '').split('-');
		return parts[0] || 'blue';
	},

	/**
	 * Gets the current brightness from localStorage
	 * @returns {string} The current brightness (light, dark)
	 */
	getBrightness: function () {
		var theme = this.getCurrentTheme();
		var parts = theme.replace('theme-', '').split('-');
		return parts[1] || 'light';
	},

	/**
	 * Gets the full current theme name
	 * @returns {string} The current theme (e.g., 'theme-blue-light')
	 */
	getCurrentTheme: function () {
		return localStorage.getItem(this.STORAGE_KEY) || this.DEFAULT_THEME;
	},

	/**
	 * Determines if dark mode is active
	 * @returns {boolean} True if brightness is 'dark'
	 */
	isDarkMode: function () {
		return this.getBrightness() === 'dark';
	},

	/**
	 * Sets the color while preserving current brightness
	 * @param {string} color - The color: 'blue', 'red', 'green', 'yellow'
	 */
	setColor: function (color) {
		var brightness = this.getBrightness();
		this.setTheme('theme-' + color + '-' + brightness);
	},

	/**
	 * Sets the brightness while preserving current color
	 * @param {string} brightness - The brightness: 'light' or 'dark'
	 */
	setBrightness: function (brightness) {
		var color = this.getColor();
		this.setTheme('theme-' + color + '-' + brightness);
	},

	/**
	 * Sets the full theme and applies it
	 * @param {string} themeName - Full theme name (e.g., 'theme-blue-light')
	 */
	setTheme: function (themeName) {
		if (this.THEMES.indexOf(themeName) === -1) {
			themeName = this.DEFAULT_THEME;
		}

		var html = document.documentElement;

		// Remove all theme classes
		for (var i = 0; i < this.THEMES.length; i++) {
			html.classList.remove(this.THEMES[i]);
		}

		// Add the selected theme class
		html.classList.add(themeName);
		localStorage.setItem(this.STORAGE_KEY, themeName);

		// Sync dark/light class for Tailwind dark: variant
		var isDark = themeName.includes('-dark');
		if (isDark) {
			html.classList.add('dark');
			html.classList.remove('light');
		} else {
			html.classList.remove('dark');
			html.classList.add('light');
		}
	},

	/**
	 * Applies the saved theme from localStorage
	 */
	applyTheme: function () {
		var theme = this.getCurrentTheme();
		this.setTheme(theme);
	},

	/**
	 * Watches for system preference changes and notifies Blazor
	 * @param {object} dotNetHelper - The DotNet object reference for callbacks
	 */
	watchSystemPreference: function (dotNetHelper) {
		var self = this;
		var mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

		var handler = function (e) {
			if (dotNetHelper) {
				dotNetHelper.invokeMethodAsync('OnSystemPreferenceChanged', e.matches);
			}
		};

		if (mediaQuery.addEventListener) {
			mediaQuery.addEventListener('change', handler);
		} else {
			mediaQuery.addListener(handler);
		}
	},

	/**
	 * Gets the display-friendly current theme label
	 * @returns {string} e.g., "BLUE Light"
	 */
	getCurrentLabel: function () {
		var color = this.getColor().toUpperCase();
		var brightness = this.getBrightness();
		return color + ' ' + brightness.charAt(0).toUpperCase() + brightness.slice(1);
	},

	/**
	 * Initializes the theme on page load
	 */
	initialize: function () {
		this.applyTheme();
	},

	/**
	 * Marks ThemeProvider as fully initialized and interactive.
	 * Called by Blazor components after JS interop succeeds.
	 */
	markInitialized: function () {
		document.documentElement.setAttribute('data-theme-ready', 'true');
	}
};

// Initialize theme immediately to prevent flash of unstyled content
window.themeManager.initialize();
