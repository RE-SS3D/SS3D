# Contributing to Space Station 3D

This document is a set of guidelines for contributing to the project.
These are just guidelines, but they are here to ensure an efficient workflow.
Use your best judgment if in doubt, or ask on the [Discord Server](https://discord.gg/Z3sPhyS).

## Code Maintainers

A set of people are responsible for making sure thecontribution cycle runs smoothly and that the contributions made to project are wanted, correct, and functional.
These people are refered to as code maintainers.

The project maintainers have the follow goals:

* To prioritize issues and pull requests so the most urgent and most wanted changes gets into the build quickly.
* To make sure even minor contributions don't get overlooked. All contributions are created equal.
* Make sure the contributions comply with our standards, will not break the project, and are wanted, before they are merged.

## How To Contribute

There many things that you can do to help out, regardless if you're a maintainer, a visitor, a coder, or a player. At the most basic, reporting bugs is essential to making everything run flawlessly. People with some skills and time to spare can help out the community by contributing some work. Any help is appreciated.

### Reporting Bugs

Reporting bugs might be one of the easiest and most essential things you can do. This section outlines what a proper bug report looks like.
The maintainers (and other people like you) need to understand your report, be able to reproduce the behavior and find related reports easily.
If your bug report meets all three of those conditions, then we can quickly and efficiently be handled, and you have directly improved the experience for others.

Before creating a bug report, please check [this list](#before-submitting-a-bug-report) as you might find out that you don't need to create one.
When you are creating a bug report, please [include as many relevant details as possible](#how-do-i-submit-a-good-bug-report). Feel free to remove any non-applicable junk.
Use a template when creating the issue, as the information it asks for helps you help us help you more efficiently.

#### How Do I Submit A (Good) Bug Report

Bugs are tracked as [GitHub issues](https://guides.github.com/features/issues/).
Your first step in creating the issue is to use a template when creating your bug report.

Explain the problem clearly and include additional details to help maintainers reproduce the problem:

* **Use a clear and descriptive title** for the issue to identify the problem.
* **Describe the steps which reproduce the problem** in as many details as possible. Be specific as any detail might matter.
* **Describe the behavior you observed after following the steps** and point out what exactly is the problem with that behavior.
* **Explain which behavior you expected to see instead and why.**
* **Include screenshots and/or animated GIFs** which show you following the steps and demonstrate the problem.
* **If you're reporting that the client crashed**, include a crash report with the error log. It is located depending on your operating system, so follow [this official guide](https://docs.unity3d.com/Manual/LogFiles.html). 
* **If the problem wasn't triggered by a specific action**, describe what you were doing before the problem happened.
* **Include which version of the client are you running?** The version number should be written on the first screen when you launch the game.

### Code or Asset Contributions

Making an actual change in the project is really admirable and also a simple but complicated process. This section will guide you through said process.

Regardless of kind of asset you produce remember to head over to the [Style guide section](#style-guides) to make sure your contribution fits in with everyone else's.

### Pull Requests

Pull requests allow the maintainers to verify that any changes are wanted and don't break the existing code before they are merged into the latest version. Note that pull requests should merge from your own branch into this repository's *develop* branch.

Here are some simple general guidelines for pull requests:

* Fill in [the template](.github/PULL_REQUEST_TEMPLATE.md). It'll pop up when you create the Pull Request.
* Do not include issue numbers in the PR title
* Include screenshots and/or animated GIFs in your pull request whenever possible.

## Style guides

Follow these guidelines for good luck.

### Art Style

The art style of all assets should adhere to the **[Art Style Guide](StyleGuides/ART.md)**. The style guide was created by *beep.

### C# Styleguide

All C# code should follow the **[C# Style Guide](StyleGuides/C_SHARP.md)**. The style is open to debate, especially when it is about clarity or readability, but anything that comes down purely to personal preference should fall back on official style guides or community convention.

### Git Commit Messages

* These are just good habits. Pull requests take priority if you want to save your energy.
* Use the present tense ("Add feature" not "Added feature")
* Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
* Limit the first line to 72 characters or less
* Reference issues and pull requests liberally after the first line
* Consider starting the commit message with an applicable emoji:
  * :art: `:art:` when improving the format/structure of the code
  * :racehorse: `:racehorse:` when improving performance
  * :penguin: `:penguin:` when fixing something on Linux
  * :apple: `:apple:` when fixing something on macOS
  * :checkered_flag: `:checkered_flag:` when fixing something on Windows
  * :bug: `:bug:` when fixing a bug
  * :fire: `:fire:` when removing code or files
  * :green_heart: `:green_heart:` when fixing the CI build
  * :white_check_mark: `:white_check_mark:` when adding tests
  * :lock: `:lock:` when dealing with security
