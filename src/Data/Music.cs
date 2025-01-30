using Ball;

public static class Music
{
    public readonly record struct Song(int PathID, int NameID);

    public static Song[] Songs =
    {
        new Song(Stores.TextStorage.GetID(Content.Music.BreakGlass), Stores.TextStorage.GetID("Break Glass")),
        new Song(Stores.TextStorage.GetID(Content.Music.Millionaire), Stores.TextStorage.GetID("Millionaire")),
        new Song(Stores.TextStorage.GetID(Content.Music.Gachapon), Stores.TextStorage.GetID("Gachapon")),
    };
}