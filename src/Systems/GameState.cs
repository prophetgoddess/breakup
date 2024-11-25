using System.Numerics;
using MoonTools.ECS;
using Filter = MoonTools.ECS.Filter;

namespace Ball;
public class GameState : MoonTools.ECS.System
{

    Filter DestroyFilter;
    Filter LivesFilter;

    public GameState(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnStartGame>().Build();
        LivesFilter = FilterBuilder.Include<Life>().Build();
    }

    void StartGame()
    {
        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
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

        var meter = CreateEntity();
        Set(meter, new Model(Content.Models.Triangle.ID));
        Set(meter, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.9f
            )));
        Set(meter, new Orientation(0f));
        Set(meter, new Velocity(Vector2.Zero));
        Set(meter, new Scale(new Vector2(0f, 0.5f)));
        Set(meter, new Meter(0f, 0.015f, 2f));
        Set(meter, new DestroyOnStartGame());
        Set(meter, new Highlight());
        Relate(meter, player, new ChildOf(new Vector2(0f, 0f)));

        Relate(ball, player, new HeldBy(new Vector2(0f, -32.0f)));
        Set(ball, new Velocity(Vector2.Zero));
        //Relate(ball, player, new IgnoreSolidCollision());

        var leftBound = CreateEntity();
        Set(leftBound, new Position(new Vector2(-8, 0)));
        Set(leftBound, new BoundingBox(0, 0, 16, 2000));
        Set(leftBound, new SolidCollision());
        Set(leftBound, new FollowsCamera(0));
        Set(leftBound, new DestroyOnStartGame());

        var leftBoundSprite = CreateEntity();
        Set(leftBoundSprite, new Position(new Vector2(-9, 0)));
        Set(leftBoundSprite, new Model(Content.Models.Square.ID));
        Set(leftBoundSprite, new Scale(new Vector2(1.5f, 2000)));
        Set(leftBoundSprite, new DestroyOnStartGame());

        var rightBound = CreateEntity();
        Set(rightBound, new Position(new Vector2(Dimensions.GameWidth + 8, 0)));
        Set(rightBound, new BoundingBox(0, 0, 16, 2000));
        Set(rightBound, new SolidCollision());
        Set(rightBound, new FollowsCamera(0));
        Set(rightBound, new DestroyOnStartGame());

        var rightBoundSprite = CreateEntity();
        Set(rightBoundSprite, new Position(new Vector2(Dimensions.GameWidth + 9, 0)));
        Set(rightBoundSprite, new Model(Content.Models.Square.ID));
        Set(rightBoundSprite, new Scale(new Vector2(1.5f, 2000)));
        Set(rightBoundSprite, new DestroyOnStartGame());

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

        Entity lastLife = default;
        for (int i = 0; i < 3; i++)
        {
            var lifeEntity = CreateEntity();
            Set(lifeEntity, new Life());
            Set(lifeEntity, new Model(Content.Models.Donut.ID));
            Set(lifeEntity, new Scale(Vector2.One * 32.0f));
            Set(lifeEntity, new UI());
            Set(lifeEntity, new Position(new Vector2(UILayoutConstants.LivesX, UILayoutConstants.LivesY + i * UILayoutConstants.LivesSpacing)));
            Set(lifeEntity, new DestroyOnStartGame());
            Set(lifeEntity, new Highlight());

            if (i == 0)
            {
                Set(lifeEntity, new FirstLife());
            }
            else
            {
                Relate(lastLife, lifeEntity, new NextLife());
            }
            lastLife = lifeEntity;
        }

        var livesLabel = CreateEntity();
        Set(livesLabel, new Position(new Vector2(10, UILayoutConstants.ScoreLabelY)));
        Set(livesLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("LIVES")));
        Set(livesLabel, new UI());
        Set(livesLabel, new DestroyOnStartGame());

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
        Set(highScoreEntity, new DestroyOnStartGame());

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

        var gameTitle = CreateEntity();
        Set(gameTitle, new Position(new Vector2(UILayoutConstants.TitleX, UILayoutConstants.TitleY)));
        Set(gameTitle,
         new Text(
            Fonts.HeaderFont,
            Fonts.TitleSize,
            Stores.TextStorage.GetID("break.zone"),
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

        if (!Some<DestroyOnStartGame>() || LivesFilter.Count == 0)
        {
            MainMenu();
        }

        if (Some<DestroyOnStartGame>() && inputState.Restart.IsPressed)
        {
            StartGame();
        }

        if (Some<MainMenu>() && inputState.Start.IsPressed)
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


    }
}