---
name: mixamo
description: Find, preview and download Mixamo animation clips with Tools/mixamo/mixamo.py. Use when a task needs a new humanoid animation, or the user mentions Mixamo.
---

# Mixamo clips

`Tools/mixamo/mixamo.py` searches a local copy of the Mixamo catalogue, renders preview filmstrips, and exports FBX clips for the X Bot rig the SS3D humanoid avatar is built from. Run it with `python3 -I`; `--help` lists every flag. Files land in `~/MixamoLibrary/` (outside the project) until you copy the chosen ones in.

The token belongs to the owner's Adobe account and lives in `~/.config/mixamo-tools/config.json`. It expires after about a day. On a 401 or "No Mixamo token", stop and ask the owner to run `! python3 -I Tools/mixamo/mixamo.py setup` and paste a fresh `localStorage.access_token` from mixamo.com. Keep the config file's contents out of output and commits.

## Steps

1. **Search.** `mixamo.py search <words> [-exclude]`. If it reports no catalogue, run `mixamo.py index` first (about a minute). Mixamo names are descriptive phrases ("Walking With An Aimed Pistol"); pack motions are short lowercase names ("pistol walk"), and a pack hit lists the motions that matched. Try synonyms before concluding a clip doesn't exist: *punch/jab/hook*, *throw/toss/lob*, *hit/impact/reaction*, *die/death/dying*. Done when you have a shortlist of ids, or have tried at least three phrasings for each motion you need.
2. **Look.** `mixamo.py preview <id> [--motion NAME]` prints a PNG path; Read it. The strip is 8 frames left to right across the clip. Compare the shortlist on what the task needs (which hand, stance, weapon height, whether the body travels), and reject clips whose silhouette is wrong rather than hoping the import fixes it. `mixamo.py show <id>` gives duration, `loop`, `supports_inplace` and each pack motion's numbered name. Done when each needed motion has one chosen clip and a stated reason.
3. **Download.** `mixamo.py download <id> [--motion NAME]`. Omit `--motion` on a pack to fetch all of it. Default exports match the clips already in `Assets/Art/Animations/` (no skin, 30 fps, root motion kept); add `--in-place`, `--mirror` or `--trim start,end` (percent) only when the task calls for it. Download only the clips you chose: each export is a request on the owner's account.
4. **Import.** Copy the FBX files into `Assets/Art/Animations/<Folder>/` and apply the import recipe in [2026-11_humanoid-animation-pass.md](../../../Documents/architecture/2026-11_humanoid-animation-pass.md) (Humanoid rig, avatar copied from `Locomotion Pack/X Bot.fbx`). Done when the clips import as Humanoid with no avatar errors in the console.

Pack motions that share a name (left and right variants) are numbered `name (2)`, matching the owner's earlier zip downloads.
