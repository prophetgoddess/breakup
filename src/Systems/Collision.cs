
using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class Collision : MoonTools.ECS.System
{
    Filter CollidingFilter;

    System.Random Random = new System.Random();

    public Collision(World world) : base(world)
    {
        CollidingFilter = FilterBuilder.Include<SolidCollision>().Include<Position>().Include<BoundingBox>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in CollidingFilter.Entities)
        {
            foreach (var other in OutRelations<Colliding>(entity))
            {
                var collision = GetRelationData<Colliding>(entity, other);
                var xCollision = collision.Direction == CollisionDirection.X;
                var yCollision = collision.Direction == CollisionDirection.Y;

                HandleBounce(entity, other, collision, xCollision, yCollision);

                HandleDestroyBall(entity, other);

                HandleHitBall(entity, other);

                Unrelate<Colliding>(entity, other);
            }
        }
    }

    void HandleBounce(Entity entity, Entity other, Colliding collision, bool xCollision, bool yCollision)
    {
        var velocity = Get<Velocity>(entity).Value;
        var position = Get<Position>(entity).Value;

        if (Has<Bounce>(entity) && collision.Solid)
        {
            if (Has<CanTakeDamageFromBall>(other) && Has<HitPoints>(other))
            {
                var hitPoints = Get<HitPoints>(other).Value;
                Set(other, new HitPoints(hitPoints - 1));
            }

            var newVelocity = velocity;

            if (xCollision && !yCollision)
                newVelocity = new Vector2(-velocity.X, velocity.Y) * 0.8f;

            else if (yCollision && !xCollision)
                newVelocity = new Vector2(velocity.X, -velocity.Y) * 0.8f;

            else if (yCollision && xCollision)
                newVelocity = new Vector2(-velocity.X, -velocity.Y) * 0.8f;

            var otherPos = Get<Position>(other).Value;
            if (yCollision && !Has<CanTakeDamageFromBall>(other) && position.Y < otherPos.Y && MathF.Abs(velocity.Length()) < float.Epsilon)
            {
                newVelocity.Y += (float)Random.NextDouble() * -100.0f;
                newVelocity.X += (float)Random.NextDouble() * 50.0f * (Random.NextDouble() < 0.5f ? 1.0f : -1.0f);
            }

            Set(entity, new Velocity(newVelocity));

        }
    }

    void HandleDestroyBall(Entity entity, Entity other)
    {
        if (Has<Bounce>(entity) && Has<ResetBallOnHit>(other))
        {
            var player = GetSingletonEntity<Player>();
            Relate(entity, player, new HeldBy(new Vector2(0f, -32.0f)));
            Set(entity, new Velocity(Vector2.Zero));
            Unrelate<Colliding>(entity, other);
            Relate(entity, player, new IgnoreSolidCollision());

            var meterEntity = GetSingletonEntity<Meter>();
            var meter = Get<Meter>(meterEntity);
            Set(meterEntity, new Meter(0f, meter.Decay, meter.Scale));

            var life = GetSingletonEntity<FirstLife>();

            while (HasOutRelation<NextLife>(life))
            {
                life = OutRelationSingleton<NextLife>(life);
            }

            Destroy(life);
        }
    }

    void HandleHitBall(Entity entity, Entity other)
    {
        var position = Get<Position>(entity).Value;
        var velocity = Get<Velocity>(entity).Value;

        if (Has<HitBall>(other) && Has<CanBeHit>(entity))
        {
            var meterValue = GetSingleton<Meter>().Value * 200.0f;
            var otherPos = Get<Position>(other).Value;
            var dir = Vector2.Normalize(otherPos - position);
            velocity = dir * -velocity.Length();

            if (HasOutRelation<Spinning>(other))
                velocity.Y -= 200.0f + meterValue;

            Set(entity, new Velocity(velocity));
        }
    }
}