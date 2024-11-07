
using MoonTools.ECS;

namespace Ball;

public class FollowCamera : MoonTools.ECS.System
{
    Filter FollowFilter;

    public FollowCamera(World world) : base(world)
    {
        FollowFilter = FilterBuilder.Include<FollowsCamera>().Include<Position>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        var offset = GetSingleton<CameraPosition>().Y;

        foreach (var entity in FollowFilter.Entities)
        {
            var follow = Get<FollowsCamera>(entity).Y;
            var position = Get<Position>(entity).value;
            position.Y = follow - offset;
            Set(entity, new Position(position));
        }
    }
}