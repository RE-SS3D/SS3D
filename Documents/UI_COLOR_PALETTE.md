# UI Color Palette

SS3D keeps shared UI colors in `Assets/Scripts/SS3D/Data/PaletteColors.cs`.
Use this palette instead of hard-coded color literals when a color is shared by more than one UI component.

## Code usage

```csharp
using SS3D.Data;
using UnityEngine.UI;

public sealed class ReadyStateView : MonoBehaviour
{
    [SerializeField] private Image _background;

    public void SetReady(bool ready)
    {
        _background.color = ready
            ? PaletteColors.Get(PaletteColor.LightBlue)
            : PaletteColors.Get(PaletteColor.UiOverlay);
    }
}
```

Existing direct static colors still work:

```csharp
_label.color = PaletteColors.TextPrimary;
_button.image.color = PaletteColors.UiOverlayHighlighted;
```

## Choosing colors

- `TextPrimary` and `TextDisabled` are for text states.
- `UiOverlay`, `UiOverlayHighlighted`, and `UiOverlayDisabled` match the existing generic button asset.
- `LightGreen`, `NormalGreen`, `LightBlue`, and `LightRed` are gameplay/UI accent colors.
- `Log*` colors mirror the existing log color set so UI and logs can share the same semantic colors when needed.

## Adding colors

1. Add a new key to the `PaletteColor` enum.
2. Add a static `Color` value in `PaletteColors`.
3. Add it to the `_colors` array so editor/debug tools can enumerate it.

Prefer `Color32` for byte-based RGB values, for example `new Color32(80, 115, 216, 255)`. Unity's `Color` constructor expects normalized `0f..1f` channel values.
