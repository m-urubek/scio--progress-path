/**
 * BroadcastChannel-based tab synchronization for Progress Path.
 * Provides a fallback mechanism for synchronizing state between browser tabs
 * when SignalR connection issues occur.
 * REQ-GROUP-020: Multiple browser tabs accessing the same group shall receive synchronized updates.
 */

(function (): void {
    'use strict';

    /** Active BroadcastChannel instance. */
    let channel: BroadcastChannel | null = null;

    /** Session ID for the current tab. */
    let currentSessionId: string | null = null;

    /** Registered message handler callback. */
    let messageHandler: ((message: TabSyncMessage) => void) | null = null;

    /**
     * Initializes tab synchronization for a specific session.
     * Creates a BroadcastChannel named 'progress-path-{sessionId}'.
     */
    function initTabSync(sessionId: string): boolean {
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
            channel.onmessage = function (event: MessageEvent): void {
                if (messageHandler && event.data) {
                    try {
                        messageHandler(event.data as TabSyncMessage);
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
     */
    function postTabMessage(type: string, data: unknown): boolean {
        if (!channel) {
            console.warn('[TabSync] Cannot post message - not initialized');
            return false;
        }

        try {
            channel.postMessage({
                type,
                data,
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
     */
    function onTabMessage(callback: (message: TabSyncMessage) => void): void {
        if (typeof callback === 'function') {
            messageHandler = callback;
        } else {
            console.warn('[TabSync] Invalid callback provided to onTabMessage');
        }
    }

    /**
     * Closes the BroadcastChannel and cleans up resources.
     */
    function dispose(): void {
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
     */
    function isSupported(): boolean {
        return typeof BroadcastChannel !== 'undefined';
    }

    // Export to global window object for Blazor JS interop
    window.tabSyncInterop = {
        initTabSync,
        postTabMessage,
        onTabMessage,
        dispose,
        isSupported
    };
})();
