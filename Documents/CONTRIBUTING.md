# How To Contribute To RE:SS3D

This document outlines the guidelines for contributing to the RE:SS3D project. If you have any questions, feel free to ask on out [discussions page](https://github.com/RE-SS3D/SS3D/discussions) or [discord server](https://discord.gg/Z3sPhyS).

Also, check out https://ss3d.space/contribute/, which has a breakdown of the different contributions types as well as links for asset kits, guides, and to-do boards for each type.

The game itself is made in Unity and written in C#, check out our  [code (C#) style guide](C_SHARP.md). We use Mirror for networking and there is a helpful document explaining its use with Unity [here](https://mirror-networking.com/docs/).

## Setting Up & Running The Project

### Downloading Unity:

Option A.
1. Clone this (SS3D) repository.
2. Download [Unity Hub](https://unity3d.com/get-unity/download).
3. Download the current Unity version that the project tells you.

Option B.
1. Clone this (SS3D) repository.
2. [Download Unity 2019.4.30f1 or newer](https://unity3d.com/get-unity/download/archive) and open the folder containing the project in Unity.

### Setup:

**[[YouTube Tutorial - Unity] RE:SS3D Load Scenes](https://www.youtube.com/watch?v=lRPIDY33ccY)**

1. Drag 'MainMenu.Scene' (in /Assets/Content/Scenes/) into the scene window. This is the only scene required to run the project, so without this scene loaded you cannot run it. Also, delete any default/untitled scenes that Unity loaded when it launched.
2. (Optional - If you plan to run multiple clients to test multiplayer) Build the project via File > Build or Build Settings.
3. Hit the play button at the top of the Unity window.

### Run:

**[[YouTube Tutorial - Unity] RE:SS3D Play Test](https://www.youtube.com/watch?v=VYnT99AHKug)**

1. Click 'Host' to take you to the server host lobby.
2. Click 'Admin' to take you to the server host settings.
3. Select and load a map.
4. Once the map is loaded, click 'Start' to start the round. Click 'Exit' to return from the settings page back to the lobby page.
5. Click 'Exit' to return from the settings page back to the lobby page.
6. Click 'Embark' to join the round.
7. (Optional - If you are testing with multiple clients) Open the built client from step 2 of the setup process. Click 'Join' to join the lobby of the locally hosted server. Click 'Embark' to join the round.

## Project Maintainers

The set of people responsible for making sure the contribution cycle runs smoothly and that the changes made to the project are wanted, correct, and functional. On the Github these people are labeled "members", and on the Discord they are known as "Centcom".

The project maintainers have the following goals:

* To prioritize issues and pull requests so the most important changes get focused on and added to the project first.
* To make sure no contributions, however minor, don't get overlooked. Despite some contributions having greater priority, all contributions are created equal.
* To verify that contributions comply with our standards, will not break the project, and are wanted, before they are accepted into the project.

## Using Github

### Forking The Repository:

To start contributing via GitHub, first you should fork this GitHub repository, using the 'Fork' button in the upper right corner of this page. Naturally, this requires a GitHub account. You will commit your changes to your fork and then make a pull request to merge it into our repository. Learn more about pull requests [here](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/about-comparing-branches-in-pull-requests).

Over time your fork will become out-dated as the main project's repository continues to be added upon. GitHub has made a [pretty clear guide](https://help.github.com/articles/syncing-a-fork/) on how to sync your fork, to keep it up to date with the SS3D repository.

### Game Testing & Reporting Bugs:

Before reporting a bug, please check the [our issues](https://github.com/RE-SS3D/SS3D/issues) to see if the bug has already reported. Bugs are categorized into 3 groups:

**[Bug (Unconfirmed)](https://github.com/RE-SS3D/SS3D/labels/Type%3A%20Bug%20%28Unconfirmed%29)** - Some kind of error reported by an individual but needs confirmation.
**[Bug (Confirmed)](https://github.com/RE-SS3D/SS3D/labels/Type%3A%20Bug%20%28Confirmed%29)** - Bugs that have been confirmed by another contributor.
**[Bug (Can't Confirm)](https://github.com/RE-SS3D/SS3D/labels/Type%3A%20Bug%20%28Can%27t%20Confirm%29)** - Bugs that have tried to be replicated but were unsuccessful.

If you are unfamiliar with issues, GitHub has a helpful guide [here](https://guides.github.com/features/issues/). Use one of the templates when creating your issue, as the information it requests helps you help us help you more efficiently.

Explain the problem clearly and include any additional details to help maintainers reproduce the problem:

* **Use a clear and descriptive title** for the issue to identify the problem.
* **Describe the steps to reproduce the problem** as specifically and detailed as possible.
* **Describe the behavior you observed** and point out the problem with that behavior.
* **Describe the expected behavior** that should happen instead.
* **Include pictures/videos** to show the steps taken to demonstrate the problem.
* **Include your game version and OS** to help track if the problem is version specific.
* **Include your game settings** to help track if the problem is settings specific.
* **If you're reporting a game crash**, include a crash report with the error log. It's location depends on your operating system, so follow [this official guide](https://docs.unity3d.com/Manual/LogFiles.html).
* **If the problem isn't triggered by a specific action**, describe what you were doing before the problem happened.

### Pull Requests:

Pull requests allow the maintainers to review any changes and verify they are wanted and don't break anything in the existing project. Similarly to issues, you should use the template when making a new PR and provide as much detail as possible.

* Pull requests should merge into the *master* branch.
* The title and description should be clear and concise.
* If the PR is attempting to fix an issue, reference the issue number in the PR description ("Fixes #number").
* Include pictures/videos in your pull request whenever possible.

### Git Clients:

To get a hold of the project, you need a git client. Git is the software that manages the source. GitHub is the website that we use to host it. If you are new, [GitHub Desktop](https://desktop.github.com/) is easiest for beginners.

### Git Commit Messages:

* Use the present imperative tense ("Add feature" not "Added feature", "Move cursor to..." not "Moves cursor to...").
* Limit the first line to 72 characters or less.
* Reference issues and pull requests liberally after the first line.

### Naming Files and Folders:

When (re)naming files and folders always do so using ['PascalCase'](https://techterms.com/definition/pascalcase), this makes naming consistant which helps with organization and clarity.

Ex. "LockerDoorAnimationController.file" would be correct,
not "Locker Door Animation Controller.file",
not "lockerdoor animation controller.file",
not "AnimationController - LockerDoor.file".
