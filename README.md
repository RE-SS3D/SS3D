<h3 align="center"><img src="https://raw.githubusercontent.com/RE-SS3D/SS3D-Art/main/Artwork/Banners/BorderedBanners/SS3DBanner1.png" alt="SS3D">Space Station 3D is an open-source resurrection of <a href="https://www.youtube.com/watch?v=VB7ddhayLKA&list=PLTkyYJ6zDmhoiQv5PJYT6oI766G4s60Av">*beep's original SS3D</a>, which was an impressive demo attempting to give the infamous <a href="https://spacestation13.com/"><img src="https://img.shields.io/badge/Space%20Station-13-red?style=flat-square" alt="ss13" align="center"></a> an extra dimension.</h3>

<br>

<h1 align="center">THIS FORK</h1>

This repository is **[henkhooft/SS3D](https://github.com/henkhooft/SS3D)** — a development fork of the official
[RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D) project. It is an active experiment in redesigning core gameplay
systems and development foundations at a faster pace than upstream's current review capacity supports.

**Read [Documents/FORK_STATUS.md](Documents/FORK_STATUS.md)** for what has diverged and what may be proposed back
upstream. Design specs live in [Documents/design/](Documents/design/); implementation plans go in
[Documents/architecture/](Documents/architecture/). See [Documents/SKILL.md](Documents/SKILL.md) before editing
either.

This is not the official RE:SS3D release channel. For the upstream project, community, and official builds, see
[RE-SS3D/SS3D](https://github.com/RE-SS3D/SS3D).

<br>

<h1 align="center">INFO</h1>

**[Fork status](Documents/FORK_STATUS.md) · [Design docs](Documents/design/) · [Doc conventions](Documents/SKILL.md)**
[<img src="https://img.shields.io/github/discussions/henkhooft/SS3D?color=blueviolet&label=Discussions&style=plastic&logo=github" alt="GitHub Discussions" align="right">](https://github.com/henkhooft/SS3D/discussions)

<br>

<h1 align="center">BUILD</h1>

[<img src="Documents/Images/currentproject.png" alt="Current Project" width="150" align="left">](https://github.com/henkhooft/SS3D/archive/develop.zip)

This fork is not the official RE:SS3D release channel. Optionally, maintainers can cut a
**manual prerelease** via GitHub Actions → **Develop Release** after EditMode + Linux multiplayer
smoke: primary download is a **Windows** zip (`Game\SS3D.exe` + `Start_SS3D_*.bat` for self-host);
Linux client/server zips are secondary. See
[Documents/architecture/2026-07_ci-develop-release-pipeline.md](Documents/architecture/2026-07_ci-develop-release-pipeline.md).
Otherwise clone **`develop`** and build in Unity yourself.
[<img src="https://img.shields.io/github/repo-size/henkhooft/SS3D?color=gold&label=Repository%20Size&style=plastic" alt="Repo Size" align="right">](https://github.com/henkhooft/SS3D)

```bash
git clone https://github.com/henkhooft/SS3D.git
cd SS3D
git checkout develop
```

Open the project in Unity (see [Documents/CONTRIBUTING.md](Documents/CONTRIBUTING.md) for setup notes).

***Note:*** *This fork is in active development. Systems, interactions, and design docs are evolving — what you
build today may not match upstream RE:SS3D.*

<br>

<h1 align="center">CONTRIBUTING</h1>

[<img src="https://img.shields.io/github/issues-raw/henkhooft/SS3D?color=green&label=Issues%20%28Open%29&logo=github&style=plastic" alt="Open Issues" align="right">](https://github.com/henkhooft/SS3D/issues)Read [Documents/SKILL.md](Documents/SKILL.md) before writing design or architecture docs. Gameplay specs belong in [Documents/design/](Documents/design/).[<img src="https://img.shields.io/github/issues-closed-raw/henkhooft/SS3D?color=red&label=Issues%20%28Closed%29&logo=github&style=plastic" alt="Closed Issues" align="right">](https://github.com/henkhooft/SS3D/issues?q=is%3Aissue+is%3Aclosed)

[<img src="https://img.shields.io/github/issues-pr-raw/henkhooft/SS3D?color=green&label=Pull%20Requests%20%28Open%29&logo=github&style=plastic" alt="Open Pull Requests" align="right">](https://github.com/henkhooft/SS3D/pulls)Code changes should align with the relevant design doc where one exists, or note the deviation in the PR.[<img src="https://img.shields.io/github/issues-pr-closed-raw/henkhooft/SS3D?color=red&label=Pull%20Requests%20%28Closed%29&logo=github&style=plastic" alt="Closed Pull Requests" align="right">](https://github.com/henkhooft/SS3D/pulls?q=is%3Apr+is%3Aclosed)

[<img src="Documents/Images/github.png" alt="github" width="150" align="left">](https://github.com/henkhooft/SS3D/issues)

GitHub hosts this fork's [issues](https://github.com/henkhooft/SS3D/issues) and [discussions](https://github.com/henkhooft/SS3D/discussions).

<br>

<h1 align="center">EDITOR TOOLS</h1>

SS3D adds a few custom tools to Unity's main toolbar to speed up day-to-day development:

- **Scene Switcher** *(left side)* — quickly load any scene listed in the `Scene` class.
- **Launcher** *(right side)* — toggle the launcher on or off, mirroring the Force Launcher option in the Application Settings.
- **Network Settings** *(right side)* — view and change the network type and server port, mirroring the Network Settings.

These are built on Unity's native main toolbar API. If you don't see them after opening the project, **right-click the main toolbar and enable them under _Tools_** — Unity hides custom toolbar elements until each user enables them the first time.

<br>

<h1 align="center">LICENSING</h1>

All **CODE** falls under the **[MIT](Documents/LICENSE-CODE.md)** license.

All **ASSETS** fall under the **[CC BY-NC-SA 4.0](Documents/LICENSE-ASSETS.md)** license.
