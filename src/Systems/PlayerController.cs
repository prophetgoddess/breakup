using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Input;

namespace Ball;

public class PlayerController : MoonTools.ECS.System
{

    public PlayerController(World world) : base(world)
    {
    }

    public override void Update(TimeSpan delta)
    {
        var player = GetSingletonEntity<Player>();

        var inputState = Get<InputState>(player);

        var movementDelta = Vector2.Zero;


        if (inputState.Left.IsDown)
        {
            movementDelta += -Vector2.UnitX;
        }
        if (inputState.Right.IsDown)
        {
            movementDelta += Vector2.UnitX;
        }

        // if (inputState.Swing.IsPressed && !HasOutRelation<Spinning>(player))
        // {
        //     var timerEntity = CreateEntity();
        //     Set(timerEntity, new Timer(0.25f));
        //     Relate(player, timerEntity, new Spinning());
        // }

        if (movementDelta != Vector2.Zero)
        {
            movementDelta = Vector2.Normalize(movementDelta);
        }

        movementDelta *= 500f;

        Set(player, new Velocity(movementDelta));

        if (HasOutRelation<Spinning>(player))
        {
            var timer = Get<Timer>(OutRelationSingleton<Spinning>(player));
            Set(player, new Orientation(timer.Remaining * MathF.PI * 2));
        }
        else
        {
            Set(player, new Orientation(0f));
        }
    }
}