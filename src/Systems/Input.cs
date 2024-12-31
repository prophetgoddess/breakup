using MoonTools.ECS;
using MoonWorks;
using MoonWorks.Input;

namespace Ball;

public enum Actions
{
    Left,
    Right,
    Up,
    Down,
    Launch,
    Restart,
    Start,
    Dash
}

public struct InputState
{
    public ButtonState Left { get; set; }
    public ButtonState Right { get; set; }
    public ButtonState Up { get; set; }
    public ButtonState Down { get; set; }
    public ButtonState Launch { get; set; }
    public ButtonState Restart { get; set; }
    public ButtonState Start { get; set; }
    public ButtonState Dash { get; set; }
}

public class ControlSet
{
    public VirtualButton Left { get; set; } = new EmptyButton();
    public VirtualButton Right { get; set; } = new EmptyButton();
    public VirtualButton Up { get; set; } = new EmptyButton();
    public VirtualButton Down { get; set; } = new EmptyButton();
    public VirtualButton Launch { get; set; } = new EmptyButton();
#if DEBUG
    public VirtualButton Restart { get; set; } = new EmptyButton();
#endif
    public VirtualButton Start { get; set; }
    public VirtualButton Dash { get; set; }
}

public class Input : MoonTools.ECS.System
{
    static Inputs Inputs { get; set; }

    static ControlSet Keyboard = new ControlSet();
    static ControlSet Gamepad = new ControlSet();

    public static string GetButtonName(Actions action)
    {
        return action switch
        {
            Actions.Left => throw new NotImplementedException(),
            Actions.Right => throw new NotImplementedException(),
            Actions.Up => throw new NotImplementedException(),
            Actions.Down => throw new NotImplementedException(),
            Actions.Launch => Inputs.GamepadExists(0) ? Gamepad.Launch.ToString() : Keyboard.Launch.ToString(),
            Actions.Restart => throw new NotImplementedException(),
            Actions.Start => Inputs.GamepadExists(0) ? Gamepad.Start.ToString() : Keyboard.Start.ToString(),
            Actions.Dash => throw new NotImplementedException(),
            _ => ""
        };
    }

    public Input(World world, Inputs inputs) : base(world)
    {
        Inputs = inputs;

        Keyboard.Up = Inputs.Keyboard.Button(KeyCode.Up);
        Keyboard.Down = Inputs.Keyboard.Button(KeyCode.Down);
        Keyboard.Left = Inputs.Keyboard.Button(KeyCode.Left);
        Keyboard.Right = Inputs.Keyboard.Button(KeyCode.Right);
        Keyboard.Launch = Inputs.Keyboard.Button(KeyCode.Space);
        Keyboard.Start = Inputs.Keyboard.Button(KeyCode.Return);
#if DEBUG
        Keyboard.Restart = Inputs.Keyboard.Button(KeyCode.R);
#endif
        Keyboard.Dash = Inputs.Keyboard.Button(KeyCode.LeftControl);

        Gamepad.Up = Inputs.GetGamepad(0).DpadUp;
        Gamepad.Down = Inputs.GetGamepad(0).DpadDown;
        Gamepad.Left = Inputs.GetGamepad(0).DpadLeft;
        Gamepad.Right = Inputs.GetGamepad(0).DpadRight;
        Gamepad.Launch = Inputs.GetGamepad(0).A;
        Gamepad.Start = Inputs.GetGamepad(0).Start;
#if DEBUG
        Gamepad.Restart = Inputs.GetGamepad(0).Guide;
#endif
        Gamepad.Dash = Inputs.GetGamepad(0).RightShoulder;

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
            Launch = controlSet.Launch.State | altControlSet.Launch.State,
            Start = controlSet.Start.State | altControlSet.Start.State,
#if DEBUG
            Restart = controlSet.Restart.State | altControlSet.Restart.State,
#endif
            Dash = controlSet.Dash.State | altControlSet.Dash.State,
        };
    }
}
