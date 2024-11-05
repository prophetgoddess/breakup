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

    public (Vector2 dest, bool xCollision, bool yCollision, Entity HitX, Entity HitY)
    Sweep(Entity e, Vector2 position, Vector2 velocity, BoundingBox boundingBox)
    {
        bool xCollision = false;
        bool yCollision = false;

        var destX = position.X + velocity.X;
        var destY = position.Y + velocity.Y;

        Entity hitX = default;
        Entity hitY = default;

        foreach (var other in ColliderFilter.Entities)
        {
            var otherPos = Get<Position>(other).value;
            var otherBox = Get<BoundingBox>(other);
            if (e != other && Overlaps(new Vector2(destX, position.Y), boundingBox, otherPos, otherBox))
            {
                xCollision = true;
                hitX = other;
                destX = position.X;
                break;
            }
        }

        foreach (var other in ColliderFilter.Entities)
        {
            var otherPos = Get<Position>(other).value;
            var otherBox = Get<BoundingBox>(other);
            if (e != other && Overlaps(new Vector2(position.X, destY), boundingBox, otherPos, otherBox))
            {
                yCollision = true;
                hitY = other;
                destY = position.Y;
                break;
            }
        }

        return (new Vector2(destX, destY), xCollision, yCollision, hitX, hitY);
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
                (dest, var xCollision, var yCollision, var hitX, var hitY) = Sweep(entity, position, velocity * dt, Get<BoundingBox>(entity));

                if (Has<HitBall>(entity))
                {
                    if (xCollision && Has<CanBeHit>(hitX))
                    {
                        var xVelocity = Get<Velocity>(hitX).value;
                        var otherPos = Get<Position>(hitX).value;
                        xVelocity.X += (otherPos.X - position.X) * 20.0f;
                        Set(hitX, new Velocity(xVelocity));
                    }

                    if (yCollision && Has<CanBeHit>(hitY))
                    {
                        var yVelocity = Get<Velocity>(hitY).value;
                        var otherPos = Get<Position>(hitY).value;
                        yVelocity.Y += (otherPos.Y - position.Y) * 20.0f;
                        Set(hitY, new Velocity(yVelocity));
                    }

                }

                if (Has<Bounce>(entity))
                {
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
                        Set(entity, new Velocity(new Vector2(newVelocity.X, velocity.Y) * 0.9f));

                    if (bounceY && !bounceX)
                        Set(entity, new Velocity(new Vector2(velocity.X, newVelocity.Y) * 0.9f));

                    if (bounceY && bounceX)
                        Set(entity, new Velocity(new Vector2(newVelocity.X, newVelocity.Y) * 0.9f));

                }


            }

            Set(entity, new Position(dest));

        }

    }
}
