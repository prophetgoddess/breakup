using MoonTools.ECS;

namespace Ball;

public class Scorer : Manipulator
{
    SaveGame SaveGame;
    public Scorer(World world) : base(world)
    {
        SaveGame = new SaveGame(world);
    }

    public static string GetFormattedNumber(int amount, int length = 8)
    {
        return amount >= 0
            ? amount.ToString($"D{length}")
            : amount.ToString($"D{length - 1}");
    }

    public void AddScore(int Amount)
    {
        if (!Some<Score>())
            return;

        var combo = GetSingleton<Combo>().Value;

        if (combo > 0)
            Amount *= combo;

        var scoreEntity = GetSingletonEntity<Score>();
        var score = Get<Score>(scoreEntity);

        var highScoreEntity = GetSingletonEntity<HighScore>();
        var highScore = Get<HighScore>(highScoreEntity).Value;
        var newScore = score.Current + Amount;

        Set(scoreEntity, new Score(newScore));
        Set(scoreEntity,
            new Text(
                Fonts.BodyFont,
                Fonts.BodySize,
                Stores.TextStorage.GetID(GetFormattedNumber(newScore)),
                MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (newScore > highScore)
        {
            highScore = newScore;
            Set(highScoreEntity, new HighScore(newScore));
            SaveGame.Save();
            if (!Some<SetHighScoreThisRun>())
            {
                Set(CreateEntity(), new SetHighScoreThisRun());
                Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.hiscore)));
            }

            Set(highScoreEntity,
                new Text(
                    Fonts.BodyFont,
                    Fonts.BodySize,
                    Stores.TextStorage.GetID(GetFormattedNumber(highScore)),
                    MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                    MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        }
    }
}