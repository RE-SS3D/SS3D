# UI Color Palette

SS3D keeps shared UI colors in `Assets/Scripts/SS3D/Data/PaletteColors.cs`.
Use this palette instead of adding a new hard-coded color when the same UI color is reused in more than one place.

The palette supports two access patterns:

- direct constants, such as `PaletteColors.LightBlue`, for simple UI code;
- semantic keys, such as `PaletteColor.LightBlue`, for editor tooling, dropdowns, validation, or code that needs to enumerate available colors.

## Direct code usage

```csharp
using SS3D.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class ReadyStateView : MonoBehaviour
{
    [SerializeField] private Image _background;

    public void SetReady(bool ready)
    {
        _background.color = ready
            ? PaletteColors.LightBlue
            : PaletteColors.ButtonBackground;
    }
}
```

## Semantic lookup usage

```csharp
using SS3D.Data;
using UnityEngine;

public sealed class PalettePreview : MonoBehaviour
{
    public Color GetPreviewColor(PaletteColor colorKey)
    {
        return PaletteColors.GetColor(colorKey);
    }
}
```

Use `PaletteColors.All` when a tool or inspector needs to list every shared color with display names and usage notes.
Use `TryGetColor` or `TryGetDefinition` when reading saved or external palette keys that might be invalid.

## Available shared colors

- `TextPrimary`, `TextInverted`, and `TextDisabled` match the text states already used by shared button styles.
- `ButtonBackground`, `ButtonBackgroundHighlighted`, `ButtonBackgroundPressed`, and `ButtonBackgroundDisabled` match the existing generic button style values.
- `LightGreen`, `NormalGreen`, `LightBlue`, and `LightRed` are the existing reusable accent colors.

## Adding colors

Add a color here only when it is genuinely shared by UI code or assets. Colors that belong to one subsystem, such as logging colors, should stay with that subsystem instead of growing the UI palette.

When adding a shared color:

1. Add a semantic key to `PaletteColor`.
2. Add a direct `PaletteColors` field for existing direct-call usage.
3. Add a matching `PaletteColorDefinition` entry, including a display name and intended usage.

Prefer `Color32` for byte-based RGB values, for example `new Color32(80, 115, 216, 255)`. Unity's `Color` constructor expects normalized `0f..1f` channel values.
