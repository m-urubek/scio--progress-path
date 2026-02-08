/**
 * Particle background configuration and initialization for Progress Path
 * Uses tsparticles to create interactive animated particles matching TherapistTemplate theme
 *
 * Colors: Pink (#ff6b9d), Purple (#c084fc), Teal (#22d3d8), Violet (#a78bfa), Light Pink (#f0abfc)
 *
 * Interactive modes:
 * - Hover: grab mode (140px distance) to connect nearby particles
 * - Click: push mode to add 2 new particles
 */

const particlesConfig = {
    fullScreen: {
        enable: false
    },
    background: {
        color: {
            value: 'transparent'
        }
    },
    fpsLimit: 60,
    detectRetina: true,
    interactivity: {
        events: {
            onHover: {
                enable: true,
                mode: 'grab'
            },
            onClick: {
                enable: true,
                mode: 'push'
            }
        },
        modes: {
            grab: {
                distance: 140,
                links: {
                    opacity: 0.5,
                    color: '#22d3d8'
                }
            },
            push: {
                quantity: 2
            }
        }
    },
    particles: {
        color: {
            value: ['#ff6b9d', '#c084fc', '#22d3d8', '#a78bfa', '#f0abfc']
        },
        links: {
            color: '#c084fc',
            distance: 150,
            enable: true,
            opacity: 0.15,
            width: 1,
            triangles: {
                enable: true,
                opacity: 0.02
            }
        },
        move: {
            enable: true,
            speed: 0.2,
            direction: 'none',
            random: true,
            straight: false,
            outModes: {
                default: 'bounce'
            },
            attract: {
                enable: true,
                rotate: {
                    x: 600,
                    y: 1200
                }
            }
        },
        number: {
            density: {
                enable: true,
                width: 1920,
                height: 1080
            },
            value: 80
        },
        opacity: {
            value: {
                min: 0.2,
                max: 0.6
            },
            animation: {
                enable: true,
                speed: 0.1,
                sync: false,
                startValue: 'random'
            }
        },
        shape: {
            type: 'circle'
        },
        size: {
            value: {
                min: 1,
                max: 4
            },
            animation: {
                enable: true,
                speed: 0.33,
                sync: false,
                startValue: 'random'
            }
        },
        twinkle: {
            particles: {
                enable: true,
                frequency: 0.01,
                opacity: 1,
                color: {
                    value: ['#ff6b9d', '#22d3d8', '#c084fc']
                }
            }
        }
    }
};

/**
 * Initialize tsparticles in the specified container
 * @param {string} containerId - The DOM element ID to render particles in
 * @returns {Promise<object>} The particles container instance for later disposal
 */
async function initParticles(containerId) {
    const container = await tsParticles.load({
        id: containerId,
        options: particlesConfig
    });
    return container;
}

/**
 * Destroy/cleanup particles instance
 * @param {string} containerId - The DOM element ID of the particles container to destroy
 */
function destroyParticles(containerId) {
    tsParticles.destroy(containerId);
}

// Export to global window object for Blazor JS interop
window.particlesInterop = {
    init: initParticles,
    destroy: destroyParticles
};
