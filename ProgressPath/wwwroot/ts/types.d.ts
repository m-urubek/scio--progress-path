/**
 * Global type declarations for Progress Path
 * Extends the Window interface with Blazor JS interop namespaces
 * and declares external library globals (KaTeX, Prism, Blazor).
 */

// ─── Interop Namespace Interfaces ────────────────────────────────────────────

interface ChatInterop {
    scrollToBottom: (element: HTMLElement) => void;
    scrollToBottomInstant: (element: HTMLElement) => void;
    isNearBottom: (element: HTMLElement, threshold?: number) => boolean;
}

interface DeviceInterop {
    getDeviceId: () => string;
    setDeviceId: (deviceId: string) => void;
    clearDeviceId: () => void;
}

interface SpeechInterop {
    isSupported: () => boolean;
    initialize: () => void;
    startRecording: (dotNetRef: DotNetObjectReference) => void;
    stopRecording: () => void;
    getIsRecording: () => boolean;
}

interface RenderingInterop {
    renderMath: (element: HTMLElement) => void;
    highlightCode: (element: HTMLElement) => void;
    renderRichContent: (element: HTMLElement) => void;
    renderRichContentById: (elementId: string) => void;
}

interface ReconnectionInterop {
    currentState: string;
    dotNetReference: DotNetObjectReference | null;
    initialized: boolean;
    init: (dotNetRef: DotNetObjectReference) => void;
    waitForBlazorAndRegister: () => void;
    registerBlazorReconnectHandlers: () => void;
    monitorReconnectionUI: () => void;
    checkForReconnectionUI: (node: Element) => void;
    checkReconnectionState: () => void;
    getCurrentState: () => string;
    onReconnecting: () => void;
    onReconnected: () => void;
    onDisconnected: () => void;
    dispose: () => void;
}

interface TabSyncMessage {
    type: string;
    data: unknown;
    timestamp: number;
}

interface TabSyncInterop {
    initTabSync: (sessionId: string) => boolean;
    postTabMessage: (type: string, data: unknown) => boolean;
    onTabMessage: (callback: (message: TabSyncMessage) => void) => void;
    dispose: () => void;
    isSupported: () => boolean;
}

// ─── .NET Interop ────────────────────────────────────────────────────────────

/** Blazor DotNetObjectReference for invoking .NET methods from JavaScript */
interface DotNetObjectReference {
    invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

// ─── External Library Globals ────────────────────────────────────────────────

/** KaTeX math rendering library */
declare const katex: {
    render(
        expression: string,
        element: HTMLElement,
        options?: {
            throwOnError?: boolean;
            displayMode?: boolean;
            output?: string;
            strict?: boolean | string;
            trust?: boolean;
            macros?: Record<string, string>;
        }
    ): void;
};

/** Prism.js syntax highlighting library */
declare const Prism: {
    highlightElement(element: Element): void;
    highlightAll(): void;
};

/** Blazor framework global */
declare const Blazor: {
    reconnect?: unknown;
    _internal?: {
        forceCloseConnection?: unknown;
    };
};

// ─── Web Speech API (browser-native, partial typing) ─────────────────────────

interface SpeechRecognitionEvent extends Event {
    results: SpeechRecognitionResultList;
}

interface SpeechRecognitionResultList {
    readonly length: number;
    [index: number]: SpeechRecognitionResult;
}

interface SpeechRecognitionResult {
    readonly isFinal: boolean;
    readonly length: number;
    [index: number]: SpeechRecognitionAlternative;
}

interface SpeechRecognitionAlternative {
    readonly transcript: string;
    readonly confidence: number;
}

interface SpeechRecognitionErrorEvent extends Event {
    readonly error: string;
    readonly message: string;
}

interface SpeechRecognitionInstance extends EventTarget {
    continuous: boolean;
    interimResults: boolean;
    lang: string;
    onresult: ((event: SpeechRecognitionEvent) => void) | null;
    onerror: ((event: SpeechRecognitionErrorEvent) => void) | null;
    onend: (() => void) | null;
    start(): void;
    stop(): void;
    abort(): void;
}

declare const SpeechRecognition: {
    new (): SpeechRecognitionInstance;
} | undefined;

// ─── Window Extensions ───────────────────────────────────────────────────────

interface Window {
    chatInterop: ChatInterop;
    deviceInterop: DeviceInterop;
    speechInterop: SpeechInterop;
    renderingInterop: RenderingInterop;
    reconnectionInterop: ReconnectionInterop;
    tabSyncInterop: TabSyncInterop;
    SpeechRecognition?: { new (): SpeechRecognitionInstance };
    webkitSpeechRecognition?: { new (): SpeechRecognitionInstance };
}
