using System.Numerics;
using MoonTools.ECS;

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
        Set(ball, new Scale(16.0f));
        Set(ball, new Position(new Vector2(
                                          Dimensions.GameWidth * 0.5f,
                                          Dimensions.GameHeight * 0.5f
                                       )));
        Set(ball, new Velocity(Vector2.Zero));
        Set(ball, new BoundingBox(0, 0, 32, 32));
        Set(ball, new SolidCollision());
        Set(ball, new Bounce());
        Set(ball, new CanBeHit());
        Set(ball, new HasGravity());
        Set(ball, new CameraFollows());
        Set(ball, new DestroyOnRestartGame());

        var player = CreateEntity();
        Set(player, new Model(Content.Models.Triangle.ID));
        Set(player, new Position(new Vector2(
                                          Dimensions.GameWidth * 0.5f,
                                          Dimensions.GameHeight * 0.9f
                                       )));
        Set(player, new Orientation(0f));
        Set(player, new Velocity(Vector2.Zero));
        Set(player, new BoundingBox(0, 0, 32, 32));
        Set(player, new SolidCollision());
        Set(player, new HitBall());
        Set(player, new Scale(4.0f));
        Set(player, new Player());
        Set(player, new FollowsCamera(Dimensions.GameHeight * 0.9f));
        Set(player, new DestroyOnRestartGame());

        Relate(ball, player, new HeldBy(new Vector2(0f, -32.0f)));
        Set(ball, new Velocity(Vector2.Zero));
        Relate(ball, player, new IgnoreSolidCollision());

        var leftBound = CreateEntity();
        Set(leftBound, new Position(new Vector2(-8, 0)));
        Set(leftBound, new BoundingBox(0, 0, 16, 2000));
        Set(leftBound, new SolidCollision());
        Set(leftBound, new FollowsCamera(0));

        var rightBound = CreateEntity();
        Set(rightBound, new Position(new Vector2(Dimensions.GameWidth + 8, 0)));
        Set(rightBound, new BoundingBox(0, 0, 16, 2000));
        Set(rightBound, new SolidCollision());
        Set(rightBound, new FollowsCamera(0));
        Set(rightBound, new DestroyOnRestartGame());

        var bottomBound = CreateEntity();
        Set(bottomBound, new Position(new Vector2(0, Dimensions.GameHeight + 8)));
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
            Set(lifeEntity, new Scale(16.0f));
            Set(lifeEntity, new UI());
            Set(lifeEntity, new Position(new Vector2(100, 50 + i * 100)));
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

    }

    public override void Update(TimeSpan delta)
    {
        if (DestroyFilter.Count == 0 || LivesFilter.Count == 0)
        {
            StartGame();
        }
    }
}