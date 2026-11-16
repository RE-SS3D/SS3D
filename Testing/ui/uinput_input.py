"""Real OS input for the lobby pilots through a virtual uinput device (kernel level, below the compositor).

Usage:
  uinput_input.py serve               read commands (below) from stdin, one per line; reply "ok ..." or "error ..."
  uinput_input.py click WINDOW X Y    left-click at window-relative pixels (X11 window id, decimal or 0x hex)
  uinput_input.py type TEXT           type printable ASCII (US layout)
  uinput_input.py key NAME            press one key by X keysym name (Return, Escape, Tab, BackSpace, ...)

Use serve for anything that clicks and then types: keys from a second, newly created device did not reach
the window the first device clicked (tested with an Xwayland Tk entry on GNOME 49), while one device that
both clicks and types works. One-shot commands are for manual checks.

Why not xdotool: on GNOME Wayland, XTEST from X clients goes through Xwayland's libei portal and asks the
user for "remote interaction" permission on every run. uinput needs no permission dialog, only write access
to /dev/uinput (granted to the session user by the uaccess tag, e.g. Steam's 60-steam-input.rules).

Coordinates: the device is an absolute pointer spanning the X root window. Mutter maps it over the whole
logical desktop, which equals the X root when every monitor runs at scale 1 (the tested setup). The window
position comes from Xlib XTranslateCoordinates, a plain query that does not trigger the portal.
Standard library only, so it runs from any python3.
"""
import ctypes
import ctypes.util
import fcntl
import os
import struct
import subprocess
import sys
import time

EV_SYN, EV_KEY, EV_ABS = 0x00, 0x01, 0x03
SYN_REPORT = 0
ABS_X, ABS_Y = 0x00, 0x01
BTN_LEFT = 0x110
UI_SET_EVBIT, UI_SET_KEYBIT, UI_SET_ABSBIT = 0x40045564, 0x40045565, 0x40045567
UI_DEV_SETUP, UI_ABS_SETUP = 0x405C5503, 0x401C5504
UI_DEV_CREATE, UI_DEV_DESTROY = 0x5501, 0x5502
BUS_VIRTUAL = 0x06
# libinput and the compositor need a moment to pick up a new device before its events count.
DEVICE_SETTLE_SECONDS = 2.0

KEY_LEFTSHIFT = 42
NAMED_KEYS = {
    "Return": 28, "KP_Enter": 96, "Escape": 1, "Tab": 15, "BackSpace": 14, "space": 57, "Delete": 111,
    "Up": 103, "Down": 108, "Left": 105, "Right": 106, "Home": 102, "End": 107,
}
# US layout: character -> (keycode, shift).
CHARS = {}
for row, codes in (("1234567890", range(2, 12)), ("qwertyuiop", range(16, 26)),
                   ("asdfghjkl", range(30, 39)), ("zxcvbnm", range(44, 51))):
    for char, code in zip(row, codes):
        CHARS[char] = (code, False)
        if char.isalpha():
            CHARS[char.upper()] = (code, True)
for char, code in zip("!@#$%^&*()", range(2, 12)):
    CHARS[char] = (code, True)
for plain, shifted, code in ((" ", None, 57), ("-", "_", 12), ("=", "+", 13), ("[", "{", 26), ("]", "}", 27),
                             (";", ":", 39), ("'", '"', 40), ("`", "~", 41), ("\\", "|", 43), (",", "<", 51),
                             (".", ">", 52), ("/", "?", 53)):
    CHARS[plain] = (code, False)
    if shifted:
        CHARS[shifted] = (code, True)


def desktop_point(window=None, x=0, y=0):
    """Window-relative pixels -> (root x, root y, root width, root height) via Xlib queries; None = root."""
    xlib = ctypes.CDLL(ctypes.util.find_library("X11") or "libX11.so.6")
    xlib.XOpenDisplay.restype = ctypes.c_void_p
    xlib.XOpenDisplay.argtypes = [ctypes.c_char_p]
    xlib.XDefaultScreen.argtypes = [ctypes.c_void_p]
    xlib.XRootWindow.restype = ctypes.c_ulong
    xlib.XRootWindow.argtypes = [ctypes.c_void_p, ctypes.c_int]
    xlib.XDisplayWidth.argtypes = xlib.XDisplayHeight.argtypes = [ctypes.c_void_p, ctypes.c_int]
    xlib.XTranslateCoordinates.argtypes = [ctypes.c_void_p, ctypes.c_ulong, ctypes.c_ulong, ctypes.c_int, ctypes.c_int,
                                           ctypes.POINTER(ctypes.c_int), ctypes.POINTER(ctypes.c_int),
                                           ctypes.POINTER(ctypes.c_ulong)]
    xlib.XCloseDisplay.argtypes = [ctypes.c_void_p]
    display = xlib.XOpenDisplay(None)
    if not display:
        raise SystemExit("cannot open X display (DISPLAY unset?)")
    try:
        screen = xlib.XDefaultScreen(display)
        root = xlib.XRootWindow(display, screen)
        window = root if window is None else window
        root_x, root_y, child = ctypes.c_int(), ctypes.c_int(), ctypes.c_ulong()
        if not xlib.XTranslateCoordinates(display, window, root, x, y,
                                          ctypes.byref(root_x), ctypes.byref(root_y), ctypes.byref(child)):
            raise ValueError(f"window {window:#x} is not on the default screen")
        return root_x.value, root_y.value, xlib.XDisplayWidth(display, screen), xlib.XDisplayHeight(display, screen)
    finally:
        xlib.XCloseDisplay(display)


def activate_window(window):
    """Ask the window manager to raise/focus the client's frame.

    XRaiseWindow alone does not move a reparented WM frame, and the weekly runner maximizes the
    editor, so the frame can stay behind it. `windowactivate` is a WM message, not XTEST input, so
    it does not trigger GNOME's remote-interaction portal.
    """
    try:
        subprocess.run(["xdotool", "windowactivate", "--sync", str(window)],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                       timeout=10, check=False)
    except (OSError, subprocess.SubprocessError):
        pass


def raise_window(window):
    """Bring the click target to the top and focus it (plain X requests; no XTEST/portal).

    The built client's frame can sit behind the editor's window even though the client renders
    offscreen (its screenshot still looks right), so the pointer click would land on the editor.
    Raise the frame before every click.
    """
    activate_window(window)
    xlib = ctypes.CDLL(ctypes.util.find_library("X11") or "libX11.so.6")
    xlib.XOpenDisplay.restype = ctypes.c_void_p
    xlib.XRaiseWindow.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
    xlib.XSetInputFocus.argtypes = [ctypes.c_void_p, ctypes.c_ulong, ctypes.c_int, ctypes.c_ulong]
    xlib.XFlush.argtypes = [ctypes.c_void_p]
    xlib.XCloseDisplay.argtypes = [ctypes.c_void_p]
    display = xlib.XOpenDisplay(None)
    if not display:
        return
    try:
        xlib.XRaiseWindow(display, window)
        # RevertToPointerRoot = 1, CurrentTime = 0; some WMs ignore the focus request.
        xlib.XSetInputFocus(display, window, 1, 0)
        xlib.XFlush(display)
    finally:
        xlib.XCloseDisplay(display)


class VirtualDevice:
    def __init__(self, width, height):
        self.fd = os.open("/dev/uinput", os.O_WRONLY | os.O_NONBLOCK)
        fcntl.ioctl(self.fd, UI_SET_EVBIT, EV_KEY)
        fcntl.ioctl(self.fd, UI_SET_EVBIT, EV_ABS)
        # udev only tags ID_INPUT_KEYBOARD (which the compositor needs to deliver keys) when every keycode
        # 1..31 is declared; declaring just the used keys left the device a pointer that silently dropped typing.
        for code in [*range(1, 128), BTN_LEFT]:
            fcntl.ioctl(self.fd, UI_SET_KEYBIT, code)
        for axis, size in ((ABS_X, width), (ABS_Y, height)):
            fcntl.ioctl(self.fd, UI_SET_ABSBIT, axis)
            # struct uinput_abs_setup: u16 code, (pad), input_absinfo {value, min, max, fuzz, flat, resolution}.
            fcntl.ioctl(self.fd, UI_ABS_SETUP, struct.pack("HxxiiiiiI", axis, 0, 0, size - 1, 0, 0, 0))
        # struct uinput_setup: input_id {bustype, vendor, product, version}, name[80], ff_effects_max.
        fcntl.ioctl(self.fd, UI_DEV_SETUP, struct.pack("HHHH80sI", BUS_VIRTUAL, 0x5353, 0x3344, 1,
                                                       b"SS3D pilot input", 0))
        fcntl.ioctl(self.fd, UI_DEV_CREATE)
        time.sleep(DEVICE_SETTLE_SECONDS)

    def emit(self, kind, code, value):
        now = time.time()
        os.write(self.fd, struct.pack("llHHi", int(now), int((now % 1) * 1e6), kind, code, value))

    def sync(self):
        self.emit(EV_SYN, SYN_REPORT, 0)

    def tap(self, code, shift=False):
        if shift:
            self.emit(EV_KEY, KEY_LEFTSHIFT, 1)
            self.sync()
        self.emit(EV_KEY, code, 1)
        self.sync()
        time.sleep(0.02)
        self.emit(EV_KEY, code, 0)
        self.sync()
        if shift:
            self.emit(EV_KEY, KEY_LEFTSHIFT, 0)
            self.sync()
        time.sleep(0.04)

    def close(self):
        time.sleep(0.2)  # let the last events drain before the device disappears
        fcntl.ioctl(self.fd, UI_DEV_DESTROY)
        os.close(self.fd)


def perform(device, words):
    """Run one command on an existing device; returns a short description or raises ValueError."""
    command = words[0] if words else ""
    if command == "click" and len(words) == 4:
        window = int(words[1], 0)
        raise_window(window)
        x, y, _, _ = desktop_point(window, int(words[2]), int(words[3]))
        device.emit(EV_ABS, ABS_X, x)
        device.emit(EV_ABS, ABS_Y, y)
        device.sync()
        time.sleep(0.1)
        device.tap(BTN_LEFT)
        return f"click at {x},{y}"
    if command == "type" and len(words) > 1:
        text = " ".join(words[1:])
        missing = sorted({c for c in text if c not in CHARS})
        if missing:
            raise ValueError(f"cannot type {missing!r}: printable US ASCII only")
        for char in text:
            device.tap(*CHARS[char])
        return f"typed {len(text)} chars"
    if command == "key" and len(words) == 2:
        name = words[1]
        if name not in NAMED_KEYS and name not in CHARS:
            raise ValueError(f"unknown key {name!r}; known: {', '.join(NAMED_KEYS)}")
        device.tap(*((NAMED_KEYS[name], False) if name in NAMED_KEYS else CHARS[name]))
        return f"key {name}"
    raise ValueError(f"bad command {' '.join(words)!r}; expected click WINDOW X Y | type TEXT | key NAME")


def main(argv):
    if not argv:
        raise SystemExit(__doc__)
    _, _, width, height = desktop_point()
    # The absolute range covers the whole root, so device units equal desktop pixels.
    device = VirtualDevice(width, height)
    try:
        if argv[0] != "serve":
            try:
                print("ok " + perform(device, argv))
            except ValueError as error:
                raise SystemExit(str(error))
            return
        print("ready", flush=True)
        for line in sys.stdin:
            try:
                print("ok " + perform(device, line.rstrip("\n").split(" ")), flush=True)
            except (ValueError, OSError) as error:
                print(f"error {error}", flush=True)
    finally:
        device.close()


if __name__ == "__main__":
    main(sys.argv[1:])
