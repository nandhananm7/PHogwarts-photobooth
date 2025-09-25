# Photobooth Desktop — Final (WPF + WebView2, local save)

- Encodes GIF in the web layer, posts to WPF host to save to **Videos\Photobooth** (default).
- Menu: File → Save Folder… lets you pick another folder.
- Local gif.js path with CDN fallback. Replace placeholders with real files for offline use:
  - wwwroot/lib/gif/gif.js
  - wwwroot/lib/gif/gif.worker.js
