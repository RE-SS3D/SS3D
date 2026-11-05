> Parent map: [ui-shell.md](ui-shell.md)
> Status: partial
> Verified: ad9c1fef8 — 2026-11-05

# Main menu

## Overview

Client main menu, built from the owner's **SS3D Main menu design(1).zip** reference (the first version plus a music bar). `MainMenuSubSystem`
(DDOL, created by `SystemsBootstrap` by type name, never on dedicated servers or batch/Null-GFX
clients) attaches `MainMenuView` to the shell's Modal layer whenever the **Launcher** scene loads:
a build started without command-line args, `-forceLauncher`, or the editor **Launcher** toolbar
toggle. Launches with args still go through Intro and `ServerConnectionView`. The lobby's **Quit to
menu** returns here from any session (`NetworkSessionSubSystem.LeaveToMainMenu`).

What is live: name (defaults to the OS user name), join a recent server or a pasted
`host[:port]` / `[ipv6]:port`, play locally on the Advanced port (default 1151), per-server label
and admin token, Remove, a status query per recent server (version, players/cap, ping), the
Connecting / Starting local session / Update dialogs with real progress steps, refusal rows and
direct-connect errors, the GitHub update banner and **Update and restart**, Quit, the music bar
(on/off, track title, Next, volume) and links to the SS3D Discord and the SS3D-Art repo.

**Music:** every clip in `Assets/Art/Sound/World/Music` except `Intro` / `IntroSneeze`, streamed,
random first track then in order. It plays on the menu, server list, settings and Connecting,
fades out for Starting local session / Update and when Game loads. Returning to Launcher restarts
the current track from the beginning; toggling music off/on within one visit still resumes it. On/off is saved in
`main-menu.json`; the slider is the player's **Music** volume (same as the in-game Sound tab), and
the source plays through the mixer's Music group. `Space Theme` was converted from the 65 MB WAV to OGG. Not live: the PUBLIC list always shows its empty state (no list
backend), and Settings shows the reference placeholder, not the live settings tabs.

**Wallpaper:** half of the picks are a looping silent video, the rest a still (`MainMenuAssetCatalog.PickWallpaper`), never the same one twice in a row. A new pick lands on every visit to the Launcher and every 60 s on the menu, server list and settings: the screen dips to black, swaps, and lifts once a new video has a frame (`MainMenuWallpaperRotation`, 1.5 s timeout). Dialogs hold the timer. A video opens on the same black until it has a frame, because showing its fallback still first read as a wrong wallpaper jumping to a video. A video's index wraps onto that still, which stays if the video fails or misses the timeout. Connecting, Starting local session and Update pause the video and blur its current frame. It stops and frees its render texture when Game loads, and restarts on return. Stills load lazily; each one shown stays loaded until the next scene load.

This diverges from [main-menu.md](../../design/main-menu.md), on the owner's request (2026-11-05): §11 lists an animated background as out of scope and §8 asks for one wallpaper per launch, ten to fifteen of them, with no gore and Halloween art only in October. The set has 41 stills and 6 videos that rotate, with no seasonal filtering. The owner reviewed it against the no-gore rule on 2026-11-05: `MurderGrinderA` was removed, and `Bug_Pandemic` and `Malf` stay.

## Start here

- `Assets/Scripts/SS3D/Launcher/MainMenuSubSystem.cs` — controller: show on Launcher, hide on Game, join/host/cancel, status refresh, update check
- `Assets/Scripts/SS3D/Launcher/MainMenuState.cs` — `MainMenuState` / `RecentServer` (newest five) and `MainMenuStore` (`persistentDataPath/Settings/main-menu.json`; tokens in plain text)
- `Assets/Scripts/SS3D/Launcher/MainMenuAddress.cs`, `MainMenuServers.cs` — address parsing; status → row (`Classify` by protocol and capacity)
- `Assets/Scripts/SS3D/Launcher/ReleaseCheck.cs` — newest stable `vX.Y.Z` with this platform's zip; `IsNewer` (dev builds of the same version are older; `0.0.0-*` never updates)
- `Assets/Scripts/SS3D/Launcher/MainMenuMusic.cs` — source, fades, track order, readable titles (`TitleOf`)
- `Assets/Scripts/SS3D/Launcher/MainMenuVideoWallpaper.cs` — `VideoPlayer` into a render texture; `Frame` is null until there is one to show
- `Assets/Scripts/SS3D/Launcher/MainMenuWallpaperRotation.cs` — pure timing of the hold, dip, swap and lift; `HoldSeconds` sets the interval
- `Assets/Scripts/SS3D/Launcher/UpdaterLauncher.cs` — starts `Update_SS3D.ps1 -WaitPid -Restart` (visible console) or `update_ss3d.sh --wait-pid --restart` (detached, `update.log`), then the menu quits
- `Assets/Scripts/SS3D/Networking/Status/` — `ServerStatusPacket` (wire format) and `ServerStatusQuery` (own LiteNetLib socket, 3 s timeout)
- `Assets/Scripts/SS3D/Systems/Rounds/ServerStatusPublisher.cs` — server side, answers on the game port while a server runs
- `Assets/Scripts/SS3D/UI/Shell/MainMenu/MainMenuView.cs` — `Present(MainMenuPresentation)`; raises `CommandIssued` / `FieldChanged`
- `Assets/Content/Systems/UI/MainMenu/MainMenu.uxml` / `MainMenu.uss`, `MainMenuAssetPaths.cs` / `MainMenuAssetCatalog.cs`, `MainMenuWallpaper.cs` (blurred dialog backdrop)
- `Assets/Art/Textures/UI/Wallpapers/`, `Assets/Art/Videos/Wallpapers/` — every file in these folders joins the pick
- `Assets/Scripts/SS3D/Editor/MainMenuPreviewWindow.cs` — **SS3D → UI → Preview Main Menu**, sample states; ignores the view's events
- `Assets/Content/Systems/Launcher/LauncherObjects.prefab` — now only the Launcher scene's black camera

## Extension points

- The view never acts on its own: add a `MainMenuCommand` / `MainMenuField`, raise it from the view and handle it in `MainMenuSubSystem.HandleCommand` / `HandleField`.
- Status reply fields: extend `ServerStatus` and bump the `SS3D-STATUS?1` / `!1` magic, so older clients stop decoding instead of misreading. The query is outside FishNet, so it needs no `ProtocolVersion` bump.
- A public list backend fills `MainMenuPresentation.Public` and `PublicList`; the rows reuse `MainMenuServers.Present`.
- New wallpaper: drop it in the folder and rebuild the catalogs. Stills are 1920×1080 with no transparency. Videos need the encode below.
- Encode a video as VP8 `.webm` with no audio, converting the matrix: `-an -vf scale=in_color_matrix=bt709:out_color_matrix=bt601,format=yuv420p -c:v libvpx -b:v 6M -crf 6`. Check the seam first: compare the last frame with the first, and trim to a matching pair if they differ. `Bug_Animation` keeps source frames 5–154 and `Bug_Pandemic` 18–217 for this reason.
- Rebuild assets through **SS3D → Data → Rebuild All UI Catalogs** (`UiCatalogRebuildAll.TryRebuildAll` for automation).

## Pitfalls

- **Rounded `overflow: hidden` containers render solid white at runtime on Linux.** The server panel and progress dialog were white rectangles in Play Mode, though the editor preview looked right. The panel and dialog no longer clip; the header carries its own top radii. Check Play Mode screenshots, not only the preview.
- **A failed join from the menu used to reload Boot.** Before the client transport reaches Started, FishNet's offline scene is still Boot, so `DefaultScene` replaced the Launcher scene with Boot (duplicate NetworkManager destroyed). `BeginSession` calls `ClientConnectionRecovery.ArmEmptyOfflineScene()` first; a failure now lands on Empty and the DDOL menu returns to the server list.
- **The menu owns connection failures.** While it is attached, `ClientConnectionRecovery.FailureUiOwnedElsewhere` hides the OnGUI Retry/Quit box. Refusals that happen after the Game scene loads (full server, wrong admin token: `PlayerSubSystem` just disconnects) still end on that box.
- **An admin can't give a token before the first join.** A server enters the list (and gets a token field) only after a successful join, and a dedicated server refuses an admin name without its token. Workaround: start the player with `-ip= -port= -ckey= -token=`. Deferred on purpose: admin identity belongs to [player-accounts](../../design/player-accounts.md), not to more menu UI.
- **Wallpaper changes use `MainMenuView.PresentWallpaper`, not `Present`.** A full present rebuilds the server list and is deferred while a field in it has focus, which would leave the veil black.
- **Re-presenting rebuilds the server list.** Status replies that arrive while a field in the list has focus are deferred (`IsEditingServerList`), and address/label/token typing never re-presents; otherwise focus and caret are lost mid-typing. The name field keeps its untrimmed value for the same reason.
- **Servers without the status patch never answer**, so the menu shows them Offline and disables Join until they update.
- **`NetworkSettings` is an asset.** In the editor, the menu's writes (name, address, port, network type) outlived Play Mode and overwrote the developer's editor settings. `MainMenuSubSystem` remembers the first values it overwrites and restores them in `OnDestroyed`.
- **Leaving a session must wait for FishNet's offline swap.** Stopping the client makes `DefaultScene` load Empty (Single); loading Launcher immediately would be replaced by it. `LeaveToMainMenu` waits until both transports are stopped and Empty is active, then loads Launcher. The menu re-unlocks the cursor on show.
- **No AudioListener after a failed join.** The Empty scene has no camera, so the music went silent ("no audio listeners" spam). The menu adds its own listener whenever none exists and removes it on hide.
- **Launcher music survives scene changes.** The DDOL source pauses after fading out; `Show` must stop/reset it before presenting a fresh launcher visit or it resumes the old playback position.
- **Unity's Slider sits high in a row.** Its field box is taller than the tracker and the tracker hugs its top. The thumb must use absolute positioning to avoid its default relative vertical offset. The music slider pins the input, drag container, tracker and dragger to a 16px box, with a rounded thumb and a value-driven filled track matching the reference.
- Wallpaper imports must preserve NPOT dimensions; the catalog builder keeps the supplied 1920×1080 size, disables mips and uses BC7 (`CompressedHQ`).
- **The catalog sits in Resources, so a plain `Texture2D[]` loads every wallpaper at launch.** `_wallpapers` is `LazyLoadReference<Texture2D>[]`; validate with `.isSet`, because `.asset` would load them all.
- **Unity decodes VP8 as BT.601, and VP8 has no tag to say otherwise.** The HD sources are BT.709, so an unconverted encode plays with shifted colours and no error. Measured against the source: 35.3 dB unconverted, 38.4 dB converted.
- **Use VP8, not the source H.264.** Unity decodes VP8 itself on every target; H.264 relies on the OS. Tested on the Linux player only; Windows is unconfirmed.
- **The video layer fades over 0.5 s, so the blur is hidden on the first dialog frame.** A screenshot taken then shows the sharp video and looks like a bug. Wait for the fade.
- **Launching a build with any window or log flag skips this menu.** It opens on Intro instead; add `-forceLauncher`.
- **Play from a scene other than Boot has no `NetworkManager`.** The menu shows but Play Locally refuses. `InstanceFinder` logs and rescans the scenes on every miss, so anything polling it each frame guards on `NetworkManager.Instances.Count` first.
- Gurajada's natural line box is taller than the reference's `line-height: 1`; navigation titles need explicit 20px text boxes.
- A ScrollView's percentage content min-height did not pin the footer; the view sets the sidebar content's min-height from its own geometry.
- Editor-window capture must use a linear RenderTexture (an sRGB target washes out colours); screen-pixel readback can return black on Linux. Use `MainMenuPreviewWindow.CaptureFrame`.

## Tests

- `MainMenuTests` (EditMode): wallpaper index order and fallback stills, the video/still split, no repeat picks, rotation timing (timeout, dialogs), wallpaper import settings, videos silent and untranscoded VP8, track titles, status packet round trip and clipping, classification, row naming, address parsing, recent-server ordering/cap, store round trip and broken file, release selection and `IsNewer`, and a status query over loopback.
- `python3 -B Tools/updater/test_update_linux.py` and `Tools/updater/test-update.ps1` cover `--wait-pid` / `-WaitPid` and `--restart`.
- Play Mode check (2026-11-02, ad hoc): Launcher toggle on, menu → Join → direct connect to a closed port returns to the list with the error and no OnGUI box (scene Empty); Play Locally reaches the lobby; a raw UDP status query to the host returned protocol, name, 1/32 players. Music (same day): plays on the Music group, Next and the toggle change track/fade, keeps playing after the failed join (menu listener added), and has faded to silence once the lobby loads; editor NetworkSettings were unchanged after Play Mode. Quit to menu: host → lobby → Quit to menu → host again → Quit to menu in one process, and an Intro-started host → Quit to menu, all without errors. Not yet covered: a real join to a remote server through the menu, and an in-game update of a packaged build.

## Depends on / Used by

- **Depends on:** [ui-shell](ui-shell.md) layers/catalogs; [networking-session](networking-session.md) (`NetworkSessionSubSystem`, `ClientConnectionRecovery`, Tugboat status patch); release updaters ([CI release pipeline](../2026-07_ci-develop-release-pipeline.md#windows-release-updates)).
- **Used by:** players launching a build without arguments.

## Related docs

- [rounds-lobby](rounds-lobby.md) — the lobby the menu hands over to; its Settings tabs are what the placeholder should eventually mount
- [agent-first composition](../2026-07_agent-first-composition.md)
