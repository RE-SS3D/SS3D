# How To Contribute To RE:SS3D

This document outlines the guidelines for contributing to the RE:SS3D project.
If you have any questions, ask a centcom member on our [Discord Server](https://discord.gg/Z3sPhyS).

First, check out https://ss3d.space/contribute/, which has a breakdown of the different types of contributions as well as asset kits, guides, and todo boards for each.

The game itself is made in Unity and written in C#, check out our  [code (C#) style guide](Documents/C_SHARP.md). We use Mirror for networking and there is a helpful document explaining it's use with Unity [here](https://mirror-networking.com/docs/).

## Setting Up The Project

1. Clone this (SS3D) repository.
2. [Download Unity 2019.3.12 or newer](https://unity3d.com/get-unity/download/archive) and open the folder containing the project in Unity.

## Project Maintainers:

The set of people responsible for making sure the contribution cycle runs smoothly and that the contributions made to the project are wanted, correct, and functional. On the Github these people are labeled "members", and on the Discord they are known as "Centcom".

The project maintainers have the following goals:

* To prioritize issues and pull requests so the most important changes get focused on and added to the project first.
* To make sure no contributions, however minor, don't get overlooked. Despite some contributions having greater priority, all contributions are created equal.
* To verify that contributions comply with our standards, will not break the project, and are wanted, before they are accepted into the project.

## Using Github

### Forking The Repository:

To start contributing via GitHub, first you should fork this GitHub repository, using the 'Fork' button in the upper right corner of this page. Naturally, this requires a GitHub account. You will commit your changes to your fork and then make a pull request to merge it into our repository. Learn more about pull requests [here](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/about-comparing-branches-in-pull-requests).

Over time your fork will become out-dated as the main project's repository continues to be added upon. GitHub has made a [pretty clear guide](https://help.github.com/articles/syncing-a-fork/) on how to sync your fork, to keep it up to date with the SS3D repository.

### Reporting Bugs:

Before reporting a bug, please check the [our issues](https://github.com/RE-SS3D/SS3D/issues) to see if the bug has already reported. If you are unfamiliar with issues, GitHub has a helpful guide [here](https://guides.github.com/features/issues/). Use the template when creating the issue, as the information it requests helps you help us help you more efficiently.

Explain the problem clearly and include any additional details to help maintainers reproduce the problem:

* **Use a clear and descriptive title** for the issue to identify the problem.
* **Describe the steps to reproduce the problem** as specifically and detailed as possible.
* **Describe the behavior you observed** and point out the problem with that behavior.
* **Describe the expected behavior** that should happen instead.
* **Include pictures/videos** to show the steps taken to demonstrate the problem.
* **Include your game/client version and OS** to help track if the problem is version specific.
* **If you're reporting the client crashed**, include a crash report with the error log. It's location depends on your operating system, so follow [this official guide](https://docs.unity3d.com/Manual/LogFiles.html).
* **If the problem isn't triggered by a specific action**, describe what you were doing before the problem happened.

### Pull Requests:

Pull requests allow the maintainers to review any changes and verify they are wanted and don't break anything in the existing project.

* Pull requests should merge into the *release* branch.
* The title and description should be clear and concise.
* If the PR is attempting fix an issue, reference the issue number in the PR description ("Fixes #number").
* Include pictures/videos in your pull request whenever possible.

### Git Clients:

To get a hold of the project, you need a git client. Git is the software that manages the source. GitHub is the website that we use to host it. If you are new, [GitHub Desktop](https://desktop.github.com/) is easiest for beginners.

### Git Commit Messages:

* Use the present imperative tense ("Add feature" not "Added feature", "Move cursor to..." not "Moves cursor to...").
* Limit the first line to 72 characters or less.
* Reference issues and pull requests liberally after the first line.
