using MoonWorks.Graphics;

namespace Ball;

public readonly record struct Palette(int NameID, Color Background, Color Foreground, Color Highlight, bool Unlocked);

public static class ColorPalettes
{
    public static Palette[] Palettes =
    {
        new Palette(
                Stores.TextStorage.GetID("Default Purple"),
                Color.Azure,
                new Color(172, 189, 186),
                new Color(165, 153, 181),
                true
            ),
        new Palette(
            Stores.TextStorage.GetID("Burnt Offering"),
            new Color(7, 16, 19),
            new Color(255, 255, 255),
            new Color(235, 81, 96),
            false
        ),
    };

    private static bool PaletteUnlocksLoaded = false;
    public static void LoadSongUnlocks(SaveData data)
    {
        if (!PaletteUnlocksLoaded)
        {
            PaletteUnlocksLoaded = true;

            for (int i = 0; i < Palettes.Length; i++)
            {
                var palette = Palettes[i];
                Palettes[i] = new Palette(palette.NameID, palette.Background, palette.Foreground, palette.Highlight, data.PaletteUnlocks[i]);
            }

        }
    }

    private static bool[] UnlockedPalettes = null;

    public static int Unlock()
    {
        if (Palettes.All(s => s.Unlocked))
        {
            return -1;
        }

        var index = Rando.Int(0, Palettes.Length);

        while (Palettes[index].Unlocked)
        {
            index = Rando.Int(0, Palettes.Length);
        }

        var palette = Palettes[index];

        Palettes[index] = new Palette(palette.NameID, palette.Background, palette.Foreground, palette.Highlight, true);

        UpdateUnlocked();

        return index;
    }

    public static bool[] Unlocked()
    {
        if (UnlockedPalettes == null)
        {
            UnlockedPalettes = Palettes.Select(s => s.Unlocked).ToArray();
        }

        return UnlockedPalettes;
    }

    public static void UpdateUnlocked()
    {
        UnlockedPalettes = null;
    }

}