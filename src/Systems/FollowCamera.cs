
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
        if (!Some<CameraPosition>() || !Some<CameraFollows>())
            return;

        var camera = GetSingletonEntity<CameraPosition>();
        var pos = Get<CameraPosition>(camera);
        var offset = pos.Y;
        var ball = GetSingletonEntity<CameraFollows>();
        var ballPosition = Get<Position>(ball).Value;

        if (ballPosition.Y < -offset)
        {
            offset = -ballPosition.Y;
        }

        if (pos.TargetY > offset)
        {
            offset += (pos.TargetY - offset) * (float)delta.TotalSeconds;
        }
        Set(camera, new CameraPosition(offset, Get<CameraPosition>(camera).TargetY));

        foreach (var entity in FollowFilter.Entities)
        {
            var follow = Get<FollowsCamera>(entity).Y;
            var position = Get<Position>(entity).Value;
            position.Y = follow - offset;
            Set(entity, new Position(position));
        }


    }
}