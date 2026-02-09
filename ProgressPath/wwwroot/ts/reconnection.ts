/**
 * Blazor Server Reconnection Interop
 * Handles Blazor circuit reconnection events and notifies the Blazor component.
 * REQ-RT-002: Handle connection drops gracefully with visible reconnection overlay.
 */

/**
 * Connection state constants.
 */
const ConnectionState = {
    Connected: 'Connected',
    Reconnecting: 'Reconnecting',
    Reconnected: 'Reconnected',
    Disconnected: 'Disconnected'
} as const;

/**
 * Reconnection interop object for Blazor.
 */
const reconnectionInterop: ReconnectionInterop = {
    /** Current connection state */
    currentState: ConnectionState.Connected,

    /** DotNet reference for invoking Blazor methods */
    dotNetReference: null,

    /** Flag to track if initialized */
    initialized: false,

    /**
     * Initializes the reconnection interop by registering Blazor reconnection event handlers.
     */
    init(dotNetRef: DotNetObjectReference): void {
        if (this.initialized) {
            // Update dotnet reference if already initialized
            this.dotNetReference = dotNetRef;
            return;
        }

        this.dotNetReference = dotNetRef;
        this.initialized = true;

        // Wait for Blazor to be fully loaded
        this.waitForBlazorAndRegister();
    },

    /**
     * Waits for Blazor to be available and registers event handlers.
     */
    waitForBlazorAndRegister(): void {
        const self = this;
        const maxAttempts = 50;
        let attempts = 0;

        const tryRegister = (): void => {
            attempts++;

            // Check if Blazor and its reconnect object are available
            if (typeof Blazor !== 'undefined' && Blazor.reconnect) {
                self.registerBlazorReconnectHandlers();
                console.log('[Reconnection] Blazor reconnection handlers registered');
            } else if (attempts < maxAttempts) {
                // Retry after a short delay
                setTimeout(tryRegister, 100);
            } else {
                console.warn('[Reconnection] Could not register Blazor reconnection handlers - Blazor not available');
            }
        };

        // Start trying to register
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', tryRegister);
        } else {
            tryRegister();
        }
    },

    /**
     * Registers handlers for Blazor's built-in reconnection events.
     */
    registerBlazorReconnectHandlers(): void {
        const originalReconnect = Blazor.reconnect;

        if (originalReconnect) {
            // Listen for connection state via custom event or polling
            // Blazor Server uses a hidden reconnection UI we can monitor
            this.monitorReconnectionUI();
        }
    },

    /**
     * Monitors the Blazor reconnection UI element for state changes.
     * Blazor Server creates a hidden reconnection modal we can observe.
     */
    monitorReconnectionUI(): void {
        const self = this;

        // Create a MutationObserver to watch for Blazor's reconnection UI
        const observer = new MutationObserver((mutations: MutationRecord[]) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'childList') {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            self.checkForReconnectionUI(node as Element);
                        }
                    });
                }
                if (mutation.type === 'attributes') {
                    self.checkForReconnectionUI(mutation.target as Element);
                }
            });
        });

        // Observe the document body for changes
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'style']
        });

        // Also check periodically for reconnection state
        // This covers edge cases where the MutationObserver might miss something
        setInterval(() => {
            self.checkReconnectionState();
        }, 500);
    },

    /**
     * Checks a node for Blazor reconnection UI indicators.
     */
    checkForReconnectionUI(node: Element): void {
        if (!node || !node.id) return;

        // Blazor Server creates elements with these IDs during reconnection
        if (node.id === 'components-reconnect-modal') {
            const currentClass = node.className || '';
            if (currentClass.includes('components-reconnect-show')) {
                this.onReconnecting();
            } else if (currentClass.includes('components-reconnect-hide') ||
                       currentClass.includes('components-reconnect-failed')) {
                if (currentClass.includes('components-reconnect-failed') ||
                    currentClass.includes('components-reconnect-rejected')) {
                    this.onDisconnected();
                }
            }
        }
    },

    /**
     * Checks the current reconnection state by looking for Blazor's reconnection UI.
     */
    checkReconnectionState(): void {
        const reconnectModal = document.getElementById('components-reconnect-modal');

        if (reconnectModal) {
            const currentClass = reconnectModal.className || '';
            const style = reconnectModal.style;
            const isVisible = style.display !== 'none' && !currentClass.includes('components-reconnect-hide');

            if (isVisible && currentClass.includes('components-reconnect-show')) {
                if (this.currentState !== ConnectionState.Reconnecting) {
                    this.onReconnecting();
                }
            } else if (currentClass.includes('components-reconnect-failed') ||
                       currentClass.includes('components-reconnect-rejected')) {
                if (this.currentState !== ConnectionState.Disconnected) {
                    this.onDisconnected();
                }
            } else if (!isVisible && this.currentState === ConnectionState.Reconnecting) {
                // Modal hidden after reconnecting = successful reconnection
                this.onReconnected();
            }
        } else if (this.currentState === ConnectionState.Reconnecting) {
            // No modal and we were reconnecting = reconnected successfully
            this.onReconnected();
        }
    },

    /**
     * Gets the current connection state.
     */
    getCurrentState(): string {
        return this.currentState;
    },

    /**
     * Called when the connection is attempting to reconnect.
     */
    onReconnecting(): void {
        if (this.currentState === ConnectionState.Reconnecting) return;

        this.currentState = ConnectionState.Reconnecting;
        console.log('[Reconnection] Connection lost, reconnecting...');

        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnReconnecting').catch((err: unknown) => {
                console.error('[Reconnection] Error invoking OnReconnecting:', err);
            });
        }
    },

    /**
     * Called when the connection has been successfully restored.
     */
    onReconnected(): void {
        if (this.currentState === ConnectionState.Connected) return;

        const wasReconnecting = this.currentState === ConnectionState.Reconnecting;
        this.currentState = ConnectionState.Connected;

        if (wasReconnecting) {
            console.log('[Reconnection] Connection restored');

            if (this.dotNetReference) {
                this.dotNetReference.invokeMethodAsync('OnReconnected').catch((err: unknown) => {
                    console.error('[Reconnection] Error invoking OnReconnected:', err);
                });
            }
        }
    },

    /**
     * Called when the connection has been permanently lost.
     */
    onDisconnected(): void {
        if (this.currentState === ConnectionState.Disconnected) return;

        this.currentState = ConnectionState.Disconnected;
        console.log('[Reconnection] Connection permanently lost');

        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnDisconnected').catch((err: unknown) => {
                console.error('[Reconnection] Error invoking OnDisconnected:', err);
            });
        }
    },

    /**
     * Disposes the reconnection interop.
     */
    dispose(): void {
        this.dotNetReference = null;
    }
};

// Export to global window object for Blazor JS interop
window.reconnectionInterop = reconnectionInterop;
