# SS3D map building: testers' guide

Thanks for testing. This build lets you build a space station, play on it, and share it.
Expect bugs. Finding them is the point.

## 1. Start the game

Unzip the download and run **`Game\SS3D.exe`** (Linux: `Game/SS3D-Client`), then choose
**Play locally**. This hosts a game on your PC and makes you the admin, so the map editor works
straight away. You don't need to edit any files.

Friends on your network can join from their own main menu, using your IP address.

On Windows, close the game and run **`Update_SS3D.bat`** regularly for the latest stable
release (prereleases and nightlies are ignored). Your maps and local data are preserved; the previous
installation is kept in `.ss3d-backups/` (only the one before the latest update). Keep both updater files beside the `Game` folder.
The main menu also offers **Update and restart** when a new release is out. See `README.txt` for rollback instructions.
On Linux, use **`./update_ss3d.sh`** instead (requires Python 3; keep `update_ss3d.py`
beside it). It follows the same stable channel and preserves the same local data.

## 2. Build

Press **B** to open the map editor. Press **B** again to close it.

- Search finds anything in any tab, by name or tag: "wall", "pipe", "apc", "security". Click a result
  to switch to its tab. **Map Check** issues that need an object have a **Show in palette** button.
- The editor reopens on the tab you used last.
- **Scripts → Spawn Points** sets where players appear. **Scripts → Rooms** paints rooms and
  sets their name, department and door access. **Scripts → Starting Air** marks what air a room starts with.
- To erase, pick the **Delete** tool: it removes whatever the open sub-tab places (Cables removes
  cables, Gas Pipes removes pipes, and so on). Or hold **Ctrl** while building: click or drag erases
  the same way, and letting go of Ctrl gives you back what you were holding.
- Floors, walls and furniture placed over empty space get a subfloor underneath automatically
  (Settings → Auto Subfloor turns this off). A floor dragged over another floor repaints it, and a
  door dropped on a wall replaces it. Hold **Alt** to replace anything else.
- Wall mounts go on the side of the wall you point at. Doors turn to line up with their wall.
  **R** still rotates either.
- The **Room** tool (key **5**) draws walls around a rectangle and a floor inside it in one drag. It
  uses the last wall and floor you picked; pick another in the library (a window works too) to change them.
- Right click (or Esc) cancels a drag. Right click with nothing dragged puts down what you're holding.
- Ctrl+Z / Ctrl+Y undo and redo.

## 3. Save

Click the **Save** icon, type a name, then click **Save Map**. Afterwards the editor lists anything
the map still needs, such as missing spawn points. Click an entry to jump to it.

To play on your map, choose it in the lobby's server settings and start a round. You can keep
editing during the round. When the round ends, the map goes back to your last save, so round damage
is never saved into it.

## 4. Share your map

In the **Save** menu, click **Save & Export**. A folder opens with `<your map>.zip`. Drag that zip
into Discord. It's small enough to upload even for large stations.

To play someone else's map, open the **Load** menu, click **Open Maps Folder**, and drop their
`.zip` (or `.json`) into that folder. Reopen the Load menu and the map shows up.

All your maps are plain files in `Game/Data/StationTemplates/`.

## 5. Report a bug

Press **F8** the moment something goes wrong (or choose **Report a bug** in the Escape menu).
The game keeps running, and the screenshot shows what you saw before the dialog opened.

- Write a short **summary**. The other fields are optional.
- Leave **Include current map** ticked for map problems. It adds your map as it is now,
  unsaved edits included, without changing your saved file.
- Click **Save Report**. The report, your logs, the screenshot and the map go into one zip in
  `Game/Data/Exports/`.

Then click **Open GitHub Issue** to file it at <https://github.com/RE-SS3D/SS3D/issues>
with the zip attached, or post the zip in Discord.

If the game crashed and you can't press F8, report it the same way by hand: say what you did,
what you expected and what happened, and attach the log files from `Game/Logs/` (plus the exported
map `.zip` for map problems).
