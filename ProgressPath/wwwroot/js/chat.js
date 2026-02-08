/**
 * Chat interop functions for Progress Path
 * Provides JavaScript functionality for the student chat interface.
 */

/**
 * Scrolls an element to its bottom with smooth animation.
 * Used by ChatMessageList.razor to auto-scroll when new messages are added.
 * @param {HTMLElement} element - The element to scroll
 */
function scrollToBottom(element) {
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
 * @param {HTMLElement} element - The element to scroll
 */
function scrollToBottomInstant(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

/**
 * Checks if an element is scrolled near the bottom.
 * Used to determine if auto-scroll should be applied.
 * @param {HTMLElement} element - The element to check
 * @param {number} threshold - Pixels from bottom to consider "near bottom" (default: 100)
 * @returns {boolean} True if element is near the bottom
 */
function isNearBottom(element, threshold = 100) {
    if (!element) return true;

    const scrollTop = element.scrollTop;
    const scrollHeight = element.scrollHeight;
    const clientHeight = element.clientHeight;

    return scrollTop + clientHeight >= scrollHeight - threshold;
}

// Export to global window object for Blazor JS interop
window.chatInterop = {
    scrollToBottom: scrollToBottom,
    scrollToBottomInstant: scrollToBottomInstant,
    isNearBottom: isNearBottom
};
