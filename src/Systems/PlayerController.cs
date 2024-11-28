using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Input;
using MoonWorks.Math;

namespace Ball;

public class PlayerController : MoonTools.ECS.System
{

    public PlayerController(World world) : base(world)
    {
    }

    public override void Update(TimeSpan delta)
    {
        if (!Some<Player>())
            return;

        var dt = (float)delta.TotalSeconds;

        var player = GetSingletonEntity<Player>();

        var inputState = GetSingleton<InputState>();

        var movementDelta = Vector2.Zero;

        if (inputState.Left.IsDown)
        {
            movementDelta += -Vector2.UnitX;
        }
        if (inputState.Right.IsDown)
        {
            movementDelta += Vector2.UnitX;
        }

        var chargeEntity = GetSingletonEntity<Charge>();
        var charge = GetSingleton<Charge>();
        System.Console.WriteLine(inputState.Swing.IsReleased);

        if (!HasOutRelation<Spinning>(player))
        {
            if (inputState.Swing.IsDown)
            {
                Set(chargeEntity, new Charge(float.Clamp(charge.Value + dt, 0f, 1f), charge.Scale));
            }
            else if (inputState.Swing.IsReleased)
            {
                var timerEntity = CreateEntity();
                Set(timerEntity, new Timer(0.25f));
                Relate(player, timerEntity, new Spinning());

                if (HasInRelation<HeldBy>(player))
                {
                    var ball = InRelationSingleton<HeldBy>(player);
                    Unrelate<HeldBy>(ball, player);
                    Unrelate<IgnoreSolidCollision>(ball, player);
                    Set(ball, new Velocity(Vector2.UnitY * -300.0f * charge.Value));
                }
            }
            else
            {
                Set(chargeEntity, new Charge(0f, charge.Scale));
            }
        }

        if (movementDelta != Vector2.Zero)
        {
            movementDelta = Vector2.Normalize(movementDelta);
        }

        movementDelta *= 300f * (inputState.Dash.IsDown ? 2f : 1.0f);

        Set(player, new Velocity(movementDelta));

        if (HasOutRelation<Spinning>(player))
        {
            var timer = Get<Timer>(OutRelationSingleton<Spinning>(player));
            Set(player, new Orientation(MathF.PI * 2 * timer.Remaining));
        }
        else
        {
            Set(player, new Orientation(0f));
        }
    }
}