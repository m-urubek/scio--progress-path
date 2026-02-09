/**
 * Rich Content Rendering interop functions for Progress Path
 * Provides JavaScript functionality for rendering LaTeX math expressions (via KaTeX)
 * and syntax-highlighted code blocks (via Prism.js).
 *
 * REQ-CHAT-010: Math rendering support (LaTeX/KaTeX)
 * REQ-CHAT-011: Code syntax highlighting support (Prism.js)
 */

/**
 * Renders all LaTeX math expressions within an element.
 * Supports inline math ($...$, \(...\)) and display/block math ($$...$$, \[...\]).
 */
function renderMath(element: HTMLElement): void {
    if (!element || typeof katex === 'undefined') {
        console.warn('KaTeX not loaded or element not provided');
        return;
    }

    // Find all elements with data-math attribute (pre-parsed LaTeX)
    const mathElements = element.querySelectorAll('[data-math]');

    mathElements.forEach((el) => {
        const latex = el.getAttribute('data-math');
        const isBlock = el.classList.contains('math-block');

        if (latex && !el.hasAttribute('data-rendered')) {
            try {
                katex.render(latex, el as HTMLElement, {
                    throwOnError: false,
                    displayMode: isBlock,
                    output: 'html',
                    strict: false,
                    trust: true,
                    macros: {
                        // Common macros for educational use
                        "\\R": "\\mathbb{R}",
                        "\\N": "\\mathbb{N}",
                        "\\Z": "\\mathbb{Z}",
                        "\\Q": "\\mathbb{Q}"
                    }
                });
                el.setAttribute('data-rendered', 'true');
            } catch (error) {
                console.error('KaTeX render error:', error);
                // On error, show the original LaTeX in a styled span
                (el as HTMLElement).innerHTML = `<span class="katex-error" title="LaTeX error">${escapeHtml(latex)}</span>`;
            }
        }
    });
}

/**
 * Highlights all code blocks within an element using Prism.js.
 */
function highlightCode(element: HTMLElement): void {
    if (!element || typeof Prism === 'undefined') {
        console.warn('Prism not loaded or element not provided');
        return;
    }

    // Find all code elements that haven't been highlighted yet
    const codeBlocks = element.querySelectorAll('pre code:not(.prism-highlighted)');

    codeBlocks.forEach((codeEl) => {
        // Mark as highlighted to prevent re-processing
        codeEl.classList.add('prism-highlighted');

        // Highlight with Prism
        Prism.highlightElement(codeEl);
    });
}

/**
 * Renders all rich content (math and code) within an element.
 * This is the main entry point called from Blazor via JS interop.
 */
function renderRichContent(element: HTMLElement): void {
    if (!element) {
        console.warn('Element not provided for rich content rendering');
        return;
    }

    // Render math first (KaTeX)
    renderMath(element);

    // Then highlight code (Prism.js)
    highlightCode(element);
}

/**
 * Renders rich content by element ID (convenience function for Blazor interop).
 */
function renderRichContentById(elementId: string): void {
    const element = document.getElementById(elementId);
    if (element) {
        renderRichContent(element);
    } else {
        console.warn(`Element with ID '${elementId}' not found`);
    }
}

/**
 * Escapes HTML special characters to prevent XSS when displaying error content.
 */
function escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Export to global window object for Blazor JS interop
window.renderingInterop = {
    renderMath,
    highlightCode,
    renderRichContent,
    renderRichContentById
};
