
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using MoonTools.ECS;

namespace Ball;

public class FollowCamera : MoonTools.ECS.System
{
    Filter FollowFilter;
    Filter FollowsFilter;

    public FollowCamera(World world) : base(world)
    {
        FollowFilter = FilterBuilder.Include<FollowsCamera>().Include<Position>().Build();
        FollowsFilter = FilterBuilder.Include<CameraFollows>().Include<Position>().Build();
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<CameraPosition>() || !Some<CameraFollows>())
            return;

        var camera = GetSingletonEntity<CameraPosition>();
        var pos = Get<CameraPosition>(camera);
        var offset = pos.Y;

        var highestY = 0.0f;

        foreach (var ball in FollowsFilter.Entities)
        {
            var ballPosition = Get<Position>(ball).Value;
            if (ballPosition.Y < highestY)
            {
                highestY = ballPosition.Y;
            }
        }

        if (highestY < -offset)
        {
            offset = -highestY;
        }

        if (offset >= GameplaySettings.MaxCameraY)
        {
            offset = GameplaySettings.MaxCameraY;
        }

        Set(camera, new CameraPosition(offset));

        foreach (var entity in FollowFilter.Entities)
        {
            var follow = Get<FollowsCamera>(entity).Y;
            var position = Get<Position>(entity).Value;
            position.Y = follow - offset;
            Set(entity, new Position(position));
        }


    }
}