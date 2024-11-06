using System.Numerics;
using Ball;
using MoonTools.ECS;

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
        var aMinX = boxA.X + posA.X;
        var aMinY = boxA.Y + posA.Y;
        var aMaxX = boxA.X + posA.X + boxA.Width;
        var aMaxY = boxA.Y + posA.Y + boxA.Height;

        var bMinX = boxB.X + posB.X;
        var bMinY = boxB.Y + posB.Y;
        var bMaxX = boxB.X + posB.X + boxB.Width;
        var bMaxY = boxB.Y + posB.Y + boxB.Height;

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
            var otherPos = Get<Position>(other).value;
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
            var otherPos = Get<Position>(other).value;
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

            var position = Get<Position>(entity).value;
            var velocity = Get<Velocity>(entity).value;
            var dest = position + velocity;

            if (Has<HasGravity>(entity))
            {
                velocity.Y += 100f;
            }

            if (Has<BoundingBox>(entity) && Has<SolidCollision>(entity))
            {
                dest = Sweep(entity, position, velocity * dt, Get<BoundingBox>(entity));

                foreach (var other in OutRelations<Colliding>(entity))
                {
                    var collision = GetRelationData<Colliding>(entity, other);
                    var xCollision = collision.Direction == CollisionDirection.X;
                    var yCollision = collision.Direction == CollisionDirection.Y;

                    if (Has<HitBall>(other) && Has<CanBeHit>(entity))
                    {
                        var otherPos = Get<Position>(other).value;
                        var dir = Vector2.Normalize(otherPos - dest);
                        velocity = dir * -velocity.Length();
                        Set(entity, new Velocity(velocity));
                    }

                    else if (Has<Bounce>(entity) && collision.Solid)
                    {
                        if (Has<DestroyOnContactWithBall>(other))
                        {
                            Destroy(other);
                        }

                        var newVelocity = Vector2.Zero;
                        var bounceX = false;
                        var bounceY = false;

                        if (xCollision)
                        {
                            bounceX = true;
                            newVelocity.X = -velocity.X;
                        }

                        if (yCollision)
                        {
                            bounceY = true;
                            newVelocity.Y = -velocity.Y;
                        }

                        if (bounceX && !bounceY)
                            Set(entity, new Velocity(new Vector2(newVelocity.X, velocity.Y)));

                        if (bounceY && !bounceX)
                            Set(entity, new Velocity(new Vector2(velocity.X, newVelocity.Y)));

                        if (bounceY && bounceX)
                            Set(entity, new Velocity(new Vector2(newVelocity.X, newVelocity.Y)));

                    }

                    Unrelate<Colliding>(entity, other);
                }

            }

            Set(entity, new Position(dest));

        }

    }
}
