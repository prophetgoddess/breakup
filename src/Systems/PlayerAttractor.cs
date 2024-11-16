

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
        var player = GetSingletonEntity<Player>();
        var playerPosition = Get<Position>(player).Value;

        foreach (var entity in PlayerAttractionFilter.Entities)
        {
            var pos = Get<Position>(entity).Value;
            var vel = Get<Velocity>(entity).Value;
            var speed = Get<MoveTowardsPlayer>(entity).Speed;

            vel += Vector2.Normalize(playerPosition - pos) * speed * 0.1f;

            vel = Vector2.Normalize(vel) * MathF.Min(vel.Length(), speed);

            Set(entity, new Velocity(vel));
        }
    }
}