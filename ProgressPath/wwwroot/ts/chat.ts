/**
 * Chat interop functions for Progress Path
 * Provides JavaScript functionality for the student chat interface.
 */

/**
 * Scrolls an element to its bottom with smooth animation.
 * Used by ChatMessageList.razor to auto-scroll when new messages are added.
 */
function scrollToBottom(element: HTMLElement): void {
    if (element) {
        element.scrollTo({
            top: element.scrollHeight,
            behavior: 'smooth'
        });
    }
}

/**
 * Scrolls an element to its bottom instantly (no animation).
 * Used for initial page load where smooth scrolling would be disorienting.
 */
function scrollToBottomInstant(element: HTMLElement): void {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

/**
 * Checks if an element is scrolled near the bottom.
 * Used to determine if auto-scroll should be applied.
 * @param threshold - Pixels from bottom to consider "near bottom" (default: 100)
 * @returns True if element is near the bottom
 */
function isNearBottom(element: HTMLElement, threshold: number = 100): boolean {
    if (!element) return true;

    const scrollTop = element.scrollTop;
    const scrollHeight = element.scrollHeight;
    const clientHeight = element.clientHeight;

    return scrollTop + clientHeight >= scrollHeight - threshold;
}

// Export to global window object for Blazor JS interop
window.chatInterop = {
    scrollToBottom,
    scrollToBottomInstant,
    isNearBottom
};
