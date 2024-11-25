

using System.Numerics;
using MoonTools.ECS;

namespace Ball;

public class PlayerAttractor : MoonTools.ECS.System
{

    Filter PlayerAttractionFilter;

    public PlayerAttractor(World world) : base(world)
    {
        PlayerAttractionFilter = FilterBuilder.Include<Position>().Include<Velocity>().Include<MoveTowardsPlayer>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<Player>())
            return;

        var player = GetSingletonEntity<Player>();

        var playerPosition = Get<Position>(player).Value;

        foreach (var entity in PlayerAttractionFilter.Entities)
        {
            if (HasOutRelation<DontMoveTowardsPlayer>(entity))
                continue;

            var pos = Get<Position>(entity).Value;
            var vel = Get<Velocity>(entity).Value;
            var speed = Get<MoveTowardsPlayer>(entity).Acceleration;

            var dir = playerPosition - pos;

            vel = Vector2.Normalize(dir) * speed;

            Set(entity, new Velocity(vel));
        }
    }
}