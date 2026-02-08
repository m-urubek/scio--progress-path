/**
 * BroadcastChannel-based tab synchronization for Progress Path.
 * Provides a fallback mechanism for synchronizing state between browser tabs
 * when SignalR connection issues occur.
 * REQ-GROUP-020: Multiple browser tabs accessing the same group shall receive synchronized updates.
 */

(function () {
    'use strict';

    /**
     * Active BroadcastChannel instance.
     * @type {BroadcastChannel|null}
     */
    let channel = null;

    /**
     * Session ID for the current tab.
     * @type {string|null}
     */
    let currentSessionId = null;

    /**
     * Registered message handler callback.
     * @type {Function|null}
     */
    let messageHandler = null;

    /**
     * Initializes tab synchronization for a specific session.
     * Creates a BroadcastChannel named 'progress-path-{sessionId}'.
     * @param {string} sessionId - The student session ID to sync
     * @returns {boolean} True if initialization succeeded, false otherwise
     */
    function initTabSync(sessionId) {
        // Check for BroadcastChannel support (not available in Safari < 15.4)
        if (typeof BroadcastChannel === 'undefined') {
            console.warn('[TabSync] BroadcastChannel not supported in this browser');
            return false;
        }

        // Close existing channel if different session
        if (channel && currentSessionId !== sessionId) {
            channel.close();
            channel = null;
        }

        // Don't re-initialize for same session
        if (channel && currentSessionId === sessionId) {
            return true;
        }

        try {
            currentSessionId = sessionId;
            channel = new BroadcastChannel(`progress-path-${sessionId}`);

            // Set up message listener
            channel.onmessage = function (event) {
                if (messageHandler && event.data) {
                    try {
                        messageHandler(event.data);
                    } catch (error) {
                        console.error('[TabSync] Error in message handler:', error);
                    }
                }
            };

            console.log('[TabSync] Initialized for session:', sessionId);
            return true;
        } catch (error) {
            console.error('[TabSync] Failed to initialize:', error);
            return false;
        }
    }

    /**
     * Posts a message to all other tabs for the same session.
     * @param {string} type - The message type (e.g., 'message', 'progress', 'state')
     * @param {object} data - The message data to broadcast
     * @returns {boolean} True if message was sent, false otherwise
     */
    function postTabMessage(type, data) {
        if (!channel) {
            console.warn('[TabSync] Cannot post message - not initialized');
            return false;
        }

        try {
            channel.postMessage({
                type: type,
                data: data,
                timestamp: Date.now()
            });
            return true;
        } catch (error) {
            console.error('[TabSync] Failed to post message:', error);
            return false;
        }
    }

    /**
     * Registers a callback to handle messages from other tabs.
     * @param {Function} callback - Handler function receiving { type, data, timestamp }
     */
    function onTabMessage(callback) {
        if (typeof callback === 'function') {
            messageHandler = callback;
        } else {
            console.warn('[TabSync] Invalid callback provided to onTabMessage');
        }
    }

    /**
     * Closes the BroadcastChannel and cleans up resources.
     */
    function dispose() {
        if (channel) {
            channel.close();
            channel = null;
        }
        currentSessionId = null;
        messageHandler = null;
        console.log('[TabSync] Disposed');
    }

    /**
     * Checks if BroadcastChannel is supported in the current browser.
     * @returns {boolean} True if BroadcastChannel is supported
     */
    function isSupported() {
        return typeof BroadcastChannel !== 'undefined';
    }

    // Export to global window object for Blazor JS interop
    window.tabSyncInterop = {
        initTabSync: initTabSync,
        postTabMessage: postTabMessage,
        onTabMessage: onTabMessage,
        dispose: dispose,
        isSupported: isSupported
    };
})();
