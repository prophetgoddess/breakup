using System.Numerics;
using Ball;
using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Audio;

public class Motion : MoonTools.ECS.System
{
    public Filter MotionFilter;
    public Filter ColliderFilter;

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
            }

            Set(entity, new Position(dest));

        }

    }

}
