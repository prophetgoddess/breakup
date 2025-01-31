using MoonWorks.Graphics;

namespace Ball;

public readonly record struct Palette(int NameID, Color Background, Color Foreground, Color Highlight);

public static class ColorPalettes
{
    public static Palette[] Palettes =
    {
        new Palette(
                Stores.TextStorage.GetID("Default Purple"),
                Color.Azure,
                new Color(172, 189, 186),
                new Color(165, 153, 181)
            ),
        new Palette(
            Stores.TextStorage.GetID("BurntOffering"),
            new Color(7, 16, 19),
            new Color(255, 255, 255),
            new Color(235, 81, 96)
        ),
    };

}