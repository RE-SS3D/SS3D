#!/usr/bin/env python3
"""Agent-friendly Mixamo CLI: index the catalogue, search it, preview, download.

    mixamo.py setup [--character-id ID]   # paste token on stdin (owner only; defaults to the primary character)
    mixamo.py index [--refresh]           # fetch full catalogue -> catalog.json
    mixamo.py search pistol walk -crouch  # ranked local search
    mixamo.py show ID                     # details incl. pack motions + params
    mixamo.py preview ID [--motion NAME]  # GIF -> filmstrip PNG (agents can Read it)
    mixamo.py download ID [--motion NAME] [--in-place] [--mirror] [--trim 0,100]

Token + character id live in ~/.config/mixamo-tools/config.json (mode 600).
The token is only ever sent to www.mixamo.com. Files go to $MIXAMO_LIBRARY
(default ~/MixamoLibrary), outside the Unity project.
Stdlib only; run with `python3 -I`.
"""
import argparse
import getpass
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request

API = "https://www.mixamo.com/api/v1"
CONFIG = pathlib.Path.home() / ".config" / "mixamo-tools" / "config.json"
LIBRARY = pathlib.Path(os.environ.get("MIXAMO_LIBRARY", pathlib.Path.home() / "MixamoLibrary"))
CATALOG = LIBRARY / "catalog.json"
PAGE_LIMIT = 96


# ── config / http ──────────────────────────────────────────────────────

def load_config():
    cfg = json.loads(CONFIG.read_text()) if CONFIG.exists() else {}
    if os.environ.get("MIXAMO_TOKEN"):
        cfg["token"] = os.environ["MIXAMO_TOKEN"]
    if not cfg.get("token"):
        sys.exit("No Mixamo token. The owner must run: mixamo.py setup")
    return cfg


def save_config(cfg):
    CONFIG.parent.mkdir(parents=True, exist_ok=True)
    CONFIG.touch(mode=0o600, exist_ok=True)
    os.chmod(CONFIG, 0o600)
    CONFIG.write_text(json.dumps(cfg, indent=2))


def request(cfg, method, path, body=None, retries=4):
    url = path if path.startswith("http") else API + path
    assert url.startswith("https://www.mixamo.com/"), url  # token never leaves mixamo.com
    headers = {
        "Accept": "application/json",
        "Content-Type": "application/json",
        "Authorization": f"Bearer {cfg['token']}",
        "X-Api-Key": "mixamo2",
        "X-Requested-With": "XMLHttpRequest",
    }
    data = json.dumps(body).encode() if body is not None else None
    for attempt in range(1, retries + 1):
        req = urllib.request.Request(url, data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(req, timeout=60) as res:
                raw = res.read()
                return res.status, (json.loads(raw) if raw else None)
        except urllib.error.HTTPError as e:
            if e.code == 429 and attempt < retries:
                time.sleep(attempt * 3)
                continue
            if e.code == 401:
                sys.exit("Mixamo returned 401: token expired. The owner must run: mixamo.py setup")
            if e.code == 202:
                return 202, json.loads(e.read() or b"null")
            raise SystemExit(f"{method} {url} -> HTTP {e.code}: {e.read()[:300]!r}")
    raise SystemExit(f"{method} {url} -> rate limited")


def fetch_public(url, dest):
    """Download a CDN file (no auth header)."""
    with urllib.request.urlopen(urllib.request.Request(url), timeout=120) as res:
        dest.write_bytes(res.read())


def sanitize(name):
    return re.sub(r"\s+", " ", re.sub(r'[<>:"/\\|?*]+', "_", name)).strip()


# ── catalogue ──────────────────────────────────────────────────────────

def load_catalog():
    if not CATALOG.exists():
        sys.exit(f"No catalogue at {CATALOG}. Run: mixamo.py index")
    return json.loads(CATALOG.read_text())


def find_entry(catalog, ident):
    for e in catalog["entries"]:
        if e["id"] == ident:
            return e
    sys.exit(f"No catalogue entry with id {ident} (ids come from `search`)")


def cmd_setup(args):
    cfg = json.loads(CONFIG.read_text()) if CONFIG.exists() else {}
    print("Get the token: log in at mixamo.com, open the browser console (F12), run")
    print("  localStorage.access_token\nand paste the value (without quotes).", file=sys.stderr)
    token = getpass.getpass("Token: ").strip().strip('"')
    if len(token) < 20:
        sys.exit("That does not look like a token.")
    cfg["token"] = token
    if args.character_id:
        cfg["character_id"] = args.character_id
    save_config(cfg)
    try:
        _, prim = request(cfg, "GET", "/characters/primary", retries=1)
    except SystemExit as e:
        print(f"(could not look up the primary character: {e})", file=sys.stderr)
        prim = None
    if not args.character_id and isinstance(prim, dict) and prim.get("primary_character_id"):
        cfg["character_id"] = prim["primary_character_id"]
        save_config(cfg)
    print(f"Saved {CONFIG}. Character: {cfg.get('character_id') or 'UNSET (pass --character-id)'}"
          f" ({(prim or {}).get('primary_character_name', '?')})")


def cmd_index(args):
    if CATALOG.exists() and not args.refresh:
        cat = json.loads(CATALOG.read_text())
        print(f"Catalogue exists ({len(cat['entries'])} entries, {cat['fetched_at']}). Use --refresh.")
        return
    cfg = load_config()
    products, page = [], 1
    while True:
        _, res = request(cfg, "GET", f"/products?page={page}&limit={PAGE_LIMIT}&order=&type=Motion%2CMotionPack&query=")
        products += res.get("results", [])
        pages = res.get("pagination", {}).get("num_pages", 1)
        print(f"page {page}/{pages}", file=sys.stderr)
        if page >= pages:
            break
        page += 1
    entries, seen = [], set()
    for p in products:
        if p["id"] in seen:
            continue
        seen.add(p["id"])
        is_pack = p.get("type") == "MotionPack"
        entry = {
            "id": p["id"], "type": p.get("type"),
            "name": (p.get("name") if is_pack else p.get("description")) or p.get("name"),
            "gif": p.get("thumbnail_animated"), "motions": [],
        }
        if p.get("type") == "MotionPack":
            try:
                _, d = request(cfg, "GET", f"/products/{p['id']}?similar=0&character_id={cfg.get('character_id', '')}")
                entry["motions"] = [m.get("name") or m.get("description") for m in d["details"].get("motions", [])]
            except SystemExit as e:
                print(f"pack {entry['name']}: {e}", file=sys.stderr)
        entries.append(entry)
    LIBRARY.mkdir(parents=True, exist_ok=True)
    CATALOG.write_text(json.dumps({"fetched_at": time.strftime("%Y-%m-%d"), "entries": entries}, indent=1))
    packs = sum(1 for e in entries if e["type"] == "MotionPack")
    print(f"{len(entries)} products ({packs} packs) -> {CATALOG}")


def cmd_search(args):
    cat = load_catalog()
    inc = [t.lower() for t in args.terms if not t.startswith("-")]
    exc = [t[1:].lower() for t in args.terms if t.startswith("-") and len(t) > 1]
    hits = []
    for e in cat["entries"]:
        if args.packs_only and e["type"] != "MotionPack":
            continue
        name = (e["name"] or "").lower()
        motions = [m.lower() for m in e["motions"] if m]
        hay = name + " " + " ".join(motions)
        if not all(t in hay for t in inc) or any(t in hay for t in exc):
            continue
        score = sum(3 if re.search(rf"\b{re.escape(t)}", name) else 1 for t in inc)
        matched = list(dict.fromkeys(m for m in e["motions"] if m and any(t in m.lower() for t in inc)))
        hits.append((score, e, matched))
    hits.sort(key=lambda h: (-h[0], len(h[1]["name"] or "")))
    for score, e, matched in hits[: args.limit]:
        kind = f"PACK({len(e['motions'])})" if e["type"] == "MotionPack" else "motion"
        line = f"{e['id']}  {kind:9} {e['name']}"
        if matched:
            line += "  ↳ " + ", ".join(matched[:6]) + (" …" if len(matched) > 6 else "")
        print(line)
    print(f"-- {len(hits)} matches (showing {min(len(hits), args.limit)})", file=sys.stderr)


def product_details(cfg, entry):
    _, d = request(cfg, "GET", f"/products/{entry['id']}?similar=0&character_id={cfg['character_id']}")
    return d


def pick_motions(d, motion_filter):
    det = d["details"]
    if det.get("motions"):
        # Packs reuse names (left/right variants); number repeats like Mixamo's zip does.
        seen, labelled = {}, []
        for m in det["motions"]:
            base = m.get("name") or m.get("description")
            seen[base] = seen.get(base, 0) + 1
            labelled.append((base if seen[base] == 1 else f"{base} ({seen[base]})", m))
        if motion_filter:
            exact = [x for x in labelled if x[0].lower() == motion_filter.lower()]
            labelled = exact or [x for x in labelled if motion_filter.lower() in x[0].lower()]
            if not labelled:
                sys.exit(f"No pack motion matches '{motion_filter}'")
        return labelled
    return [(d.get("description") or d.get("name"), det)]


def require_character(cfg):
    if not cfg.get("character_id"):
        sys.exit("No character id. The owner must run: mixamo.py setup --character-id <X Bot id>")


def cmd_show(args):
    cfg = load_config(); require_character(cfg)
    entry = find_entry(load_catalog(), args.id)
    d = product_details(cfg, entry)
    print(f"{entry['name']}  [{entry['type']}]  gif: {entry['gif']}")
    for name, m in pick_motions(d, args.motion):
        g = m.get("gms_hash", {})
        params = {p[0]: p[1] for p in g.get("params", [])} if isinstance(g.get("params"), list) else g.get("params")
        print(f"  - {name}: inplace={g.get('inplace')} mirror={g.get('mirror')} trim={g.get('trim')} "
              f"supports_inplace={m.get('supports_inplace')} loop={m.get('loopable')} "
              f"duration={m.get('duration')} params={params}")


def cmd_preview(args):
    entry = find_entry(load_catalog(), args.id)
    url, label = entry["gif"], entry["name"]
    if args.motion:
        cfg = load_config(); require_character(cfg)
        d = product_details(cfg, entry)
        name, m = pick_motions(d, args.motion)[0]
        url = m.get("thumbnail_animated") or url
        label = name
    if not url:
        sys.exit("No preview GIF for this entry")
    out_dir = LIBRARY / "_previews"
    out_dir.mkdir(parents=True, exist_ok=True)
    png = out_dir / (sanitize(label) + ".png")
    with tempfile.TemporaryDirectory() as tmp:
        gif = pathlib.Path(tmp) / "in.gif"
        fetch_public(url, gif)
        subprocess.run(["magick", str(gif), "-coalesce", str(pathlib.Path(tmp) / "f_%04d.png")], check=True)
        frames = sorted(pathlib.Path(tmp).glob("f_*.png"))
        n = min(args.frames, len(frames))
        pick = [frames[round(i * (len(frames) - 1) / max(n - 1, 1))] for i in range(n)]
        subprocess.run(["magick", "montage", *map(str, pick), "-tile", f"{n}x1",
                        "-geometry", "220x220+2+2", "-background", "#808080", str(png)], check=True)
    print(png)


def cmd_download(args):
    cfg = load_config(); require_character(cfg)
    entry = find_entry(load_catalog(), args.id)
    d = product_details(cfg, entry)
    folder = LIBRARY / ("Packs/" + sanitize(entry["name"])[:60] if entry["type"] == "MotionPack" else "Singles")
    folder.mkdir(parents=True, exist_ok=True)
    for name, m in pick_motions(d, args.motion):
        g = dict(m["gms_hash"])
        if args.in_place:
            g["inplace"] = True
        if args.mirror:
            g["mirror"] = True
        if args.trim:
            g["trim"] = [int(x) for x in args.trim.split(",")]
        if isinstance(g.get("params"), list):
            g["params"] = ",".join(str(p[1]) for p in g["params"])
        suffix = ("_InPlace" if args.in_place else "") + ("_Mirror" if args.mirror else "")
        dest = folder / (sanitize(name) + suffix + ".fbx")
        if dest.exists() and not args.force:
            print(f"exists {dest}")
            continue
        request(cfg, "POST", "/animations/export", {
            "character_id": cfg["character_id"], "gms_hash": [g],
            "preferences": {"format": "fbx7", "skin": "false", "fps": "30", "reducekf": "0"},
            "product_name": name, "type": "Motion",
        })
        for _ in range(120):
            status, res = request(cfg, "GET", f"/characters/{cfg['character_id']}/monitor")
            if res and res.get("status") == "completed":
                fetch_public(res["job_result"], dest)
                print(dest)
                break
            if res and res.get("status") == "failed":
                sys.exit(f"export failed for {name}: {res.get('message')}")
            time.sleep(0.5)
        else:
            sys.exit(f"export timed out for {name}")
        time.sleep(0.3)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("setup"); s.add_argument("--character-id"); s.set_defaults(fn=cmd_setup)
    s = sub.add_parser("index"); s.add_argument("--refresh", action="store_true"); s.set_defaults(fn=cmd_index)
    s = sub.add_parser("search"); s.add_argument("terms", nargs="+"); s.add_argument("--limit", type=int, default=30)
    s.add_argument("--packs-only", action="store_true"); s.set_defaults(fn=cmd_search)
    s = sub.add_parser("show"); s.add_argument("id"); s.add_argument("--motion"); s.set_defaults(fn=cmd_show)
    s = sub.add_parser("preview"); s.add_argument("id"); s.add_argument("--motion")
    s.add_argument("--frames", type=int, default=8); s.set_defaults(fn=cmd_preview)
    s = sub.add_parser("download"); s.add_argument("id"); s.add_argument("--motion")
    s.add_argument("--in-place", action="store_true"); s.add_argument("--mirror", action="store_true")
    s.add_argument("--trim", help="start,end percent, e.g. 10,90"); s.add_argument("--force", action="store_true")
    s.set_defaults(fn=cmd_download)
    args = ap.parse_args()
    args.fn(args)


if __name__ == "__main__":
    main()
