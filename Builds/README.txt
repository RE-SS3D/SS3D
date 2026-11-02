SS3D — quick start (this fork)

Building maps? Read TESTERS.md: how to build, save, share and report bugs.

Not an official RE:SS3D build. Default port is 1151 everywhere.

WINDOWS (SS3D-Windows-<version>.zip)

  Game\SS3D.exe           main menu: play locally (hosts a game on this PC), join a
                          server (remembers the last five), and update when a new
                          release is out
  Update_SS3D.bat         update this folder to the latest stable Windows release

UPDATING (WINDOWS)

  The main menu shows a banner when a newer stable release exists; UPDATE AND RESTART
  runs the updater below, waits for the game to close and starts it again afterwards.

  Or close all SS3D windows, then double-click Update_SS3D.bat. Keep Update_SS3D.ps1 next
  to it. Requires Windows PowerShell 5.1 (included with Windows 10/11) and internet access.
  It follows stable numbered vX.Y.Z releases; prereleases and nightlies are ignored.
  The download is SHA-256 checked before installation. Failed downloads leave the game intact.
  Maps, exports, logs, server state and Config are preserved. Unmodified bundled maps are
  refreshed; edited maps keep their contents even if the new release uses the same name.
  Older downloads without a map manifest keep all local maps on their first update.
  Player settings stored outside this folder are untouched.

  Start Game\SS3D.exe as usual after updating. The updater prints the installed version
  and the backup location. To roll back, close the game and run Game\SS3D.exe in the
  printed .ss3d-backups folder. That is the complete installation and local data from BEFORE the update; later
  edits remain in the newer installation. Only that one previous installation is kept;
  older backups are deleted after a successful update.
  Update_SS3D.bat -CheckOnly checks for updates without downloading or installing.

  If an update is interrupted while installing, keep .ss3d-update-* and .ss3d-backups:
  the previous Game folder can be recovered from the latest backup.
  Update_SS3D.ps1 updates itself along with the game; Update_SS3D.bat stays as it is.

LINUX CLIENT (SS3D-Linux-Client-<version>.zip)

  Game/SS3D-Client                             main menu: play locally, join, update
  ./update_ss3d.sh                            update this folder to the latest stable release

  Close all Linux client windows before running ./update_ss3d.sh. Requires Python 3;
  keep update_ss3d.py beside the shell script. ./update_ss3d.sh --check-only checks
  without downloading or installing. Prereleases and nightlies are ignored.
  Local data preservation, SHA-256 verification and .ss3d-backups rollback work as on
  Windows above. Run Game/SS3D-Client inside a backup to use the previous build with its
  pre-update data. The updater never stops a game process. The main menu's
  UPDATE AND RESTART runs it with --wait-pid/--restart in the background (output in
  update.log next to this file) and the game starts again when it finishes.
  A stable release must include its Linux client zip; otherwise updating fails safely.
  No sudo, Unity, Git, curl, jq, or extra Python packages are needed.

LINUX DEDICATED SERVER (SS3D-Linux-Server-<version>.zip)

  ./start_ss3d_server.sh [port]                headless; logs to GameServer/server.log

Players on other machines join with the host's LAN or public IP; forward UDP 1151 for
internet play.

Whoever plays locally is ServerOwner (admin) of that game, whatever name they use.
Add a line "<name> ServerOwner" to Game/Config/permissions.txt to make another name an admin.

PUBLIC / DEDICATED SERVERS

  Names are not passwords: anyone can join under an admin's name. On a dedicated server, an admin
  name (Administrator or ServerOwner in permissions.txt) can only join with a matching token
  from GameServer/Config/admin-tokens.txt, one "<name> <token>" line each. Enter the token
  under the server's details in the main menu (the server appears there after the first
  join), or skip the menu and join directly:
    Game\SS3D.exe -ip=<server ip> -port=<port> -ckey=<name> -token=<token>
    Game/SS3D-Client -ip=<server ip> -port=<port> -ckey=<name> -token=<token>
  Without that file, nobody can join a dedicated server under an admin name.

  ./start_ss3d_server.sh 1151 -openbuild    lets every player use the map editor (B) without
  being an admin. They can't delete maps or overwrite maps someone else saved, can only undo
  their own edits, and are rate-limited.

If Config/ or Data/Tilemaps are missing next to the executable, maps and admin permissions
will not load (host log: "No station templates found to load"). Re-download the release.

Local Unity developers: build into Builds/Game (see Game/BUILD_THE_GAME_HERE.txt) and start
Builds/Game/SS3D.exe (or SS3D.x86_64) directly.
