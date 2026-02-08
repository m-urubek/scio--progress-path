/**
 * Web Speech API interop for Progress Path
 * Provides voice dictation functionality for the student chat interface.
 * REQ-CHAT-012: Voice dictation via browser-native Web Speech API
 * REQ-CHAT-013: Microphone button with recording state indication
 * REQ-CHAT-014: Transcribed text appears in input field for review
 * REQ-CHAT-015: Unsupported browsers hide the dictation button
 */

// SpeechRecognition instance (null until initialized)
let recognition: SpeechRecognitionInstance | null = null;

// Current recording state
let isRecording = false;

// Reference to Blazor .NET object for callbacks
let dotNetReference: DotNetObjectReference | null = null;

/**
 * Checks if the Web Speech API is supported by the browser.
 * Detection per REQ-CHAT-015: 'webkitSpeechRecognition' in window || 'SpeechRecognition' in window
 */
function isSupported(): boolean {
    return 'webkitSpeechRecognition' in window || 'SpeechRecognition' in window;
}

/**
 * Initializes the SpeechRecognition instance with appropriate settings.
 * Should be called once when the component first renders.
 */
function initialize(): void {
    if (!isSupported()) {
        console.warn('[SpeechInterop] Web Speech API is not supported in this browser');
        return;
    }

    // Get the SpeechRecognition constructor (standard or webkit-prefixed)
    const SpeechRecognitionCtor = window.SpeechRecognition || window.webkitSpeechRecognition;

    if (!SpeechRecognitionCtor) {
        console.warn('[SpeechInterop] SpeechRecognition constructor not available');
        return;
    }

    recognition = new SpeechRecognitionCtor();

    // Configure recognition settings
    recognition.continuous = false;     // Stop after user stops speaking
    recognition.interimResults = true;  // Show real-time transcription as user speaks
    recognition.lang = 'en-US';         // Set language to English

    // Set up event handlers
    recognition.onresult = handleResult;
    recognition.onerror = handleError;
    recognition.onend = handleEnd;

    console.log('[SpeechInterop] Initialized Web Speech API');
}

/**
 * Starts voice recording.
 */
function startRecording(dotNetRef: DotNetObjectReference): void {
    if (!recognition) {
        console.error('[SpeechInterop] Recognition not initialized. Call initialize() first.');
        return;
    }

    if (isRecording) {
        console.warn('[SpeechInterop] Already recording');
        return;
    }

    // Store the Blazor reference for callbacks
    dotNetReference = dotNetRef;

    try {
        recognition.start();
        isRecording = true;
        console.log('[SpeechInterop] Recording started');
    } catch (error) {
        console.error('[SpeechInterop] Error starting recognition:', error);
        // Notify Blazor of the error
        if (dotNetReference) {
            const message = error instanceof Error ? error.message : 'Failed to start recording';
            dotNetReference.invokeMethodAsync('OnSpeechError', message);
        }
    }
}

/**
 * Stops voice recording.
 */
function stopRecording(): void {
    if (!recognition) {
        console.warn('[SpeechInterop] Recognition not initialized');
        return;
    }

    if (!isRecording) {
        console.warn('[SpeechInterop] Not currently recording');
        return;
    }

    try {
        recognition.stop();
        isRecording = false;
        console.log('[SpeechInterop] Recording stopped');
    } catch (error) {
        console.error('[SpeechInterop] Error stopping recognition:', error);
    }
}

/**
 * Handles speech recognition results.
 * Extracts transcript and sends it back to Blazor.
 */
function handleResult(event: SpeechRecognitionEvent): void {
    if (!event.results || event.results.length === 0) {
        return;
    }

    // Get the most recent result
    const result = event.results[event.results.length - 1];
    const transcript = result[0].transcript;

    // Only send final results (not interim) to Blazor
    // This provides a cleaner UX - text appears when user stops speaking
    if (result.isFinal) {
        console.log('[SpeechInterop] Final transcript:', transcript);
        if (dotNetReference) {
            dotNetReference.invokeMethodAsync('OnSpeechResult', transcript);
        }
    }
}

/**
 * Handles speech recognition errors.
 * Logs the error and notifies Blazor.
 */
function handleError(event: SpeechRecognitionErrorEvent): void {
    console.error('[SpeechInterop] Recognition error:', event.error);

    // Map error codes to user-friendly messages
    let errorMessage: string;
    switch (event.error) {
        case 'not-allowed':
            errorMessage = 'Microphone permission denied. Please allow microphone access.';
            break;
        case 'no-speech':
            errorMessage = 'No speech detected. Please try again.';
            break;
        case 'network':
            errorMessage = 'Network error occurred. Please check your connection.';
            break;
        case 'aborted':
            errorMessage = 'Recording was aborted.';
            break;
        case 'audio-capture':
            errorMessage = 'No microphone found. Please connect a microphone.';
            break;
        case 'service-not-allowed':
            errorMessage = 'Speech recognition service is not allowed.';
            break;
        default:
            errorMessage = `Speech recognition error: ${event.error}`;
    }

    isRecording = false;

    if (dotNetReference) {
        dotNetReference.invokeMethodAsync('OnSpeechError', errorMessage);
    }
}

/**
 * Handles when speech recognition ends.
 * Updates recording state and notifies Blazor.
 */
function handleEnd(): void {
    console.log('[SpeechInterop] Recognition ended');
    isRecording = false;

    if (dotNetReference) {
        dotNetReference.invokeMethodAsync('OnSpeechEnded');
    }
}

/**
 * Gets the current recording state.
 */
function getIsRecording(): boolean {
    return isRecording;
}

// Export to global window object for Blazor JS interop
window.speechInterop = {
    isSupported,
    initialize,
    startRecording,
    stopRecording,
    getIsRecording
};
