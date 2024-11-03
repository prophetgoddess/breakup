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

    public (Vector2 dest, bool xCollision, bool yCollision)
    Sweep(Entity e, Vector2 position, Vector2 velocity, BoundingBox boundingBox)
    {
        bool xCollision = false;
        bool yCollision = false;

        var destX = position.X + velocity.X;
        var destY = position.Y + velocity.Y;

        foreach (var other in ColliderFilter.Entities)
        {
            var otherPos = Get<Position>(other).value;
            var otherBox = Get<BoundingBox>(other);
            if (e != other && Overlaps(new Vector2(destX, position.Y), boundingBox, otherPos, otherBox))
            {
                xCollision = true;
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
                destY = position.Y;
                break;
            }
        }

        return (new Vector2(destX, destY), xCollision, yCollision);
    }

    public override void Update(TimeSpan delta)
    {
        var dt = (float)delta.TotalSeconds;

        foreach (var entity in MotionFilter.Entities)
        {
            var position = Get<Position>(entity).value;
            var velocity = Get<Velocity>(entity).value * dt;
            var dest = position + velocity;

            if (Has<BoundingBox>(entity) && Has<SolidCollision>(entity))
            {
                (dest, var xCollision, var yCollision) = Sweep(entity, position, velocity, Get<BoundingBox>(entity));

                var newVelocity = velocity;

                if (xCollision)
                {
                    newVelocity.X = -velocity.X;
                }
                if (yCollision)
                {
                    newVelocity.Y = -velocity.Y;
                }

                Set(entity, new Velocity(newVelocity));
            }

            Set(entity, new Position(dest));
        }
    }
}