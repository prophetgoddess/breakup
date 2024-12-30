using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Input;

namespace Ball;

public struct InputState
{
    public ButtonState Left { get; set; }
    public ButtonState Right { get; set; }
    public ButtonState Up { get; set; }
    public ButtonState Down { get; set; }
    public ButtonState Swing { get; set; }
    public ButtonState Restart { get; set; }
    public ButtonState Start { get; set; }
    public ButtonState Dash { get; set; }
    public ButtonState Slow { get; set; }
}

public class ControlSet
{
    public VirtualButton Left { get; set; } = new EmptyButton();
    public VirtualButton Right { get; set; } = new EmptyButton();
    public VirtualButton Up { get; set; } = new EmptyButton();
    public VirtualButton Down { get; set; } = new EmptyButton();
    public VirtualButton Swing { get; set; } = new EmptyButton();
#if DEBUG
    public VirtualButton Restart { get; set; } = new EmptyButton();
#endif
    public VirtualButton Start { get; set; }
    public VirtualButton Dash { get; set; }
    public VirtualButton Slow { get; set; }
}

public class Input : MoonTools.ECS.System
{
    Inputs Inputs { get; }

    ControlSet Keyboard = new ControlSet();
    ControlSet Gamepad = new ControlSet();

    public Input(World world, Inputs inputs) : base(world)
    {
        Inputs = inputs;

        Keyboard.Up = Inputs.Keyboard.Button(KeyCode.Up);
        Keyboard.Down = Inputs.Keyboard.Button(KeyCode.Down);
        Keyboard.Left = Inputs.Keyboard.Button(KeyCode.Left);
        Keyboard.Right = Inputs.Keyboard.Button(KeyCode.Right);
        Keyboard.Swing = Inputs.Keyboard.Button(KeyCode.Space);
        Keyboard.Start = Inputs.Keyboard.Button(KeyCode.Return);
#if DEBUG
        Keyboard.Restart = Inputs.Keyboard.Button(KeyCode.R);
#endif
        Keyboard.Dash = Inputs.Keyboard.Button(KeyCode.LeftControl);
        Keyboard.Slow = Inputs.Keyboard.Button(KeyCode.LeftShift);

        Gamepad.Up = Inputs.GetGamepad(0).DpadUp;
        Gamepad.Down = Inputs.GetGamepad(0).DpadDown;
        Gamepad.Left = Inputs.GetGamepad(0).DpadLeft;
        Gamepad.Right = Inputs.GetGamepad(0).DpadRight;
        Gamepad.Swing = Inputs.GetGamepad(0).A;
        Gamepad.Start = Inputs.GetGamepad(0).Start;
#if DEBUG
        Gamepad.Restart = Inputs.GetGamepad(0).Guide;
#endif
        Gamepad.Dash = Inputs.GetGamepad(0).RightShoulder;
        Gamepad.Slow = Inputs.GetGamepad(0).LeftShoulder;

        var inputEntity = CreateEntity();
        Set(inputEntity, InputState(Keyboard, Gamepad));
    }

    public override void Update(TimeSpan delta)
    {

        InputState inputState = InputState(Keyboard, Gamepad);
        Set(GetSingletonEntity<InputState>(), inputState);

    }

    private static InputState InputState(ControlSet controlSet, ControlSet altControlSet)
    {
        return new InputState
        {
            Left = controlSet.Left.State | altControlSet.Left.State,
            Right = controlSet.Right.State | altControlSet.Right.State,
            Up = controlSet.Up.State | altControlSet.Up.State,
            Down = controlSet.Down.State | altControlSet.Down.State,
            Swing = controlSet.Swing.State | altControlSet.Swing.State,
            Start = controlSet.Start.State | altControlSet.Start.State,
#if DEBUG
            Restart = controlSet.Restart.State | altControlSet.Restart.State,
#endif
            Dash = controlSet.Dash.State | altControlSet.Dash.State,
            Slow = controlSet.Slow.State | altControlSet.Slow.State
        };
    }
}
