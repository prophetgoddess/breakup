using System.Numerics;
using Ball;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Audio;

public class Motion : MoonTools.ECS.System
{
    public Filter MotionFilter;
    public Filter ColliderFilter;
    System.Random Random = new System.Random();

    public Motion(World world) : base(world)
    {
        MotionFilter = FilterBuilder.Include<Velocity>().Include<Position>().Build();
        ColliderFilter = FilterBuilder.Include<Position>().Include<SolidCollision>().Build();
    }

    public bool Overlaps(Vector2 posA, BoundingBox boxA, Vector2 posB, BoundingBox boxB)
    {
        var aMinX = boxA.X + posA.X - boxA.Width * 0.5f;
        var aMinY = boxA.Y + posA.Y - boxA.Height * 0.5f;
        var aMaxX = boxA.X + posA.X + boxA.Width * 0.5f;
        var aMaxY = boxA.Y + posA.Y + boxA.Height * 0.5f;

        var bMinX = boxB.X + posB.X - boxB.Width * 0.5f;
        var bMinY = boxB.Y + posB.Y - boxB.Height * 0.5f;
        var bMaxX = boxB.X + posB.X + boxB.Width * 0.5f;
        var bMaxY = boxB.Y + posB.Y + boxB.Height * 0.5f;

        bool overlaps = aMinX <= bMaxX &&
                        aMaxX >= bMinX &&
                        aMinY <= bMaxY &&
                        aMaxY >= bMinY;

        return overlaps;
    }

    public Vector2 Sweep(Entity e, Vector2 position, Vector2 velocity, BoundingBox boundingBox)
    {
        var destX = position.X + velocity.X;
        var destY = position.Y + velocity.Y;

        foreach (var other in ColliderFilter.Entities)
        {
            var otherPos = Get<Position>(other).Value;
            var otherBox = Get<BoundingBox>(other);
            if (e != other && Overlaps(new Vector2(destX, position.Y), boundingBox, otherPos, otherBox))
            {
                if (!Related<IgnoreSolidCollision>(other, e) && !Related<IgnoreSolidCollision>(e, other))
                {
                    destX = position.X;
                    Relate(e, other, new Colliding(CollisionDirection.X, true));
                    break;
                }
                Relate(e, other, new Colliding(CollisionDirection.X, false));
            }
        }

        foreach (var other in ColliderFilter.Entities)
        {
            var otherPos = Get<Position>(other).Value;
            var otherBox = Get<BoundingBox>(other);
            if (e != other && Overlaps(new Vector2(position.X, destY), boundingBox, otherPos, otherBox))
            {

                if (!Related<IgnoreSolidCollision>(other, e) && !Related<IgnoreSolidCollision>(e, other))
                {
                    destY = position.Y;
                    Relate(e, other, new Colliding(CollisionDirection.Y, true));
                    break;
                }

                Relate(e, other, new Colliding(CollisionDirection.Y, false));
            }
        }

        return new Vector2(destX, destY);
    }

    public override void Update(TimeSpan delta)
    {
        var dt = (float)delta.TotalSeconds;

        foreach (var entity in MotionFilter.Entities)
        {

            var position = Get<Position>(entity).Value;
            var velocity = Get<Velocity>(entity).Value;
            if (Has<HasGravity>(entity))
            {
                velocity.Y += 5f;
                Set(entity, new Velocity(velocity));
            }

            var dest = position + velocity;

            if (HasOutRelation<HeldBy>(entity))
            {
                var held = OutRelationSingleton<HeldBy>(entity);
                var data = GetRelationData<HeldBy>(entity, held);
                dest = Get<Position>(held).Value + data.offset;
                Set(entity, new Position(dest));
                continue;
            }

            if (Has<BoundingBox>(entity) && Has<SolidCollision>(entity))
            {
                dest = Sweep(entity, position, velocity * dt, Get<BoundingBox>(entity));

                foreach (var other in OutRelations<Colliding>(entity))
                {
                    var collision = GetRelationData<Colliding>(entity, other);
                    var xCollision = collision.Direction == CollisionDirection.X;
                    var yCollision = collision.Direction == CollisionDirection.Y;

                    if (Has<Bounce>(entity) && Has<ResetBallOnHit>(other))
                    {
                        var player = GetSingletonEntity<Player>();
                        Relate(entity, player, new HeldBy(new Vector2(0f, -32.0f)));
                        Set(entity, new Velocity(Vector2.Zero));
                        Unrelate<Colliding>(entity, other);
                        Relate(entity, player, new IgnoreSolidCollision());

                        var life = GetSingletonEntity<FirstLife>();

                        while (HasOutRelation<NextLife>(life))
                        {
                            life = OutRelationSingleton<NextLife>(life);
                        }

                        Destroy(life);

                        continue;
                    }

                    if (Has<HitBall>(other) && Has<CanBeHit>(entity))
                    {
                        var meterValue = GetSingleton<Meter>().Value * 200.0f;
                        var otherPos = Get<Position>(other).Value;
                        var dir = Vector2.Normalize(otherPos - dest);
                        velocity = dir * -velocity.Length();
                        velocity.Y -= 200.0f + meterValue;
                        Set(entity, new Velocity(velocity));
                    }

                    else if (Has<Bounce>(entity) && collision.Solid)
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
                        if (yCollision && !Has<CanTakeDamageFromBall>(other) && position.Y < otherPos.Y)
                        {
                            newVelocity.Y += (float)Random.NextDouble() * -100.0f;
                            newVelocity.X += (float)Random.NextDouble() * 50.0f * (Random.NextDouble() < 0.5f ? 1.0f : -1.0f);
                        }

                        Set(entity, new Velocity(newVelocity));

                    }

                    Unrelate<Colliding>(entity, other);
                }

            }

            Set(entity, new Position(dest));

        }

    }
}
