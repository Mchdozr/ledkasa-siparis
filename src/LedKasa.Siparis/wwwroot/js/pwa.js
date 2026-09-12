window.ledkasaPwa = (function () {
    let deferredPrompt = null;

    const isDev = location.hostname === "localhost" || location.hostname === "127.0.0.1";

    window.addEventListener("pageshow", event => {
        if (event.persisted)
            location.reload();
    });

    window.addEventListener("beforeinstallprompt", event => {
        event.preventDefault();
        deferredPrompt = event;
    });

    function registerServiceWorker() {
        if (!("serviceWorker" in navigator))
            return;

        if (isDev) {
            navigator.serviceWorker.getRegistrations().then(regs => regs.forEach(r => r.unregister()));
            return;
        }

        navigator.serviceWorker.register("/service-worker.js");
    }

    return {
        afterCircuit: registerServiceWorker,
        isOnline: () => navigator.onLine,
        canInstall: () => !!deferredPrompt,
        install: async () => {
            if (!deferredPrompt) return false;
            deferredPrompt.prompt();
            const result = await deferredPrompt.userChoice;
            deferredPrompt = null;
            return result.outcome === "accepted";
        },
        onOnlineChange: dotnet => {
            const notify = () => dotnet.invokeMethodAsync("SetOnline", navigator.onLine);
            window.addEventListener("online", notify);
            window.addEventListener("offline", notify);
        }
    };
})();
