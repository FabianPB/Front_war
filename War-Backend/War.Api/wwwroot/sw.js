// WAR Online — Service Worker for PWA caching
const CACHE_NAME = 'war-online-v1';
const STATIC_ASSETS = [
  '/',
  '/world.html',
  '/world.js',
  '/world.css',
  '/favicon.png',
  '/icon-192.png',
  '/icon-512.png',
  '/apple-touch-icon.png',
  '/manifest.json',
];

self.addEventListener('install', e => {
  e.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(STATIC_ASSETS))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys().then(keys =>
      Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', e => {
  const url = new URL(e.request.url);
  // Don't cache SignalR connections or API calls
  if (url.pathname.startsWith('/game') || url.pathname.startsWith('/chat') ||
      url.pathname.includes('negotiate') || e.request.url.includes('signalr')) {
    return;
  }
  // Network-first for HTML, cache-first for static assets
  if (e.request.mode === 'navigate') {
    e.respondWith(
      fetch(e.request).catch(() => caches.match(e.request))
    );
  } else {
    e.respondWith(
      caches.match(e.request).then(cached => cached || fetch(e.request))
    );
  }
});
