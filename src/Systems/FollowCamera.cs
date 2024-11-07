
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
        var camera = GetSingletonEntity<CameraPosition>();
        var offset = Get<CameraPosition>(camera).Y;
        var ball = GetSingletonEntity<CameraFollows>();
        var ballPosition = Get<Position>(ball).value;


        Console.WriteLine($"y: {ballPosition.Y} offset: {offset}");
        if (ballPosition.Y < -offset)
        {
            offset = -ballPosition.Y;
        }

        Set(camera, new CameraPosition(offset));

        foreach (var entity in FollowFilter.Entities)
        {
            var follow = Get<FollowsCamera>(entity).Y;
            var position = Get<Position>(entity).value;
            position.Y = follow - offset;
            Set(entity, new Position(position));
        }


    }
}