const CACHE = "ledkasa-siparis-v1";
const PRECACHE = ["/offline.html", "/manifest.webmanifest", "/icons/icon-192.png", "/icons/icon-512.png"];

self.addEventListener("install", event => {
    event.waitUntil(caches.open(CACHE).then(cache => cache.addAll(PRECACHE)));
    self.skipWaiting();
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
    );
    self.clients.claim();
});

self.addEventListener("fetch", event => {
    const request = event.request;
    if (request.method !== "GET") return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    const isStatic = url.pathname.startsWith("/css/")
        || url.pathname.startsWith("/js/")
        || url.pathname.startsWith("/icons/")
        || url.pathname.startsWith("/_content/")
        || url.pathname.endsWith(".css")
        || url.pathname.endsWith(".js")
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
