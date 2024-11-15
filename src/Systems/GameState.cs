using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Audio;

namespace Ball;
public class GameState : MoonTools.ECS.System
{

    Filter DestroyFilter;
    Filter LivesFilter;

    public GameState(World world) : base(world)
    {
        DestroyFilter = FilterBuilder.Include<DestroyOnRestartGame>().Build();
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
        Set(ball, new HasGravity());
        Set(ball, new CameraFollows());
        Set(ball, new DestroyOnRestartGame());

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
        Set(player, new DestroyOnRestartGame());

        var meter = CreateEntity();
        Set(meter, new Model(Content.Models.Triangle.ID));
        Set(meter, new Position(new Vector2(
                Dimensions.GameWidth * 0.5f,
                Dimensions.GameHeight * 0.9f
            )));
        Set(meter, new Orientation(0f));
        Set(meter, new Velocity(Vector2.Zero));
        Set(meter, new Scale(new Vector2(0f, 0.5f)));
        Set(meter, new Meter(0f, 0.02f, 4f));
        Set(meter, new DestroyOnRestartGame());
        Relate(meter, player, new ChildOf(new Vector2(0f, 0f)));

        Relate(ball, player, new HeldBy(new Vector2(0f, -32.0f)));
        Set(ball, new Velocity(Vector2.Zero));
        Relate(ball, player, new IgnoreSolidCollision());

        var leftBound = CreateEntity();
        Set(leftBound, new Position(new Vector2(-8, 0)));
        Set(leftBound, new BoundingBox(0, 0, 16, 2000));
        Set(leftBound, new SolidCollision());
        Set(leftBound, new FollowsCamera(0));
        Set(leftBound, new DestroyOnRestartGame());

        var leftBoundSprite = CreateEntity();
        Set(leftBoundSprite, new Position(new Vector2(-9, 0)));
        Set(leftBoundSprite, new Model(Content.Models.Square.ID));
        Set(leftBoundSprite, new Scale(new Vector2(1.5f, 2000)));
        Set(leftBoundSprite, new DestroyOnRestartGame());

        var rightBound = CreateEntity();
        Set(rightBound, new Position(new Vector2(Dimensions.GameWidth + 8, 0)));
        Set(rightBound, new BoundingBox(0, 0, 16, 2000));
        Set(rightBound, new SolidCollision());
        Set(rightBound, new FollowsCamera(0));
        Set(rightBound, new DestroyOnRestartGame());

        var rightBoundSprite = CreateEntity();
        Set(rightBoundSprite, new Position(new Vector2(Dimensions.GameWidth + 9, 0)));
        Set(rightBoundSprite, new Model(Content.Models.Square.ID));
        Set(rightBoundSprite, new Scale(new Vector2(1.5f, 2000)));
        Set(rightBoundSprite, new DestroyOnRestartGame());

        var bottomBound = CreateEntity();
        Set(bottomBound, new Position(new Vector2(Dimensions.GameWidth * 0.5f, Dimensions.GameHeight + 8)));
        Set(bottomBound, new BoundingBox(0, 0, Dimensions.GameWidth, 16));
        Set(bottomBound, new SolidCollision());
        Set(bottomBound, new ResetBallOnHit());
        Set(bottomBound, new FollowsCamera(Dimensions.GameHeight + 8));
        Set(bottomBound, new DestroyOnRestartGame());

        var cameraEntity = CreateEntity();
        Set(cameraEntity, new CameraPosition(0f));
        Set(cameraEntity, new DestroyOnRestartGame());

        Entity lastLife = default;
        for (int i = 0; i < 3; i++)
        {
            var lifeEntity = CreateEntity();
            Set(lifeEntity, new Life());
            Set(lifeEntity, new Model(Content.Models.Donut.ID));
            Set(lifeEntity, new Scale(Vector2.One * 16.0f));
            Set(lifeEntity, new UI());
            Set(lifeEntity, new Position(new Vector2(100, 50 + i * 50)));
            Set(lifeEntity, new DestroyOnRestartGame());

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

        var scoreEntity = Some<Score>() ? GetSingletonEntity<Score>() : CreateEntity();
        Set(scoreEntity, new Text(Stores.FontStorage.GetID(Content.Fonts.FX300), 24, Stores.TextStorage.GetID("0")));
        Set(scoreEntity, new Score(0));
        Set(scoreEntity, new Position(new Vector2(Dimensions.WindowWidth - 190, 60)));
        Set(scoreEntity, new UI());

    }

    string GetFormattedScore(int amount, int length = 6)
    {
        return amount >= 0
            ? amount.ToString($"D{length}")
            : amount.ToString($"D{length - 1}");
    }

    public override void Update(TimeSpan delta)
    {

        var inputState = GetSingleton<InputState>();

        if (LivesFilter.Count == 0 || inputState.Restart.IsPressed)
        {
            StartGame();
        }

        if (!Some<Score>())
            return;

        var newScore = (int)GetSingleton<CameraPosition>().Y;
        var scoreEntity = GetSingletonEntity<Score>();
        var score = Get<Score>(scoreEntity);
        var highScoreEntity = GetSingletonEntity<HighScore>();
        var highScore = Get<HighScore>(highScoreEntity).Value;

        Set(scoreEntity, new Score(newScore));
        Set(scoreEntity,
        new Text(
            Stores.FontStorage.GetID(Content.Fonts.FX300),
            24,
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
            Stores.FontStorage.GetID(Content.Fonts.FX300),
            24,
            Stores.TextStorage.GetID(GetFormattedScore(highScore)),
            MoonWorks.Graphics.Font.HorizontalAlignment.Left,
            MoonWorks.Graphics.Font.VerticalAlignment.Middle));
    }
}