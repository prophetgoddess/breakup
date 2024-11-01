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
        Vector4 a = new Vector4(
            boxA.X + posA.X,
            boxA.Y + posA.Y,
            boxA.X + posA.X + boxA.Width,
            boxA.Y + posA.Y + boxA.Height
        );

        Vector4 b = new Vector4(
            boxB.X + posB.X,
            boxB.Y + posB.Y,
            boxB.X + posB.X + boxB.Width,
            boxB.Y + posB.Y + boxB.Height
        );

        return
            a.X <= b.Z &&
            a.Z >= b.X &&
            a.Y <= b.W &&
            a.W >= b.Z;
    }

    public Vector2 Sweep(Vector2 position, Vector2 velocity, BoundingBox boundingBox)
    {
        var destX = position.X;
        var destY = position.Y;

        var incrementX = velocity.X * 0.1f;
        var incrementY = velocity.Y * 0.1f;

        var xCollision = false;

        while (incrementX != 0.0f && !xCollision)
        {
            foreach (var other in ColliderFilter.Entities)
            {
                var otherPosition = Get<Position>(other).value;
                var otherBox = Get<BoundingBox>(other);

                if (Overlaps(new Vector2(destX, position.Y), boundingBox, otherPosition, otherBox))
                {
                    xCollision = true;
                    break;
                }
            }

            if (xCollision)
                break;

            if (incrementX > 0.0f && destX + incrementX > position.X + velocity.X)
            {
                break;
            }

            else if (incrementX < 0.0f && destX + incrementX < position.X + velocity.X)
            {
                break;
            }

            destX += incrementX;
        }

        var yCollision = false;

        while (incrementY != 0.0f && !yCollision)
        {
            foreach (var other in ColliderFilter.Entities)
            {
                var otherPosition = Get<Position>(other).value;
                var otherBox = Get<BoundingBox>(other);

                if (Overlaps(new Vector2(position.X, destY), boundingBox, otherPosition, otherBox))
                {
                    yCollision = true;
                    break;
                }
            }

            if (yCollision)
                break;

            if (incrementY > 0.0f && destY + incrementY > position.Y + velocity.Y)
            {
                break;
            }

            else if (incrementY < 0.0f && destY + incrementY < position.Y + velocity.Y)
            {
                break;
            }

            destY += incrementY;
        }


        return new Vector2(destX, destY);
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
                dest = Sweep(position, velocity, Get<BoundingBox>(entity));
            }

            Set(entity, new Position(dest));
        }
    }
}