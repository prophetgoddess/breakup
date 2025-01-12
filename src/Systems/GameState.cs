using System.Numerics;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Math;
using SDL3;
using Filter = MoonTools.ECS.Filter;

namespace Ball;

public class GameStateManager : MoonTools.ECS.System
{
    Filter DestroyFilter;
    Filter DontDestroyFilter;
    Filter BallFilter;

    BallSpawner BallSpawner;
    SaveGame SaveGame;
    MainMenuSpawner MainMenuSpawner;
    SettingsMenuSpawner SettingsMenuSpawner;
    PauseMenuSpawner PauseMenuSpawner;
    GameSpawner GameSpawner;
    Game Game;

    float holdActivationTime = 0.5f;
    float holdTime = 1f;

    float holdActivationTimer = 0f;
    float holdTimer = 0f;

    public GameStateManager(World world, Game game) : base(world)
    {
        Game = game;
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Exclude<DontDestroyOnNextTransition>().Build();
        DontDestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Include<DontDestroyOnNextTransition>().Build();

        BallFilter = FilterBuilder.Include<CanDealDamageToBlock>().Include<HasGravity>().Include<CanBeHit>().Build();

        BallSpawner = new BallSpawner(world);
        SaveGame = new SaveGame(world);
        MainMenuSpawner = new MainMenuSpawner(world);
        SettingsMenuSpawner = new SettingsMenuSpawner(world);
        PauseMenuSpawner = new PauseMenuSpawner(world);
        GameSpawner = new GameSpawner(world);
    }

    public static string GetFormattedNumber(int amount, int length = 8)
    {
        return amount >= 0
            ? amount.ToString($"D{length}")
            : amount.ToString($"D{length - 1}");
    }

    public override void Update(TimeSpan delta)
    {
        var inputState = GetSingleton<InputState>();
        var dt = (float)delta.TotalSeconds;

        if (inputState.Start.IsPressed)
        {
            if (Some<MainMenu>())
            {
                GameSpawner.StartGame();
            }
            else if (Some<Pause>() && !Some<Selected>() && !Some<MainMenu>())
            {
                foreach (var entity in DestroyFilter.Entities)
                {
                    Destroy(entity);
                }
                foreach (var entity in DontDestroyFilter.Entities)
                {
                    Remove<DontDestroyOnNextTransition>(entity);
                }

            }
            else if (!Some<Pause>() && !Some<Selected>() && !Some<MainMenu>())
            {
                PauseMenuSpawner.OpenPauseMenu();
            }
        }

        if (inputState.Cancel.IsDown && (Some<MainMenu>() || Some<Pause>()) && !Some<UpgradeOption>())
        {
            if (holdActivationTimer < holdActivationTime)
                holdActivationTimer += dt;
            else
            {
                holdTimer += dt;

                if (!Some<QuitMeter>())
                {
                    var quitMeter = CreateEntity();
                    Set(quitMeter, new QuitMeter());
                    Set(quitMeter, new Model(Content.Models.Square.ID));
                    Set(quitMeter, new Position(new Vector2(
                            Dimensions.GameWidth * 0.5f,
                            Dimensions.GameHeight * 0.1f
                        )));
                    Set(quitMeter, new Orientation(0f));
                    Set(quitMeter, new Velocity(Vector2.Zero));
                    Set(quitMeter, new Scale(new Vector2(0f, 4f)));
                    Set(quitMeter, new Power(0f, 0.01f, 2f));
                    Set(quitMeter, new DestroyOnStateTransition());
                    Set(quitMeter, new Highlight());
                    Set(quitMeter, new KeepOpacityWhenPaused());
                    Set(quitMeter, new Depth(0.01f));
                    if (Some<Player>())
                    {
                        Set(quitMeter, new FollowsCamera(Dimensions.GameHeight * 0.65f));
                    }
                    else
                    {
                        Set(quitMeter, new UI());
                        Set(quitMeter, new Position(new Vector2(
                            Dimensions.UIWidth * 0.5f,
                            Dimensions.UIHeight * 0.525f
                        )));
                    }
                }
                else
                {
                    var quitMeter = GetSingletonEntity<QuitMeter>();
                    var t = Easing.InOutQuad(holdTimer / holdTime);
                    if (Some<Player>())
                        Set(quitMeter, new Scale(new Vector2(t * Dimensions.GameWidth, 4f)));
                    else
                        Set(quitMeter, new Scale(new Vector2(t * Dimensions.UIWidth, 4f)));
                }
            }

            if (holdTimer >= holdTime)
            {
                holdActivationTimer = 0f;
                holdTimer = 0f;
                if (Some<MainMenu>())
                {
                    Game.Quit();
                }
                else if (Some<Pause>() && !Some<MainMenu>())
                {
                    MainMenuSpawner.OpenMainMenu();
                }
                if (Some<QuitMeter>())
                {
                    Destroy(GetSingletonEntity<QuitMeter>());
                }
            }
        }
        else
        {

            holdActivationTimer = 0f;
            holdTimer = 0f;
        }

        if (Some<Lives>() && GetSingleton<Lives>().Value <= 0)
        {
            if (Some<ReviveWithOneHealth>() && GetSingleton<ReviveWithOneHealth>().Active)
            {
                var revive = GetSingletonEntity<ReviveWithOneHealth>();
                Set(revive, new ReviveWithOneHealth(false));
                var l = GetSingletonEntity<Lives>();
                Set(l, new Lives(1));
            }
            else
            {
                MainMenuSpawner.OpenMainMenu();
            }
        }
        else if (Some<Lives>() && GetSingleton<Lives>().Value >= 3 && Some<ReviveWithOneHealth>())
        {
            var revive = GetSingletonEntity<ReviveWithOneHealth>();
            Set(revive, new ReviveWithOneHealth(true));
        }



        if (!Some<Gems>())
            return;

        var gemsEntity = GetSingletonEntity<Gems>();
        var gems = Get<Gems>(gemsEntity);

        Set(gemsEntity,
        new Text(
            Fonts.BodyFont,
            Fonts.BodySize,
            Stores.TextStorage.GetID(GetFormattedNumber(gems.Total)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (!Some<Score>())
            return;

        var newScore = (int)GetSingleton<CameraPosition>().Y + gems.Total;
        var scoreEntity = GetSingletonEntity<Score>();
        var score = Get<Score>(scoreEntity);
        var highScoreEntity = GetSingletonEntity<HighScore>();
        var highScore = Get<HighScore>(highScoreEntity).Value;

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
        }

        Set(highScoreEntity,
         new Text(
            Fonts.BodyFont,
            Fonts.BodySize,
            Stores.TextStorage.GetID(GetFormattedNumber(highScore)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (!Some<Lives>())
            return;

        var livesEntity = GetSingletonEntity<Lives>();
        var lives = Get<Lives>(livesEntity);

        if (BallFilter.Empty)
        {
            Set(livesEntity, new Lives(lives.Value - 1));
            var ball = BallSpawner.SpawnBall(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.5f
            ));

            Relate(ball, GetSingletonEntity<Player>(), new HeldBy(new Vector2(0f, -32.0f)));
            Set(ball, new Velocity(Vector2.Zero));
            Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.fail)));
        }

        Set(livesEntity,
        new Text(
            Fonts.BodyFont,
            Fonts.MidSize,
            Stores.TextStorage.GetID(GetFormattedNumber(lives.Value, 2)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (!Some<Level>())
            return;

        var levelEntity = GetSingletonEntity<Level>();
        var level = Get<Level>(levelEntity);

        Set(levelEntity,
        new Text(
            Fonts.BodyFont,
            Fonts.MidSize,
            Stores.TextStorage.GetID(GetFormattedNumber(level.Value, 2)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (!Some<CanDealDamageToBlock>())
            return;

        var ballEntity = GetSingletonEntity<CanDealDamageToBlock>();
        Set(ballEntity, new CanDealDamageToBlock(level.Value + 1));

        var xpEntity = GetSingletonEntity<XP>();
        var xp = Get<XP>(xpEntity);
        Set(xpEntity,
        new Text(
            Fonts.BodyFont,
            Fonts.MidSize,
            Stores.TextStorage.GetID(GetFormattedNumber(xp.Target - xp.Current, 4)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

    }
}