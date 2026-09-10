window.ledkasaPwa = (function () {
    let deferredPrompt = null;
    let onlineHandler = null;

    const isDev = location.hostname === "localhost" || location.hostname === "127.0.0.1";

    if ("serviceWorker" in navigator) {
        window.addEventListener("load", () => {
            if (isDev) {
                navigator.serviceWorker.getRegistrations().then(regs => regs.forEach(r => r.unregister()));
                return;
            }
            navigator.serviceWorker.register("/service-worker.js").then(reg => {
                reg.addEventListener("updatefound", () => {
                    const worker = reg.installing;
                    worker?.addEventListener("statechange", () => {
                        if (worker.state === "installed" && navigator.serviceWorker.controller) {
                            if (confirm("Yeni bir güncelleme var. Sayfa yenilensin mi?")) {
                                location.reload();
                            }
                        }
                    });
                });
            });
        });
    }

    window.addEventListener("beforeinstallprompt", event => {
        event.preventDefault();
        deferredPrompt = event;
    });

    return {
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
            onlineHandler = notify;
        }
    };
})();
