# Contributing to Space Station 3D

This document is a set of guidelines for contributing to the project.
Use your best judgment if in doubt, or ask on the [Discord Server](https://discord.gg/Z3sPhyS).

## Code Maintainers

A set of people are responsible for making sure thecontribution cycle runs smoothly and that the contributions made to project are wanted, correct, and functional.
These people are refered to as code maintainers.

The project maintainers have the follow goals:

* To prioritize issues and pull requests so the most urgent and most wanted changes gets into the build quickly.
* To make sure even minor contributions don't get overlooked. All contributions are created equal.
* Make sure the contributions comply with our standards, will not break the project, and are wanted, before they are merged.

## How To Contribute

There many things that you can do to help out, regardless if you're a maintainer, a visitor, a coder, or a player. At the most basic, reporting bugs is essential to making everything run flawlessly. People with skills and time to spare can help out by contributing work. Any help is appreciated.

### Code or Asset Contributions

Making an actual change in the project is really admirable and also a simple but complicated process. To get started, choose a task on the [implementation board](https://github.com/RE-SS3D/SS3D/projects/2), hop on over to the discord, and request to be assigned. For asset contributions you might also want to check the [model list](https://trello.com/b/z0H4ci3u/ss3d-model-list-mkiv).

Regardless of kind of asset you produce remember to head over to the [Style guide section](#style-guides) to make sure your contribution fits in with everyone else's.

### Reporting Bugs

Before creating a bug report, please check the issues in case the bug is already reported.
They are tracked as [GitHub issues](https://guides.github.com/features/issues/).
Use the template when creating the issue, as the information it asks for helps you help us help you more efficiently.

Explain the problem clearly and include additional details to help maintainers reproduce the problem:

* **Use a clear and descriptive title** for the issue to identify the problem.
* **Describe the steps which reproduce the problem** in as many details as possible. Be specific as any detail might matter.
* **Describe the behavior you observed** and point out what exactly is the problem with that behavior.
* **Describe what you expected to happen instead**
* **Include screenshots and/or animated GIFs** which show you following the steps and demonstrate the problem.
* **If you're reporting that the client crashed**, include a crash report with the error log. It's location depends on your operating system, so follow [this official guide](https://docs.unity3d.com/Manual/LogFiles.html).
* **If the problem wasn't triggered by a specific action**, describe what you were doing before the problem happened.
* **Include which version of the client are you running?** The version number should be written on the first screen when you launch the game.

### Pull Requests

Pull requests allow the maintainers to verify that any changes are wanted and don't break the existing code.

* Pull requests should merge into the *develop* branch
* Do not include issue numbers in the PR title
* Include screenshots and/or animated GIFs in your pull request whenever possible.

## Style guides

Follow these guidelines for good luck:

**The art style** of all assets should adhere to the **[Art Style Guide](StyleGuides/ART.md)**. The style guide was created by *beep.

**All C# code** should follow the **[C# Style Guide](StyleGuides/C_SHARP.md)**. The style is open to debate, especially when it is about clarity or readability, but anything that comes down purely to personal preference should fall back on official style guides or community convention.

### Git Commit Messages

* These are just good habits. Pull requests take priority if you want to save your energy.
* Use the present imperative tense ("Add feature" not "Added feature", "Move cursor to..." not "Moves cursor to...")
* Limit the first line to 72 characters or less
* Reference issues and pull requests liberally after the first line
* Consider starting the commit message with an applicable emoji:
  * :art: `:art:` when improving the format/structure of the code, :racehorse: `:racehorse:` when improving performance, etc.
