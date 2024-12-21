using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class MainMenuSpawner : Manipulator
{
    Filter DestroyFilter;
    Filter HideFilter;
    Filter PauseFilter;
    Filter BallFilter;
    SaveGame SaveGame;

    public MainMenuSpawner(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Build();
        HideFilter = FilterBuilder.Include<HideOnMainMenu>().Build();
        PauseFilter = FilterBuilder.Include<Pause>().Build();
        BallFilter = FilterBuilder.Include<CanDealDamageToBlock>().Include<HasGravity>().Include<CanBeHit>().Build();
        SaveGame = new SaveGame(world);

    }

    public void CloseMainMenu()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
        }
    }

    public void OpenMainMenu()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            if (Has<Score>(entity))
            {
                var s = CreateEntity();
                Set(s, new Score(Get<Score>(entity).Current));
                Set(s, new DestroyOnStateTransition());
            }
            if (Has<HighScore>(entity))
            {
                var hs = CreateEntity();
                Set(hs, new HighScore(Get<HighScore>(entity).Value));
                Set(hs, new DestroyOnStateTransition());
            }
            Destroy(entity);
        }

        if (!Some<HighScore>())
        {
            var saveData = SaveGame.Load();
            var hs = CreateEntity();
            Set(hs, new HighScore(saveData.HighScore));
            Set(hs, new DestroyOnStateTransition());
        }

        foreach (var entity in HideFilter.Entities)
        {
            Set(entity, new Invisible());
        }

        var gameTitle = CreateEntity();
        Set(gameTitle, new Position(new Vector2(UILayoutConstants.TitleX, UILayoutConstants.TitleY)));
        Set(gameTitle,
         new Text(
            Fonts.HeaderFont,
            Fonts.TitleSize,
            Stores.TextStorage.GetID("break.up"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        Set(gameTitle, new UI());
        Set(gameTitle, new DestroyOnStateTransition());
        Set(gameTitle, new MainMenu());

        var prompt = CreateEntity();
        Set(prompt, new Position(new Vector2(UILayoutConstants.PromptX, UILayoutConstants.PromptY)));
        Set(prompt,
         new Text(
            Fonts.BodyFont,
            Fonts.PromptSize,
            Stores.TextStorage.GetID("press start"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Baseline));
        Set(prompt, new UI());
        Set(prompt, new DestroyOnStateTransition());
        Set(prompt, new MainMenu());

        var scores = CreateEntity();
        Set(scores, new Position(new Vector2(UILayoutConstants.PromptX, 10)));
        Set(scores,
         new Text(
            Fonts.BodyFont,
            Fonts.PromptSize,
            Stores.TextStorage.GetID($"last score: {(Some<Score>() ? GetSingleton<Score>().Current.ToString() : "NONE")} / high score: {(Some<HighScore>() ? GetSingleton<HighScore>().Value.ToString() : "NONE")}"),
            MoonWorks.Graphics.Font.HorizontalAlignment.Center,
            MoonWorks.Graphics.Font.VerticalAlignment.Top));
        Set(scores, new UI());
        Set(scores, new DestroyOnStateTransition());
        Set(scores, new MainMenu());

        var leftBound = CreateEntity();
        Set(leftBound, new Position(new Vector2(-8, 0)));
        Set(leftBound, new BoundingBox(0, 0, 16, 2000));
        Set(leftBound, new SolidCollision());
        Set(leftBound, new DestroyOnStateTransition());
        Set(leftBound, new UI());

        var rightBound = CreateEntity();
        Set(rightBound, new Position(new Vector2(Dimensions.UIWidth + 8, 0)));
        Set(rightBound, new BoundingBox(0, 0, 16, 2000));
        Set(rightBound, new SolidCollision());
        Set(rightBound, new DestroyOnStateTransition());
        Set(rightBound, new UI());

        var bottomBound = CreateEntity();
        Set(bottomBound, new Position(new Vector2(Dimensions.UIWidth * 0.5f, Dimensions.UIHeight + 8)));
        Set(bottomBound, new BoundingBox(0, 0, Dimensions.UIWidth, 16));
        Set(bottomBound, new SolidCollision());
        Set(bottomBound, new DestroyOnStateTransition());
        Set(bottomBound, new UI());

        var topBound = CreateEntity();
        Set(topBound, new Position(new Vector2(Dimensions.UIWidth * 0.5f, 0f)));
        Set(topBound, new BoundingBox(0, 0, Dimensions.UIWidth, 16));
        Set(topBound, new SolidCollision());
        Set(topBound, new DestroyOnStateTransition());
        Set(topBound, new UI());

    }
}