using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class BallSpawner : MoonTools.ECS.Manipulator
{
    public BallSpawner(World world) : base(world)
    {
    }

    public Entity SpawnBall(Vector2 position)
    {
        var ball = CreateEntity();
        Set(ball, new SDFGraphic(Content.SDF.ball));
        Set(ball, new Scale(Vector2.One * 24.0f));
        Set(ball, new Position(position));
        Set(ball, new Velocity(Vector2.Zero));
        Set(ball, new BoundingBox(0, 0, 22, 22));
        Set(ball, new SolidCollision());
        Set(ball, new Bounce(0.9f));
        Set(ball, new CanBeHit());
        Set(ball, new HasGravity(1f));
        Set(ball, new CameraFollows());
        Set(ball, new DestroyOnStateTransition());
        Set(ball, new Highlight());
        Set(ball, new CanDealDamageToBlock(1));

        return ball;
    }
}