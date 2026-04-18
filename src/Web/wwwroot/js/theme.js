// Copyright (c) 2024-2025. MyBlog Project.
// Theme management for color and brightness preferences
// Simplified architecture: CSS custom property swap for color, native dark: variant for brightness
// SPDX-License-Identifier: MIT

/**
 * Theme Manager - Simplified color + brightness management
 * 4 colors (blue, red, green, yellow) × 2 brightness (light, dark)
 * Color controlled by :root.color-{name} class
 * Brightness controlled by Tailwind's native dark: variant via .dark class
 * Stores preferences in localStorage as two separate keys
 */
window.themeManager = {
	COLORS: ['blue', 'red', 'green', 'yellow'],
	BRIGHTNESS: ['light', 'dark'],
	COLOR_KEY: 'theme-color',
	BRIGHTNESS_KEY: 'theme-mode',
	DEFAULT_COLOR: 'blue',
	DEFAULT_BRIGHTNESS: 'light',

	/**
	 * Gets the current color from localStorage
	 * @returns {string} The current color (blue, red, green, yellow)
	 */
	getColor: function () {
		var color = localStorage.getItem(this.COLOR_KEY) || this.DEFAULT_COLOR;
		if (this.COLORS.indexOf(color) === -1) color = this.DEFAULT_COLOR;
		return color;
	},

	/**
	 * Gets the current brightness from localStorage
	 * @returns {string} The current brightness (light, dark)
	 */
	getBrightness: function () {
		var brightness = localStorage.getItem(this.BRIGHTNESS_KEY);
		if (!brightness) {
			// Default to system preference
			brightness = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
		}
		if (this.BRIGHTNESS.indexOf(brightness) === -1) brightness = this.DEFAULT_BRIGHTNESS;
		return brightness;
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
		if (this.COLORS.indexOf(color) === -1) color = this.DEFAULT_COLOR;
		
		var html = document.documentElement;
		
		// Remove all color classes
		for (var i = 0; i < this.COLORS.length; i++) {
			html.classList.remove('color-' + this.COLORS[i]);
		}
		
		// Add the selected color class
		html.classList.add('color-' + color);
		localStorage.setItem(this.COLOR_KEY, color);
	},

	/**
	 * Sets the brightness while preserving current color
	 * @param {string} brightness - The brightness: 'light' or 'dark'
	 */
	setBrightness: function (brightness) {
		if (this.BRIGHTNESS.indexOf(brightness) === -1) brightness = this.DEFAULT_BRIGHTNESS;
		
		var html = document.documentElement;
		
		// Toggle dark class for Tailwind's dark: variant
		if (brightness === 'dark') {
			html.classList.add('dark');
		} else {
			html.classList.remove('dark');
		}
		
		localStorage.setItem(this.BRIGHTNESS_KEY, brightness);
	},

	/**
	 * Applies the saved theme from localStorage
	 */
	applyTheme: function () {
		var color = this.getColor();
		var brightness = this.getBrightness();
		this.setColor(color);
		this.setBrightness(brightness);
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
	 * @returns {string} e.g., "Blue Dark"
	 */
	getCurrentLabel: function () {
		var color = this.getColor();
		var brightness = this.getBrightness();
		return color.charAt(0).toUpperCase() + color.slice(1) + ' ' + 
		       brightness.charAt(0).toUpperCase() + brightness.slice(1);
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
