using MoonWorks.Graphics;

namespace Ball;

public readonly record struct Palette(Color Background, Color Foreground, Color Highlight);

public static class Palettes
{
    public static Palette DefaultLight = new Palette
    (
        Color.White,
        Color.DarkGray,
        Color.DodgerBlue
    );

    public static Palette DefaultDark = new Palette(
        Color.Black,
        Color.White,
        Color.LimeGreen
    );

    public static Palette MillenialApartment = new Palette(
        Color.Azure,
        new Color(172, 189, 186),
        new Color(165, 153, 181)
    );
}