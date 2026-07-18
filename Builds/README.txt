SS3D — quick start (this fork)

Not an official RE:SS3D build. Unzip so you have:

  Start_SS3D_Host.bat
  Start_SS3D_Client_*.bat
  Game\SS3D.exe
  ...

1. Double-click Start_SS3D_Host.bat (self-hosts; skips the launcher).
2. On the same PC (or another on the LAN), run a Client bat — default is 127.0.0.1:1151.
3. Language host bats set -language=fr / pt / ru.

Dedicated Linux server binaries are a separate download on the same GitHub prerelease
when cut via Actions → Develop Release. Day-to-day play does not need them: Host.bat is enough.

Local Unity developers: build into Builds/Game (see Game/BUILD_THE_GAME_HERE.txt) and use
these same bats from the Builds/ folder.
