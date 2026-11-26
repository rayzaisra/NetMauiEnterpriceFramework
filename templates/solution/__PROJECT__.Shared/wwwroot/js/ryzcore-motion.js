import {
    animate,
    cleanInlineStyles,
    createScope,
    stagger
} from "../lib/animejs/anime.ryz.min.js";

const reducedMotionQuery = "(prefers-reduced-motion: reduce)";
const finish = cleanInlineStyles;
const scopes = new Map();
let lastTabPanel = null;

const i18n = {
    en: {
        ImportingExcel: "Importing Excel",
        UploadingFile: "Uploading file...",
        ValidatingRows: "Validating rows...",
        SavingData: "Saving data...",
        SavingTasks: "Saving tasks...",
        ExportingExcel: "Exporting Excel",
        PreparingData: "Preparing data...",
        GeneratingWorkbook: "Generating workbook...",
        FinalizingFile: "Finalizing file...",
        SavingChanges: "Saving changes",
        WritingToDatabase: "Writing to database...",
        RefreshingData: "Refreshing data...",
        Submitting: "Submitting",
        ProcessingRequest: "Processing request...",
        ApplyingChanges: "Applying changes...",
        Updating: "Updating",
        FinishingUp: "Finishing up...",
        Processing: "Processing",
        PleaseWait: "Please wait..."
    },
    id: {
        ImportingExcel: "Mengimpor Excel",
        UploadingFile: "Mengunggah berkas...",
        ValidatingRows: "Memvalidasi baris...",
        SavingData: "Menyimpan data...",
        SavingTasks: "Menyimpan tugas...",
        ExportingExcel: "Mengekspor Excel",
        PreparingData: "Menyiapkan data...",
        GeneratingWorkbook: "Membuat buku kerja...",
        FinalizingFile: "Menyelesaikan berkas...",
        SavingChanges: "Menyimpan perubahan",
        WritingToDatabase: "Menulis ke basis data...",
        RefreshingData: "Menyegarkan data...",
        Submitting: "Mengirim",
        ProcessingRequest: "Memproses permintaan...",
        ApplyingChanges: "Menerapkan perubahan...",
        Updating: "Memperbarui",
        FinishingUp: "Menyelesaikan...",
        Processing: "Memproses",
        PleaseWait: "Mohon tunggu..."
    }
};

function currentLang() {
    const lang = document.documentElement.getAttribute("data-language")
        || document.documentElement.getAttribute("lang")
        || "en";
    return lang.toLowerCase().startsWith("id") ? "id" : "en";
}

function t(key) {
    const lang = currentLang();
    return i18n[lang]?.[key] ?? i18n.en[key] ?? key;
}

export function setLanguage(lang) {
    const normalized = (lang || "en").toLowerCase().startsWith("id") ? "id" : "en";
    document.documentElement.setAttribute("data-language", normalized);
    document.documentElement.setAttribute("lang", normalized);
}

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
    return Array.from(document.querySelectorAll(".enterprise-tabs .ryz-tab-panel"))
        .find((panel) => {
            const style = window.getComputedStyle(panel);
            return style.display !== "none"
                && style.visibility !== "hidden"
                && panel.getClientRects().length > 0;
        }) ?? null;
}

export function initDashboard() {
    run("dashboard", () => {
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
    const panel = visibleTabPanel();
    if (!panel) {
        return;
    }

    if (lastTabPanel && lastTabPanel !== panel) {
        lastTabPanel.classList.remove("is-active");
    }

    lastTabPanel = panel;

    if (!motionAllowed()) {
        return;
    }

    panel.classList.remove("is-active");
    void panel.offsetWidth;
    panel.classList.add("is-active");
}

export function beginRouteTransition() {
    document.documentElement.classList.add("ryz-route-transition");
}

export function endRouteTransition() {
    document.documentElement.classList.remove("ryz-route-transition");
}

export function dismissBootLoader() {
    const root = document.documentElement;
    if (!root.classList.contains("ryz-boot-ready")) {
        root.classList.add("ryz-boot-ready");
    }
}

function watchBootLoader() {
    const boot = document.querySelector("[data-ryz-boot]");
    if (!boot) {
        return;
    }

    const dismiss = () => {
        requestAnimationFrame(() => {
            requestAnimationFrame(() => dismissBootLoader());
        });
    };

    const target = document.getElementById("app");
    if (!target) {
        dismiss();
        return;
    }

    if (target.childElementCount > 0) {
        dismiss();
        return;
    }

    const observer = new MutationObserver(() => {
        if (target.childElementCount === 0) {
            return;
        }

        observer.disconnect();
        dismiss();
    });

    observer.observe(target, { childList: true });

    window.setTimeout(() => {
        observer.disconnect();
        dismissBootLoader();
    }, 12000);
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", watchBootLoader, { once: true });
} else {
    watchBootLoader();
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
let processCompleteTimer = null;
let processStageTimer = null;
let processCompleting = false;
let busyObserver = null;
let busyCheckTimer = null;

const busyActionPattern = /(export|import|save|submit|approve|reject|update|chang|menyimpan|memproses|mengirim)/i;

function processElements() {
    return {
        overlay: document.querySelector(".ryz-process-overlay"),
        title: document.querySelector(".ryz-process-title"),
        stage: document.querySelector(".ryz-process-stage"),
        bar: document.querySelector(".ryz-process-bar"),
        track: document.querySelector(".ryz-process-track"),
        percent: document.querySelector(".ryz-process-percent")
    };
}

function clearProcessStageTimer() {
    if (processStageTimer) {
        clearInterval(processStageTimer);
        processStageTimer = null;
    }
}

function cycleStages(stage, steps) {
    clearProcessStageTimer();
    if (!steps || steps.length === 0) {
        return;
    }

    let index = 0;
    processStageTimer = setInterval(() => {
        index = (index + 1) % steps.length;
        stage.textContent = steps[index].text;
    }, 1700);
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
    clearTimeout(processCompleteTimer);
    clearProcessStageTimer();
    processCompleteTimer = null;

    if (processProgressAnimation) {
        processProgressAnimation.pause();
        processProgressAnimation = null;
    }

    const indeterminate = mode === "busy" || mode === "upload";
    processMode = mode;
    processCompleting = false;
    elements.overlay.classList.add("is-active");
    elements.overlay.setAttribute("aria-hidden", "false");
    elements.title.textContent = title;
    elements.stage.textContent = stage;
    elements.bar.style.width = indeterminate ? "0%" : "0%";
    elements.percent.textContent = indeterminate ? "" : "0%";
    elements.percent.style.visibility = indeterminate ? "hidden" : "visible";

    if (elements.track) {
        elements.track.classList.toggle("is-indeterminate", indeterminate);
    }

    cycleStages(elements.stage, steps);

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

    if (indeterminate) {
        return true;
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
        }
    });

    return true;
}

function completeProcess(mode) {
    if (processMode !== mode || processCompleting) {
        return;
    }

    processCompleting = true;
    clearTimeout(processShowTimer);
    clearTimeout(processCompleteTimer);
    clearProcessStageTimer();

    if (processProgressAnimation) {
        processProgressAnimation.pause();
        processProgressAnimation = null;
    }

    const elements = processElements();
    if (!elements.bar || !elements.percent) {
        hideProcess();
        return;
    }

    if (elements.track?.classList.contains("is-indeterminate")) {
        if (elements.stage) {
            elements.stage.textContent = "Completed";
        }

        processHideTimer = setTimeout(hideProcess, 520);
        return;
    }

    processCompleteTimer = setTimeout(hideProcess, 1600);

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
    clearTimeout(processCompleteTimer);
    clearTimeout(processHideTimer);
    clearProcessStageTimer();
    processCompleteTimer = null;
    processHideTimer = null;

    const elements = processElements();
    if (elements.overlay) {
        elements.overlay.classList.remove("is-active");
        elements.overlay.setAttribute("aria-hidden", "true");
    }

    if (elements.track) {
        elements.track.classList.remove("is-indeterminate");
    }

    if (elements.percent) {
        elements.percent.style.visibility = "visible";
    }

    stopProcessLoops();
    processProgressState = null;
    processCompleting = false;
    processMode = null;
}

function stopProcess(mode) {
    if (mode && processMode !== mode) {
        return;
    }

    clearTimeout(processHideTimer);
    clearTimeout(processShowTimer);
    clearTimeout(processCompleteTimer);

    if (processProgressAnimation) {
        processProgressAnimation.pause();
        processProgressAnimation = null;
    }

    hideProcess();
}

function describeBusyAction(label) {
    if (label.includes("import")) {
        return {
            title: t("ImportingExcel"),
            stage: t("UploadingFile"),
            duration: 12000,
            steps: [
                { at: 0, text: t("UploadingFile") },
                { at: 40, text: t("ValidatingRows") },
                { at: 78, text: t("SavingData") }
            ]
        };
    }

    if (label.includes("export")) {
        return {
            title: t("ExportingExcel"),
            stage: t("PreparingData"),
            duration: 16000,
            steps: [
                { at: 0, text: t("PreparingData") },
                { at: 60, text: t("GeneratingWorkbook") },
                { at: 88, text: t("FinalizingFile") }
            ]
        };
    }

    if (label.includes("save") || label.includes("menyimpan") || label.includes("update")) {
        return {
            title: t("SavingChanges"),
            stage: t("WritingToDatabase"),
            duration: 7000,
            steps: [
                { at: 0, text: t("WritingToDatabase") },
                { at: 70, text: t("RefreshingData") }
            ]
        };
    }

    if (label.includes("submit") || label.includes("approve") || label.includes("reject")) {
        return {
            title: t("Submitting"),
            stage: t("ProcessingRequest"),
            duration: 9000,
            steps: [
                { at: 0, text: t("ProcessingRequest") },
                { at: 70, text: t("ApplyingChanges") }
            ]
        };
    }

    return {
        title: t("Updating"),
        stage: t("ApplyingChanges"),
        duration: 6000,
        steps: [
            { at: 0, text: t("ApplyingChanges") },
            { at: 70, text: t("FinishingUp") }
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
        const overlay = document.querySelector(".ryz-process-overlay");
        const overlayActive = overlay?.classList.contains("is-active") ?? false;

        if (processCompleting && !overlayActive) {
            processCompleting = false;
            processMode = null;
        }

        if (processCompleting) {
            return;
        }

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
        }, 40);

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
        t("ImportingExcel"),
        t("UploadingFile"),
        22000,
        [
            { at: 0, text: t("UploadingFile") },
            { at: 40, text: t("ValidatingRows") },
            { at: 78, text: t("SavingTasks") }
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
