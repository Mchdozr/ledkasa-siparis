const CACHE = "ledkasa-siparis-v2";
const PRECACHE = ["/offline.html", "/manifest.webmanifest", "/icons/icon-192.png", "/icons/icon-512.png"];

self.addEventListener("install", event => {
    event.waitUntil(caches.open(CACHE).then(cache => cache.addAll(PRECACHE)));
    self.skipWaiting();
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
    );
});

self.addEventListener("fetch", event => {
    const request = event.request;
    if (request.method !== "GET") return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;
    if (url.pathname.startsWith("/_blazor") || url.pathname.startsWith("/_framework"))
        return;

    const isStatic = url.pathname.startsWith("/css/")
        || url.pathname.startsWith("/js/")
        || url.pathname.startsWith("/icons/")
        || url.pathname.endsWith(".css")
        || url.pathname.endsWith(".png")
        || url.pathname.endsWith(".webmanifest");

    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request).catch(() => caches.match("/offline.html"))
        );
        return;
    }

    if (!isStatic) return;

    event.respondWith(
        caches.open(CACHE).then(async cache => {
            try {
                const response = await fetch(request);
                if (response.ok) cache.put(request, response.clone());
                return response;
            } catch {
                return (await cache.match(request)) || Response.error();
            }
        })
    );
});
