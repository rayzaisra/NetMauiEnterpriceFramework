(function () {
  "use strict";

  const listeners = new Map();
  const activityEvents = ["mousemove", "mousedown", "keydown", "touchstart", "scroll", "focus"];
  const activityThrottleMs = 15000;

  function ensureState(dotNetRef) {
    let state = listeners.get(dotNetRef);
    if (state) return state;

    state = {
      lastActivitySentAt: 0,
      onActivity: () => {
        const now = Date.now();
        if (now - state.lastActivitySentAt < activityThrottleMs) return;
        state.lastActivitySentAt = now;
        dotNetRef.invokeMethodAsync("NotifyBrowserActivity").catch(() => {});
      },
      onVisibility: () => {
        if (document.visibilityState === "visible") {
          dotNetRef.invokeMethodAsync("NotifyBrowserVisible").catch(() => {});
        }
      },
    };

    activityEvents.forEach((eventName) => {
      window.addEventListener(eventName, state.onActivity, { passive: true });
    });
    document.addEventListener("visibilitychange", state.onVisibility);
    listeners.set(dotNetRef, state);
    return state;
  }

  window.ryzcoreSessionIdle = {
    start(dotNetRef) {
      if (!dotNetRef) return;
      ensureState(dotNetRef);
    },
    stop(dotNetRef) {
      const state = listeners.get(dotNetRef);
      if (!state) return;
      activityEvents.forEach((eventName) => {
        window.removeEventListener(eventName, state.onActivity);
      });
      document.removeEventListener("visibilitychange", state.onVisibility);
      listeners.delete(dotNetRef);
    },
  };
})();
