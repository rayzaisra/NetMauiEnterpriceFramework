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

let processMode = null;
let processProgressState = null;
let processProgressAnimation = null;
let processLoopAnimations = [];
let processHideTimer = null;
let processShowTimer = null;
let busyObserver = null;
let busyCheckTimer = null;

const busyActionPattern = /(export|import|save|submit|approve|reject|update|chang|menyimpan|memproses|mengirim)/i;

function processElements() {
    return {
        overlay: document.querySelector(".ryz-process-overlay"),
        title: document.querySelector(".ryz-process-title"),
        stage: document.querySelector(".ryz-process-stage"),
        bar: document.querySelector(".ryz-process-bar"),
        percent: document.querySelector(".ryz-process-percent")
    };
}

function stopProcessLoops() {
    for (const animation of processLoopAnimations) {
        animation.pause();
    }

    processLoopAnimations = [];
}

function showProcess(mode, title, stage, duration, steps) {
    const elements = processElements();
    if (!elements.overlay || !elements.title || !elements.stage || !elements.bar || !elements.percent) {
        return false;
    }

    clearTimeout(processHideTimer);
    clearTimeout(processShowTimer);

    processMode = mode;
    elements.overlay.classList.add("is-active");
    elements.overlay.setAttribute("aria-hidden", "false");
    elements.title.textContent = title;
    elements.stage.textContent = stage;
    elements.bar.style.width = "0%";
    elements.percent.textContent = "0%";

    stopProcessLoops();

    if (motionAllowed()) {
        const rings = Array.from(document.querySelectorAll(".ryz-process-ring"));
        rings.forEach((ring, index) => {
            processLoopAnimations.push(animate(ring, {
                rotate: [0, 360],
                duration: 1400 + (index * 520),
                loop: true,
                ease: "linear"
            }));
        });

        const dots = document.querySelectorAll(".ryz-process-dot");
        if (dots.length > 0) {
            processLoopAnimations.push(animate(dots, {
                scale: [0.55, 1.2, 0.55],
                opacity: [0.4, 1, 0.4],
                duration: 900,
                delay: stagger(150),
                loop: true,
                ease: "inOut(2)"
            }));
        }
    }

    processProgressState = { value: 0 };
    processProgressAnimation = animate(processProgressState, {
        value: 92,
        duration,
        ease: "out(4)",
        onUpdate: () => {
            const value = Math.min(92, Math.round(processProgressState.value));
            elements.bar.style.width = `${value}%`;
            elements.percent.textContent = `${value}%`;

            if (!steps || steps.length === 0) {
                return;
            }

            let currentStage = steps[0].text;
            for (const step of steps) {
                if (value >= step.at) {
                    currentStage = step.text;
                }
            }

            elements.stage.textContent = currentStage;
        }
    });

    return true;
}

function completeProcess(mode) {
    if (processMode !== mode) {
        return;
    }

    clearTimeout(processShowTimer);

    if (processProgressAnimation) {
        processProgressAnimation.pause();
        processProgressAnimation = null;
    }

    const elements = processElements();
    if (!elements.bar || !elements.percent) {
        hideProcess();
        return;
    }

    const current = processProgressState ? processProgressState.value : 0;
    const state = { value: current };

    animate(state, {
        value: 100,
        duration: 420,
        ease: "out(3)",
        onUpdate: () => {
            const value = Math.min(100, Math.round(state.value));
            elements.bar.style.width = `${value}%`;
            elements.percent.textContent = `${value}%`;
        },
        onComplete: () => {
            elements.bar.style.width = "100%";
            elements.percent.textContent = "100%";
            if (elements.stage) {
                elements.stage.textContent = "Completed";
            }

            clearTimeout(processHideTimer);
            processHideTimer = setTimeout(hideProcess, 320);
        }
    });
}

function hideProcess() {
    const elements = processElements();
    if (elements.overlay) {
        elements.overlay.classList.remove("is-active");
        elements.overlay.setAttribute("aria-hidden", "true");
    }

    stopProcessLoops();
    processProgressState = null;
    processMode = null;
}

function stopProcess(mode) {
    if (mode && processMode !== mode) {
        return;
    }

    clearTimeout(processHideTimer);
    clearTimeout(processShowTimer);

    if (processProgressAnimation) {
        processProgressAnimation.pause();
        processProgressAnimation = null;
    }

    hideProcess();
}

function describeBusyAction(label) {
    if (label.includes("import")) {
        return {
            title: "Importing Excel",
            stage: "Uploading file...",
            duration: 22000,
            steps: [
                { at: 0, text: "Uploading file..." },
                { at: 40, text: "Validating rows..." },
                { at: 78, text: "Saving data..." }
            ]
        };
    }

    if (label.includes("export")) {
        return {
            title: "Exporting Excel",
            stage: "Preparing data...",
            duration: 16000,
            steps: [
                { at: 0, text: "Preparing data..." },
                { at: 60, text: "Generating workbook..." },
                { at: 88, text: "Finalizing file..." }
            ]
        };
    }

    if (label.includes("save") || label.includes("menyimpan") || label.includes("update")) {
        return {
            title: "Saving changes",
            stage: "Writing to database...",
            duration: 7000,
            steps: [
                { at: 0, text: "Writing to database..." },
                { at: 70, text: "Refreshing data..." }
            ]
        };
    }

    if (label.includes("submit") || label.includes("approve") || label.includes("reject")) {
        return {
            title: "Submitting",
            stage: "Processing request...",
            duration: 9000,
            steps: [
                { at: 0, text: "Processing request..." },
                { at: 70, text: "Applying changes..." }
            ]
        };
    }

    return {
        title: "Updating",
        stage: "Applying changes...",
        duration: 6000,
        steps: [
            { at: 0, text: "Applying changes..." },
            { at: 70, text: "Finishing up..." }
        ]
    };
}

function activeBusyButton() {
    return Array.from(document.querySelectorAll(".ryz-busy-button.is-busy"))
        .find((button) => busyActionPattern.test((button.textContent || "").trim())) ?? null;
}

function checkBusyButtons() {
    const button = activeBusyButton();

    if (button) {
        clearTimeout(processHideTimer);
        clearTimeout(processShowTimer);

        if (processMode === "busy" || processMode === "upload") {
            return;
        }

        processShowTimer = setTimeout(() => {
            const stillBusy = activeBusyButton();
            if (!stillBusy || processMode) {
                return;
            }

            const action = describeBusyAction((stillBusy.textContent || "").trim().toLowerCase());
            showProcess("busy", action.title, action.stage, action.duration, action.steps);
        }, 120);

        return;
    }

    clearTimeout(processShowTimer);
    processShowTimer = null;

    if (processMode === "busy") {
        completeProcess("busy");
    }
}

function scheduleBusyCheck() {
    if (busyCheckTimer) {
        return;
    }

    busyCheckTimer = setTimeout(() => {
        busyCheckTimer = null;
        checkBusyButtons();
    }, 80);
}

export function initBusyIndicators() {
    if (busyObserver) {
        return;
    }

    busyObserver = new MutationObserver(scheduleBusyCheck);
    busyObserver.observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ["class"]
    });

    scheduleBusyCheck();
}

export function startUploadIndicator() {
    showProcess(
        "upload",
        "Importing Excel",
        "Uploading file...",
        22000,
        [
            { at: 0, text: "Uploading file..." },
            { at: 40, text: "Validating rows..." },
            { at: 78, text: "Saving tasks..." }
        ]);
}

export function completeUploadIndicator() {
    completeProcess("upload");
}

export function stopUploadIndicator() {
    stopProcess("upload");
}

export function cleanup() {
    for (const scope of scopes.values()) {
        scope.revert();
    }

    scopes.clear();

    if (busyObserver) {
        busyObserver.disconnect();
        busyObserver = null;
    }

    clearTimeout(busyCheckTimer);
    busyCheckTimer = null;
    stopProcess();
}
