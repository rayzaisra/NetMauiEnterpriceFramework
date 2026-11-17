import {
    animate,
    cleanInlineStyles,
    createScope,
    createTimeline,
    stagger
} from "../lib/animejs/anime.ryz.min.js";

const reducedMotionQuery = "(prefers-reduced-motion: reduce)";
const finish = cleanInlineStyles;
const scopes = new Map();

function hasTargets(selector) {
    return document.querySelector(selector) !== null;
}

function motionAllowed() {
    return !window.matchMedia(reducedMotionQuery).matches;
}

function run(key, build) {
    const previous = scopes.get(key);
    if (previous) {
        previous.revert();
        scopes.delete(key);
    }

    const scope = createScope({
        mediaQueries: {
            reduceMotion: reducedMotionQuery
        }
    });

    scope.add((self) => {
        if (self.matches?.reduceMotion) {
            return;
        }

        build();
    });

    scopes.set(key, scope);
}

function visibleTabPanel() {
    return Array.from(document.querySelectorAll(".enterprise-tabs .mud-tab-panel"))
        .find((panel) => {
            const style = window.getComputedStyle(panel);
            return style.display !== "none"
                && style.visibility !== "hidden"
                && panel.getClientRects().length > 0;
        }) ?? null;
}

export function initLogin() {
    run("login", () => {
        if (hasTargets(".login-brand-panel")) {
            const timeline = createTimeline({
                defaults: {
                    ease: "out(3)"
                },
                onComplete: finish
            });

            timeline
                .add(".login-brand-top", {
                    opacity: [0, 1],
                    y: [-12, 0],
                    duration: 360
                }, 0)
                .add(".login-brand-visual img", {
                    opacity: [0, 1],
                    scale: [1.045, 1],
                    duration: 620
                }, 60)
                .add(".login-form-panel", {
                    opacity: [0, 1],
                    y: [24, 0],
                    duration: 440
                }, 80)
                .add(".login-form-header", {
                    opacity: [0, 1],
                    y: [12, 0],
                    duration: 280
                }, 220)
                .add(".login-form-fields > *", {
                    opacity: [0, 1],
                    y: [12, 0],
                    duration: 300,
                    delay: stagger(48)
                }, 280)
                .add(".login-form-panel .mud-button-root", {
                    opacity: [0, 1],
                    y: [8, 0],
                    duration: 260
                }, 420);
        }

        if (hasTargets(".maui-auth-card")) {
            const timeline = createTimeline({
                defaults: {
                    ease: "out(3)"
                },
                onComplete: finish
            });

            timeline
                .add(".maui-auth-logo", {
                    opacity: [0, 1],
                    scale: [0.84, 1],
                    duration: 360
                }, 0)
                .add(".maui-auth-heading", {
                    opacity: [0, 1],
                    y: [12, 0],
                    duration: 280
                }, 100)
                .add(".maui-auth-form-area", {
                    opacity: [0, 1],
                    y: [20, 0],
                    duration: 420
                }, 160)
                .add(".maui-auth-form-area .mud-input-control", {
                    opacity: [0, 1],
                    y: [12, 0],
                    duration: 300,
                    delay: stagger(48)
                }, 250)
                .add(".maui-auth-submit", {
                    opacity: [0, 1],
                    y: [8, 0],
                    duration: 260
                }, 400);
        }
    });
}

export function initDashboard() {
    run("dashboard", () => {
        if (hasTargets(".dashboard-hero")) {
            animate(".dashboard-hero", {
                opacity: [0, 1],
                y: [18, 0],
                duration: 360,
                ease: "out(3)",
                onComplete: finish
            });
        }

        const cards = Array.from(document.querySelectorAll(".dashboard-card")).slice(0, 8);
        if (cards.length > 0) {
            animate(cards, {
                opacity: [0, 1],
                y: [20, 0],
                duration: 400,
                delay: stagger(60),
                ease: "out(3)",
                onComplete: finish
            });
        }

        if (hasTargets(".maui-widget-card, .maui-menu-tile")) {
            const widgets = Array.from(document.querySelectorAll(".maui-widget-card")).slice(0, 8);
            if (widgets.length > 0) {
                animate(widgets, {
                    opacity: [0, 1],
                    y: [18, 0],
                    duration: 360,
                    delay: stagger(65),
                    ease: "out(3)",
                    onComplete: finish
                });
            }

            const tiles = Array.from(document.querySelectorAll(".maui-menu-tile")).slice(0, 12);
            if (tiles.length > 0) {
                animate(tiles, {
                    opacity: [0, 1],
                    y: [14, 0],
                    duration: 320,
                    delay: stagger(30),
                    ease: "out(3)",
                    onComplete: finish
                });
            }
        }
    });
}

export function initNotifications() {
    run("notifications", () => {
        if (hasTargets(".notification-page-panel")) {
            animate(".notification-page-panel", {
                opacity: [0, 1],
                y: [14, 0],
                duration: 300,
                ease: "out(3)",
                onComplete: finish
            });
        }

        const cards = Array.from(document.querySelectorAll(".maui-notification-card")).slice(0, 8);
        if (cards.length > 0) {
            animate(cards, {
                opacity: [0, 1],
                y: [16, 0],
                duration: 340,
                delay: stagger(45),
                ease: "out(3)",
                onComplete: finish
            });
        }
    });
}

export function initMobilePage() {
    run("mobile-page", () => {
        const selector = ".maui-menu-section, .maui-profile-identity-card, .maui-profile-card";
        const sections = Array.from(document.querySelectorAll(selector)).slice(0, 8);
        if (sections.length > 0) {
            animate(sections, {
                opacity: [0, 1],
                y: [14, 0],
                duration: 320,
                delay: stagger(50),
                ease: "out(3)",
                onComplete: finish
            });
        }
    });
}

export function playTabPanel() {
    if (!motionAllowed()) {
        return;
    }

    const panel = visibleTabPanel();
    if (!panel) {
        return;
    }

    animate(panel, {
        opacity: [0, 1],
        y: [10, 0],
        duration: 280,
        ease: "out(3)",
        onComplete: finish
    });

    const header = panel.querySelector(".tab-header");
    if (header) {
        animate(header, {
            opacity: [0, 1],
            x: [-8, 0],
            duration: 240,
            ease: "out(3)",
            onComplete: finish
        });
    }
}

export function playNotificationArrival() {
    if (!motionAllowed()) {
        return;
    }

    const firstItem = document.querySelector(".notification-list .notification-item");
    if (firstItem) {
        animate(firstItem, {
            opacity: [0, 1],
            y: [-16, 0],
            scale: [0.97, 1],
            duration: 360,
            ease: "out(3)",
            onComplete: finish
        });
    }

    const badge = document.querySelector(".notification-wrapper .mud-badge-badge");
    if (badge) {
        animate(badge, {
            scale: [0.65, 1.12, 1],
            duration: 420,
            ease: "out(3)",
            onComplete: finish
        });
    }
}

export function pulseMenu(element) {
    if (!element || !motionAllowed()) {
        return;
    }

    animate(element, {
        scale: [0.96, 1],
        duration: 180,
        ease: "out(3)",
        onComplete: finish
    });
}

export function cleanup() {
    for (const scope of scopes.values()) {
        scope.revert();
    }

    scopes.clear();
}
