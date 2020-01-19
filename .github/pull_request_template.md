**Fixes**: #IssueNumber(s) (Optional)

## Purpose
Description for what this PR is meant to achieve. Can also explain how in depth the feature/fix is and what it still lacks.
Example:
*This PR aims to add a basic implementation for internal organ simulation for living creatures. It implements body organs as a concept, and adds methods for interactions between organs. Additionally it ties the organs into the body damage and limb detachment systems. Since we have eating and blood features, only the stomach and heart organs have been implemented. 
This PR does not implement lungs, or brains because we don't have atmospherics or soul/conciousness mechanics.*

## Changes to existing binary files (Optional if none)
A list of every prefab and scene file that was changed and why/how they were changed.
Example:
- **Human.prefab** was changed to add an organs list to the Body component
- **TorsoBodypart.prefab** was changed to add support for dismembered torsos still containing organs
- **Monkey.prefab** was changed to add organ list to the Body component
- **MainScene.unity** was changed to move the monkey in the scene away from the alcohol, as it can poison itself too early in the game.

## Technical implementation notes
A more technical description of how the PR does what it does. This is mainly to help provide context for others when reviewing the changes.
Example:
*Organs and their associated features are mostly data in nature. Thus the general handling happens in the Body component. The possible internal organ types listed in the `InternalOrganType.cs` enum. Specific organs are arranged by creature and grouped by the organ type. Functionality is mostly handled through interfaces. There is general support of a physical representation of an organ in the game world (like during surgery or mutilation), but it has not been implemented at this time.*

## Known issues (Optional if none)
A list of things not working exactly as intended. Either because some other system is unfinished or due to being beyond the scope of this PR. In general, PRs with bugs directly related to the feature being introduced should not be submitted.
Example:
- Not all food has any effect on the stomach organ, as some foods are lacking nutritional/composition data.
- This basic implementation does not support multiple organs of the same type in a body. We still need to decide how that would work.
