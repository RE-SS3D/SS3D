# UI Color Palette

SS3D keeps shared UI colors in `Assets/Scripts/SS3D/Data/PaletteColors.cs`.
Use this palette instead of adding a new hard-coded color when the same UI color is reused in more than one place.

## Usage

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

## Available shared colors

- `TextPrimary`, `TextInverted`, and `TextDisabled` match the text states used by shared button styles.
- `ButtonBackground`, `ButtonBackgroundHighlighted`, `ButtonBackgroundPressed`, and `ButtonBackgroundDisabled` match the existing generic button style values.
- `LightGreen`, `NormalGreen`, `LightBlue`, and `LightRed` are the existing reusable accent colors.

## Adding colors

Add a color here only when it is genuinely shared by UI code or assets. Colors that belong to one subsystem, such as logging colors, should stay with that subsystem.

Prefer `Color32` for byte-based RGB values, for example `new Color32(80, 115, 216, 255)`. Unity's `Color` constructor expects normalized `0f..1f` channel values.
