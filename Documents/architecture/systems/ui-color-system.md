# UI Color System

This system provides a centralized way to manage and apply colors across the SS3D user interface, ensuring visual consistency and allowing for easy global changes.

## Components

### 1. UIColorSettings (ScriptableObject)
The `UIColorSettings` asset holds all the color definitions. 
- **Location:** Create an instance via `Create > SS3D > UI > Color Settings`.
- **Usage:** Modify the colors in the Inspector to update all linked UI elements.

### 2. UIColorApplier (Component)
A helper script that links a UI component (Image or TextMeshPro) to a specific color in the `UIColorSettings`.
- **How to use:**
    1. Add the `UIColorApplier` component to a GameObject with an `Image` or `TMP_Text`.
    2. Drag the `UIColorSettings` asset into the "Settings" field.
    3. Select the desired "Color Type" from the dropdown.

## Accessing Colors via Code

To access colors in your own scripts, reference the `UIColorSettings` asset:

```csharp
public class MyUIElement : MonoBehaviour {
    [SerializeField] private UIColorSettings _colorSettings;

    public void UpdateStyle() {
        Color accent = _colorSettings.GetColor(UIColorSettings.UIColorType.Accent);
        // Apply color...
    }
}
```

## Benefits
- **Consistency:** No more guessing hex codes.
- **Efficiency:** Change the "Accent" color once, and every button/icon using it updates instantly.
- **Designer Friendly:** Artists can tweak the UI palette without touching code.
