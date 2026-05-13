// Custom initialization wrapper for PSC.Blazor.Components.MarkdownEditor
// This ensures proper loading and initialization of the markdown editor

window.markdownEditorInterop = {
    editors: {},
    
    initialize: function (dotNetObjectRef, elementRef, elementId, options) {
        try {
            // Wait for EasyMDE to be available
            if (typeof EasyMDE === 'undefined') {
                console.error('EasyMDE is not loaded');
                return;
            }

            // Create the editor instance
            const textarea = document.getElementById(elementId);
            if (!textarea) {
                console.error('Textarea element not found:', elementId);
                return;
            }

            const editor = new EasyMDE({
                element: textarea,
                ...options
            });

            // Store the editor instance
            this.editors[elementId] = {
                editor: editor,
                dotNetRef: dotNetObjectRef
            };

            // Set up event handlers
            editor.codemirror.on('change', () => {
                const value = editor.value();
                dotNetObjectRef.invokeMethodAsync('OnEditorChanged', value);
            });

            console.log('MarkdownEditor initialized successfully:', elementId);
        } catch (error) {
            console.error('Error initializing markdown editor:', error);
        }
    },

    dispose: function (elementId) {
        if (this.editors[elementId]) {
            const editorData = this.editors[elementId];
            if (editorData.editor && editorData.editor.toTextArea) {
                editorData.editor.toTextArea();
            }
            delete this.editors[elementId];
            console.log('MarkdownEditor disposed:', elementId);
        }
    },

    getValue: function (elementId) {
        if (this.editors[elementId] && this.editors[elementId].editor) {
            return this.editors[elementId].editor.value();
        }
        return '';
    },

    setValue: function (elementId, value) {
        if (this.editors[elementId] && this.editors[elementId].editor) {
            this.editors[elementId].editor.value(value);
        }
    }
};

// Ensure the interop is available globally
console.log('Markdown Editor Interop Loaded');
