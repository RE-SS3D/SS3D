## Adding Controls (Actions)

1. Open `Engine/Input/InputActions.inputactions` in unity, and use the GUI to add your actions. They should be standard button bindings.  
Use the category that best fits your new action.  
1a. If you cannot find a category that describes your action well, then make your own.

2. Set the Interaction for the action (not the binding itself!) to be a button press, and set the press type appropiately for your use.

3. Now set the binding for your action. Use keyboard and mouse bindings only, and make sure the control scheme is set to `Keyboard+Mouse` only.

4. Press `Save Asset` in the top of the editor.

5. Done! You can now reference your keybinding using `SS3D.Input.InputHelper.inputs.mycategory.myaction`!

## Implementation Notes (IMPORTANT)

- `myaction.triggered` only runs on the frame a key is pressed and/or released, depending on your press type.
- `myaction.IsPressed()` will return true if the user is currently holding the key down.

## Technical Notes

- If you set your action to hold, triggered and acts in the exact same way as if you set the press type to Press and Release.

- If you set your action type to a vector or something similar for some reason, the min and max will always be -1 and 1 for keys, and -160 and 160 for mousewheel.

- Use `myaction.ReadValue<Vector2>()` to get a Vector2 from a value. This is inconsistent on whether this actually returns the correct values, or just a 0 value Vector2 in testing though.