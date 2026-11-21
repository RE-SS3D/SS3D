"""Serve the repo root for the UI bible, with caching off so CSS edits show on reload.

    python Documents/plans/assets/ui-bible/serve.py [port]

then open http://localhost:8642/Documents/plans/assets/ui-bible/
The bible loads sprites and fonts from Assets/ by relative path, so the server root must be the repo root.
"""
import functools
import http.server
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[4]
PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8642


class NoCacheHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, fmt, *args):
        pass


if __name__ == "__main__":
    handler = functools.partial(NoCacheHandler, directory=str(REPO))
    with http.server.ThreadingHTTPServer(("127.0.0.1", PORT), handler) as server:
        print(f"UI bible: http://localhost:{PORT}/Documents/plans/assets/ui-bible/  (serving {REPO})", flush=True)
        server.serve_forever()
