/**
 * Device ID management for Progress Path
 * Handles localStorage-based device identification for session binding.
 * REQ-GROUP-017: Device-binding via localStorage
 * REQ-GROUP-018: Session restoration using device ID
 * REQ-GROUP-019: Single device can join multiple groups
 */

const DEVICE_ID_KEY = 'progresspath_device_id';

/**
 * Generates a UUID v4.
 * Uses crypto.randomUUID() if available, falls back to manual generation for older browsers.
 * @returns {string} A new UUID v4 string
 */
function generateUUID() {
    // Use native crypto.randomUUID() if available (modern browsers)
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
        return crypto.randomUUID();
    }

    // Fallback: Generate UUID v4 manually for older browsers
    // Format: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

/**
 * Gets the device ID from localStorage.
 * If no device ID exists, generates a new one and stores it.
 * @returns {string} The device ID (UUID v4)
 */
function getDeviceId() {
    try {
        let deviceId = localStorage.getItem(DEVICE_ID_KEY);

        if (!deviceId) {
            deviceId = generateUUID();
            localStorage.setItem(DEVICE_ID_KEY, deviceId);
            console.log('[DeviceInterop] Generated new device ID:', deviceId);
        }

        return deviceId;
    } catch (error) {
        // localStorage may be unavailable (private browsing, etc.)
        console.error('[DeviceInterop] Error accessing localStorage:', error);
        // Return a temporary device ID for this session only
        return generateUUID();
    }
}

/**
 * Sets a specific device ID in localStorage.
 * Primarily used for testing and debugging purposes.
 * @param {string} deviceId - The device ID to set
 */
function setDeviceId(deviceId) {
    try {
        if (deviceId) {
            localStorage.setItem(DEVICE_ID_KEY, deviceId);
            console.log('[DeviceInterop] Set device ID:', deviceId);
        }
    } catch (error) {
        console.error('[DeviceInterop] Error setting device ID:', error);
    }
}

/**
 * Clears the device ID from localStorage.
 * Primarily used for testing and debugging purposes.
 * After clearing, the next call to getDeviceId() will generate a new ID.
 */
function clearDeviceId() {
    try {
        localStorage.removeItem(DEVICE_ID_KEY);
        console.log('[DeviceInterop] Cleared device ID');
    } catch (error) {
        console.error('[DeviceInterop] Error clearing device ID:', error);
    }
}

// Export to global window object for Blazor JS interop
window.deviceInterop = {
    getDeviceId: getDeviceId,
    setDeviceId: setDeviceId,
    clearDeviceId: clearDeviceId
};
