/**
 * Particle background - auto-initializing via tsparticles
 * Renders into the #tsparticles div defined in App.razor.
 * Uses fullScreen: false so tsparticles doesn't mess with canvas positioning.
 */

(function () {
    "use strict";

    console.log("[particles] script loaded");

    var config = {
        fullScreen: { enable: false },
        background: { color: { value: "transparent" } },
        fpsLimit: 60,
        detectRetina: true,
        interactivity: {
            events: {
                onHover: { enable: true, mode: "grab" },
                onClick: { enable: true, mode: "push" }
            },
            modes: {
                grab: {
                    distance: 140,
                    links: { opacity: 0.5, color: "#22d3d8" }
                },
                push: { quantity: 2 }
            }
        },
        particles: {
            color: {
                value: ["#ff6b9d", "#c084fc", "#22d3d8", "#a78bfa", "#f0abfc"]
            },
            links: {
                color: "#c084fc",
                distance: 150,
                enable: true,
                opacity: 0.15,
                width: 1,
                triangles: { enable: true, opacity: 0.02 }
            },
            move: {
                enable: true,
                speed: 0.8,
                direction: "none",
                random: true,
                straight: false,
                outModes: { default: "bounce" },
                attract: {
                    enable: true,
                    rotate: { x: 600, y: 1200 }
                }
            },
            number: {
                density: { enable: true, width: 1920, height: 1080 },
                value: 80
            },
            opacity: {
                value: { min: 0.3, max: 0.8 },
                animation: { enable: true, speed: 0.1, sync: false, startValue: "random" }
            },
            shape: { type: "circle" },
            size: {
                value: { min: 2, max: 5 },
                animation: { enable: true, speed: 0.33, sync: false, startValue: "random" }
            },
            twinkle: {
                particles: {
                    enable: true,
                    frequency: 0.01,
                    opacity: 1,
                    color: { value: ["#ff6b9d", "#22d3d8", "#c084fc"] }
                }
            }
        }
    };

    function init() {
        if (typeof tsParticles === "undefined") {
            console.warn("[particles] tsParticles not loaded, retrying...");
            setTimeout(init, 500);
            return;
        }

        var container = document.getElementById("tsparticles");
        if (!container) {
            console.warn("[particles] #tsparticles div not found, retrying...");
            setTimeout(init, 500);
            return;
        }

        console.log("[particles] initializing into #tsparticles, tsParticles v" + (tsParticles.version || "?"));

        tsParticles.load({ id: "tsparticles", options: config }).then(function (c) {
            console.log("[particles] SUCCESS, particles:", c.particles.count);

            // Log canvas info
            var canvases = container.querySelectorAll("canvas");
            canvases.forEach(function (cv, i) {
                console.log("[particles] canvas[" + i + "]:", cv.width + "x" + cv.height, "styles:", cv.style.cssText);
            });
        }).catch(function (err) {
            console.error("[particles] FAILED:", err);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    window.particlesInterop = {
        init: function (id) { return tsParticles.load({ id: id, options: config }); },
        destroy: function (id) { tsParticles.destroy(id); }
    };
})();
