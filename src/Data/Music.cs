using Ball;

public readonly record struct Song(int PathID, int NameID, bool unlocked);

public static class Music
{
    public static Song[] Songs =
    {
        new Song(Stores.TextStorage.GetID(Content.Music.BreakGlass), Stores.TextStorage.GetID("Break Glass"), true),
        new Song(Stores.TextStorage.GetID(Content.Music.Millionaire), Stores.TextStorage.GetID("Millionaire"), false),
        new Song(Stores.TextStorage.GetID(Content.Music.Gachapon), Stores.TextStorage.GetID("Gachapon"), false),
    };

    private static bool SongUnlocksLoaded = false;
    public static void LoadSongUnlocks(SaveData data)
    {
        if (!SongUnlocksLoaded)
        {
            SongUnlocksLoaded = true;

            for (int i = 0; i < Songs.Length; i++)
            {
                var song = Songs[i];
                Songs[i] = new Song(song.PathID, song.NameID, data.SongUnlocks[i]);
            }

        }
    }

    private static bool[] UnlockedSongs = null;

    public static int UnlockSong()
    {
        if (Songs.All(s => s.unlocked))
        {
            return -1;
        }

        var index = Rando.Int(0, Songs.Length);

        while (Songs[index].unlocked)
        {
            index = Rando.Int(0, Songs.Length);
        }

        var song = Songs[index];

        Songs[index] = new Song(song.PathID, song.NameID, true);

        UpdateUnlocked();

        return index;
    }

    public static bool[] Unlocked()
    {
        if (UnlockedSongs == null)
        {
            UnlockedSongs = Songs.Select(s => s.unlocked).ToArray();
        }

        return UnlockedSongs;
    }

    public static void UpdateUnlocked()
    {
        UnlockedSongs = null;
    }
}