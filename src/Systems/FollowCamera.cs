
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using MoonTools.ECS;
using MoonWorks.Input;

namespace Ball;

public class FollowCamera : MoonTools.ECS.System
{
    Filter FollowFilter;
    Filter FollowsFilter;
    Scorer Scorer;

    Inputs Inputs;

    public FollowCamera(World world, Inputs inputs) : base(world)
    {
        Inputs = inputs;
        FollowFilter = FilterBuilder.Include<FollowsCamera>().Include<Position>().Build();
        FollowsFilter = FilterBuilder.Include<CameraFollows>().Include<Position>().Build();
        Scorer = new Scorer(world);
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<CameraPosition>() || !Some<CameraFollows>())
            return;

        var camera = GetSingletonEntity<CameraPosition>();
        var pos = Get<CameraPosition>(camera);
        var offset = pos.Y;

#if DEBUG
        var inputState = GetSingleton<InputState>();
        if (Inputs.Keyboard.IsHeld(KeyCode.W))
        {
            offset += (float)delta.TotalSeconds * 100.0f;
        }
#endif
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
            Scorer.AddScore((int)Math.Abs(highestY) - (int)Math.Abs(offset));
            offset = -highestY;
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