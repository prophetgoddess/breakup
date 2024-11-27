using System.Numerics;
using MoonTools.ECS;
using Filter = MoonTools.ECS.Filter;

namespace Ball;
public class GameState : MoonTools.ECS.System
{

    Filter DestroyFilter;
    Filter HideFilter;

    public GameState(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStartGame>().Build();
        HideFilter = FilterBuilder.Include<HideOnMainMenu>().Build();
    }

    void StartGame()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
        }

        foreach (var entity in HideFilter.Entities)
        {
            Remove<Invisible>(entity);
        }

        Set(CreateEntity(), new Initialize());

        var ball = CreateEntity();
        Set(ball, new Model(Content.Models.Donut.ID));
        Set(ball, new Scale(Vector2.One * 10.0f));
        Set(ball, new Position(new Vector2(
                    Dimensions.GameWidth * 0.5f,
                    Dimensions.GameHeight * 0.5f
                )));
        Set(ball, new Velocity(Vector2.Zero));
        Set(ball, new BoundingBox(0, 0, 18, 18));
        Set(ball, new SolidCollision());
        Set(ball, new Bounce());
        Set(ball, new CanBeHit());
        Set(ball, new HasGravity(1f));
        Set(ball, new CameraFollows());
        Set(ball, new DestroyOnStartGame());
        Set(ball, new Highlight());
        Set(ball, new CanDealDamageToBlock(1));


        var player = CreateEntity();
        Set(player, new Model(Content.Models.EmptyTriangle.ID));
        Set(player, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.9f
            )));
        Set(player, new Orientation(0f));
        Set(player, new Velocity(Vector2.Zero));
        Set(player, new BoundingBox(0, 8, 55, 50));
        Set(player, new SolidCollision());
        Set(player, new HitBall());
        Set(player, new Scale(new Vector2(4, 4)));
        Set(player, new Player());
        Set(player, new FollowsCamera(Dimensions.GameHeight * 0.9f));
        Set(player, new DestroyOnStartGame());

        var power = CreateEntity();
        Set(power, new Model(Content.Models.Triangle.ID));
        Set(power, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.9f
            )));
        Set(power, new Orientation(0f));
        Set(power, new Velocity(Vector2.Zero));
        Set(power, new Scale(new Vector2(0f, 0.5f)));
        Set(power, new Power(0f, 0.015f, 2f));
        Set(power, new DestroyOnStartGame());
        Set(power, new Highlight());
        Relate(power, player, new ChildOf(new Vector2(0f, 0f)));

        var xp = CreateEntity();
        Set(xp, new FollowsCamera(Dimensions.GameHeight - 8f));
        Set(xp, new Model(Content.Models.Square.ID));
        Set(xp, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight - 8f
            )));
        Set(xp, new Scale(new Vector2(0f, 0.5f)));
        Set(xp, new XP(0, 10));
        Set(xp, new DestroyOnStartGame());
        Set(xp, new Highlight());

        Relate(ball, player, new HeldBy(new Vector2(0f, -32.0f)));
        Set(ball, new Velocity(Vector2.Zero));

        var leftBound = CreateEntity();
        Set(leftBound, new Position(new Vector2(-8, 0)));
        Set(leftBound, new BoundingBox(0, 0, 16, 2000));
        Set(leftBound, new SolidCollision());
        Set(leftBound, new FollowsCamera(0));
        Set(leftBound, new DestroyOnStartGame());

        var leftBoundSprite = CreateEntity();
        Set(leftBoundSprite, new Position(new Vector2(-9, 0)));
        Set(leftBoundSprite, new Model(Content.Models.Square.ID));
        Set(leftBoundSprite, new Scale(new Vector2(24f, 2000)));
        Set(leftBoundSprite, new DestroyOnStartGame());
        Set(leftBoundSprite, new FollowsCamera(0));

        var rightBound = CreateEntity();
        Set(rightBound, new Position(new Vector2(Dimensions.GameWidth + 8, 0)));
        Set(rightBound, new BoundingBox(0, 0, 16, 2000));
        Set(rightBound, new SolidCollision());
        Set(rightBound, new FollowsCamera(0));
        Set(rightBound, new DestroyOnStartGame());

        var rightBoundSprite = CreateEntity();
        Set(rightBoundSprite, new Position(new Vector2(Dimensions.GameWidth + 9, 0)));
        Set(rightBoundSprite, new Model(Content.Models.Square.ID));
        Set(rightBoundSprite, new Scale(new Vector2(24f, 2000)));
        Set(rightBoundSprite, new DestroyOnStartGame());
        Set(rightBoundSprite, new FollowsCamera(0));


        var bottomBound = CreateEntity();
        Set(bottomBound, new Position(new Vector2(Dimensions.GameWidth * 0.5f, Dimensions.GameHeight + 8)));
        Set(bottomBound, new BoundingBox(0, 0, Dimensions.GameWidth, 16));
        Set(bottomBound, new SolidCollision());
        Set(bottomBound, new ResetBallOnHit());
        Set(bottomBound, new FollowsCamera(Dimensions.GameHeight + 8));
        Set(bottomBound, new DestroyOnStartGame());

        var cameraEntity = CreateEntity();
        Set(cameraEntity, new CameraPosition(0f));
        Set(cameraEntity, new DestroyOnStartGame());

        var livesLabel = CreateEntity();
        Set(livesLabel, new Position(new Vector2(10, UILayoutConstants.ScoreLabelY)));
        Set(livesLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("LIVES")));
        Set(livesLabel, new UI());
        Set(livesLabel, new DestroyOnStartGame());

        var livesEntity = CreateEntity();
        Set(livesEntity, new Lives(3));
        Set(livesEntity, new Position(new Vector2(10, UILayoutConstants.ScoreY)));
        Set(livesEntity,
         new Text(
            Fonts.BodyFont,
            Fonts.MidSize,
            Stores.TextStorage.GetID("")));
        Set(livesEntity, new UI());
        Set(livesEntity, new Highlight());
        Set(livesEntity, new DestroyOnStartGame());

        var levelLabel = CreateEntity();
        Set(levelLabel, new Position(new Vector2(10, UILayoutConstants.HighScoreLabelY)));
        Set(levelLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("LEVEL")));
        Set(levelLabel, new UI());
        Set(levelLabel, new DestroyOnStartGame());

        var levelEntity = CreateEntity();
        Set(levelEntity, new Level(0));
        Set(levelEntity, new Position(new Vector2(10, UILayoutConstants.HighScoreY)));
        Set(levelEntity,
         new Text(
            Fonts.BodyFont,
            Fonts.MidSize,
            Stores.TextStorage.GetID("")));
        Set(levelEntity, new UI());
        Set(levelEntity, new Highlight());
        Set(levelEntity, new DestroyOnStartGame());

        var scoreLabel = CreateEntity();
        Set(scoreLabel, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.ScoreLabelY)));
        Set(scoreLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("SCORE")));
        Set(scoreLabel, new UI());
        Set(scoreLabel, new DestroyOnStartGame());

        var highScoreLabel = CreateEntity();
        Set(highScoreLabel, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.HighScoreLabelY)));
        Set(highScoreLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("BEST")));
        Set(highScoreLabel, new UI());
        Set(highScoreLabel, new DestroyOnStartGame());

        var highScoreEntity = CreateEntity();
        Set(highScoreEntity, new HighScore(0));
        Set(highScoreEntity, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.HighScoreY)));
        Set(highScoreEntity,
         new Text(
            Fonts.BodyFont,
            Fonts.BodySize,
            Stores.TextStorage.GetID("")));
        Set(highScoreEntity, new UI());
        Set(highScoreEntity, new Highlight());
        Set(highScoreEntity, new HideOnMainMenu());

        var gemsLabel = CreateEntity();
        Set(gemsLabel, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.GemsLabelY)));
        Set(gemsLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("GEMS")));
        Set(gemsLabel, new UI());
        Set(gemsLabel, new DestroyOnStartGame());

        var scoreEntity = Some<Score>() ? GetSingletonEntity<Score>() : CreateEntity();
        Set(scoreEntity, new Text(Fonts.BodyFont, Fonts.BodySize, Stores.TextStorage.GetID("0")));
        Set(scoreEntity, new Highlight());
        Set(scoreEntity, new Score(0));
        Set(scoreEntity, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.ScoreY)));
        Set(scoreEntity, new UI());
        Set(scoreEntity, new DestroyOnStartGame());

        var gemsEntity = Some<Gems>() ? GetSingletonEntity<Gems>() : CreateEntity();
        Set(gemsEntity, new Text(Fonts.BodyFont, Fonts.BodySize, Stores.TextStorage.GetID("0")));
        Set(gemsEntity, new Highlight());
        Set(gemsEntity, new Gems(0, 0));
        Set(gemsEntity, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.GemsY)));
        Set(gemsEntity, new UI());
        Set(gemsEntity, new DestroyOnStartGame());

    }

    void MainMenu()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
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
        Set(gameTitle, new DestroyOnStartGame());
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
        Set(prompt, new DestroyOnStartGame());
        Set(prompt, new MainMenu());
    }

    string GetFormattedScore(int amount, int length = 8)
    {
        return amount >= 0
            ? amount.ToString($"D{length}")
            : amount.ToString($"D{length - 1}");
    }

    public override void Update(TimeSpan delta)
    {
        var inputState = GetSingleton<InputState>();

        if (!Some<DestroyOnStartGame>() || (Some<Lives>() && GetSingleton<Lives>().Value <= 0))
        {
            MainMenu();
        }

        if ((Some<DestroyOnStartGame>() && inputState.Restart.IsPressed) ||
            (Some<MainMenu>() && inputState.Start.IsPressed))
        {
            StartGame();
        }

        if (!Some<Gems>())
            return;

        var gemsEntity = GetSingletonEntity<Gems>();
        var gems = Get<Gems>(gemsEntity);

        Set(gemsEntity,
        new Text(
            Fonts.BodyFont,
            Fonts.BodySize,
            Stores.TextStorage.GetID(GetFormattedScore(gems.Total)),
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
            Stores.TextStorage.GetID(GetFormattedScore(newScore)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (newScore > highScore)
        {
            highScore = newScore;
            Set(highScoreEntity, new HighScore(newScore));
        }

        Set(highScoreEntity,
         new Text(
            Fonts.BodyFont,
            Fonts.BodySize,
            Stores.TextStorage.GetID(GetFormattedScore(highScore)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        if (!Some<Lives>())
            return;

        var livesEntity = GetSingletonEntity<Lives>();
        var lives = Get<Lives>(livesEntity);

        Set(livesEntity,
        new Text(
            Fonts.BodyFont,
            Fonts.MidSize,
            Stores.TextStorage.GetID(GetFormattedScore(lives.Value, 2)),
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
            Stores.TextStorage.GetID(GetFormattedScore(level.Value, 2)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));

        var ballEntity = GetSingletonEntity<CanDealDamageToBlock>();
        Set(ballEntity, new CanDealDamageToBlock(level.Value + 1));

    }
}