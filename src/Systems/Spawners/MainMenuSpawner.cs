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
    MarqueeSpawner MarqueeSpawner;

    public MainMenuSpawner(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Build();
        HideFilter = FilterBuilder.Include<HideOnMainMenu>().Build();
        PauseFilter = FilterBuilder.Include<Pause>().Build();
        BallFilter = FilterBuilder.Include<CanDealDamageToBlock>().Include<HasGravity>().Include<CanBeHit>().Build();
        MarqueeSpawner = new MarqueeSpawner(world);
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

        Set(MarqueeSpawner.SpawnMarquee("break.up", Fonts.HeaderFont, Fonts.TitleSize, 3, 100f, UILayoutConstants.TitleY), new MainMenu());

        MarqueeSpawner.SpawnMarquee("press start", Fonts.BodyFont, Fonts.PromptSize, 7.0f, -100f, UILayoutConstants.PromptY);

        MarqueeSpawner.SpawnMarquee(
            $"last {(Some<Score>() ? GetSingleton<Score>().Current.ToString() : "NONE")} - best {(Some<HighScore>() ? GetSingleton<HighScore>().Value.ToString() : "NONE")}",
             Fonts.BodyFont, Fonts.PromptSize, 4.0f, -100f, 20);


        MarqueeSpawner.SpawnMarquee($"press X for settings - hold to quit", Fonts.BodyFont, Fonts.PromptSize, 3.0f, 100f, Dimensions.UIHeight - 20.0f);


    }
}