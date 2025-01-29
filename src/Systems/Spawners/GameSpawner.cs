using System.Numerics;
using MoonTools.ECS;
using MoonWorks;

namespace Ball;

public class GameSpawner : Manipulator
{
    SaveGame SaveGame;
    Filter DestroyFilter;
    BallSpawner BallSpawner;
    GiveUpgrade GiveUpgrade;

    public GameSpawner(World world) : base(world)
    {
        SaveGame = new SaveGame(world);
        DestroyFilter = FilterBuilder.Include<DestroyOnStateTransition>().Build();
        BallSpawner = new BallSpawner(world);
        GiveUpgrade = new GiveUpgrade(world);

    }

    public void StartGame()
    {
        if (Some<SetHighScoreThisRun>())
        {
            Destroy(GetSingletonEntity<SetHighScoreThisRun>());
        }
        UpgradeMenuSpawner.ResetUpgrades();

        foreach (var entity in DestroyFilter.Entities)
        {
            Destroy(entity);
        }

        Set(CreateEntity(), new Initialize());

        var ball = BallSpawner.SpawnBall(new Vector2(
                    Dimensions.GameWidth * 0.5f,
                    Dimensions.GameHeight * 0.6f
                ));

        var player = CreateEntity();
        Set(player, new SDFGraphic(Content.SDF.EmptyTriangle));
        Set(player, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.9f
            )));
        Set(player, new Combo(0));
        Set(player, new Orientation(0f));
        Set(player, new Velocity(Vector2.Zero));
        Set(player, new BoundingBox(0, 0, 50, 30));
        Set(player, new Origin(new Vector2(0.5f, 0.4f)));
        Set(player, new SolidCollision());
        Set(player, new HitBall());
        Set(player, new Scale(new Vector2(50, 50)));
        Set(player, new Player());
        Set(player, new FollowsCamera(Dimensions.GameHeight * 0.9f));
        Set(player, new DestroyOnStateTransition());

        Relate(ball, player, new HeldBy(new Vector2(0f, -36.0f)));
        Set(ball, new Velocity(Vector2.Zero));

        var power = CreateEntity();
        Set(power, new SDFGraphic(Content.SDF.Triangle));
        Set(power, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.9f
            )));
        Set(power, new Origin(new Vector2(0.5f, 0.4f)));
        Set(power, new Orientation(0f));
        Set(power, new Velocity(Vector2.Zero));
        Set(power, new Scale(new Vector2(0f, 0f)));
        Set(power, new Power(0f, 0.01f, 25f));
        Set(power, new DestroyOnStateTransition());
        Set(power, new Highlight());
        Relate(power, player, new ChildOf(new Vector2(0f, 0f)));

        var leftBound = CreateEntity();
        Set(leftBound, new Position(new Vector2(-8, 0)));
        Set(leftBound, new BoundingBox(0, 0, 16, 2000));
        Set(leftBound, new SolidCollision());
        Set(leftBound, new FollowsCamera(0));
        Set(leftBound, new DestroyOnStateTransition());

        var leftBoundSprite = CreateEntity();
        Set(leftBoundSprite, new Position(new Vector2(-9, 0)));
        Set(leftBoundSprite, new Model(Content.Models.Square.ID));
        Set(leftBoundSprite, new Scale(new Vector2(24f, 2000)));
        Set(leftBoundSprite, new DestroyOnStateTransition());
        Set(leftBoundSprite, new FollowsCamera(0));
        Set(leftBoundSprite, new KeepOpacityWhenPaused());

        var rightBound = CreateEntity();
        Set(rightBound, new Position(new Vector2(Dimensions.GameWidth + 8, 0)));
        Set(rightBound, new BoundingBox(0, 0, 16, 2000));
        Set(rightBound, new SolidCollision());
        Set(rightBound, new FollowsCamera(0));
        Set(rightBound, new DestroyOnStateTransition());

        var rightBoundSprite = CreateEntity();
        Set(rightBoundSprite, new Position(new Vector2(Dimensions.GameWidth + 9, 0)));
        Set(rightBoundSprite, new Model(Content.Models.Square.ID));
        Set(rightBoundSprite, new Scale(new Vector2(24f, 2000)));
        Set(rightBoundSprite, new DestroyOnStateTransition());
        Set(rightBoundSprite, new FollowsCamera(0));
        Set(rightBoundSprite, new KeepOpacityWhenPaused());

        var bottomBound = CreateEntity();
        Set(bottomBound, new Position(new Vector2(Dimensions.GameWidth * 0.5f, Dimensions.GameHeight + 8)));
        Set(bottomBound, new BoundingBox(0, 0, Dimensions.GameWidth, 16));
        Set(bottomBound, new SolidCollision());
        Set(bottomBound, new DestroysBall());
        Set(bottomBound, new FollowsCamera(Dimensions.GameHeight + 8));
        Set(bottomBound, new DestroyOnStateTransition());
        Set(bottomBound, new Model(Content.Models.Square.ID));
        Set(bottomBound, new Scale(new Vector2(Dimensions.GameWidth, 26.0f)));
        Set(bottomBound, new Highlight());
        Set(bottomBound, new Invisible());

        var cameraEntity = CreateEntity();
        Set(cameraEntity, new CameraPosition(0f));
        Set(cameraEntity, new DestroyOnStateTransition());

        var livesLabel = CreateEntity();
        Set(livesLabel, new Position(new Vector2(10, UILayoutConstants.ScoreLabelY)));
        Set(livesLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("LIVES")));
        Set(livesLabel, new UI());
        Set(livesLabel, new LivesLabel());
        Set(livesLabel, new DestroyOnStateTransition());

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
        Set(livesEntity, new DestroyOnStateTransition());

        var levelLabel = CreateEntity();
        Set(levelLabel, new Position(new Vector2(10, UILayoutConstants.HighScoreLabelY)));
        Set(levelLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("LEVEL")));
        Set(levelLabel, new UI());
        Set(levelLabel, new DestroyOnStateTransition());

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
        Set(levelEntity, new DestroyOnStateTransition());

        var xpLabel = CreateEntity();
        Set(xpLabel, new Position(new Vector2(10, UILayoutConstants.GemsLabelY)));
        Set(xpLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("NEXT")));
        Set(xpLabel, new UI());
        Set(xpLabel, new DestroyOnStateTransition());

        var xp = CreateEntity();
        Set(xp, new Position(new Vector2(
                10,
                UILayoutConstants.GemsY
            )));
        Set(xp, new XP(0, 10));
        Set(xp,
        new Text(
            Fonts.BodyFont,
            Fonts.InfoSize,
            Stores.TextStorage.GetID("")));
        Set(xp, new DestroyOnStateTransition());
        Set(xp, new Highlight());
        Set(xp, new UI());

        var scoreLabel = CreateEntity();
        Set(scoreLabel, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.ScoreLabelY)));
        Set(scoreLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("SCORE")));
        Set(scoreLabel, new UI());
        Set(scoreLabel, new DestroyOnStateTransition());

        var highScoreLabel = CreateEntity();
        Set(highScoreLabel, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.HighScoreLabelY)));
        Set(highScoreLabel,
         new Text(
            Fonts.HeaderFont,
            Fonts.HeaderSize,
            Stores.TextStorage.GetID("BEST")));
        Set(highScoreLabel, new UI());
        Set(highScoreLabel, new DestroyOnStateTransition());

        var saveData = SaveGame.Load();

        var highScoreEntity = CreateEntity();
        Set(highScoreEntity, new HighScore(saveData.HighScore));
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
        Set(gemsLabel, new DestroyOnStateTransition());

        var scoreEntity = Some<Score>() ? GetSingletonEntity<Score>() : CreateEntity();
        Set(scoreEntity, new Text(Fonts.BodyFont, Fonts.BodySize, Stores.TextStorage.GetID("0")));
        Set(scoreEntity, new Highlight());
        Set(scoreEntity, new Score(0));
        Set(scoreEntity, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.ScoreY)));
        Set(scoreEntity, new UI());
        Set(scoreEntity, new DestroyOnStateTransition());

        var gemsEntity = Some<Gems>() ? GetSingletonEntity<Gems>() : CreateEntity();
        Set(gemsEntity, new Text(Fonts.BodyFont, Fonts.BodySize, Stores.TextStorage.GetID("0")));
        Set(gemsEntity, new Highlight());
        Set(gemsEntity, new Gems(0, 0));
        Set(gemsEntity, new Position(new Vector2(UILayoutConstants.InfoX, UILayoutConstants.GemsY)));
        Set(gemsEntity, new UI());
        Set(gemsEntity, new DestroyOnStateTransition());

        GiveUpgrade.Upgrade(Upgrades.Invictus);

    }
}