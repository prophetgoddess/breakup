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
#if DEBUG
    public ButtonState Restart { get; set; }
#endif
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
    public VirtualButton Start { get; set; } = new EmptyButton();
    public VirtualButton Dash { get; set; } = new EmptyButton();
}

public class Input : MoonTools.ECS.System
{
    static Inputs Inputs { get; set; }

    Actions RebindState = Actions.Left;

    static ControlSet Keyboard = new ControlSet();
    static ControlSet Gamepad = new ControlSet();

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
        Keyboard.Dash = Inputs.Keyboard.Button(KeyCode.LeftShift);

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

        var rebinding = Some<RebindControls>() && GetSingleton<RebindControls>().Rebinding == true;

        if (rebinding)
        {
            var display = InRelationSingleton<SettingControls>(GetSingletonEntity<RebindControls>());

            Set(display,
                new Text(
                    Fonts.HeaderFont,
                    Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
                    Stores.TextStorage.GetID("Press" + RebindState.ToString()),
                    MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                    MoonWorks.Graphics.Font.VerticalAlignment.Middle));

            if (Inputs.AnyPressed)
            {
                var pressed = Inputs.AnyPressedButton;

                switch (RebindState)
                {
                    case Actions.Left:
                        if (Inputs.GetGamepad(0).AnyPressed)
                        {
                            Gamepad.Left = pressed;
                        }
                        else if (Inputs.Keyboard.AnyPressed)
                        {
                            Keyboard.Left = pressed;
                        }
                        RebindState++;
                        break;
                    case Actions.Right:
                        if (Inputs.GetGamepad(0).AnyPressed)
                        {
                            Gamepad.Right = pressed;
                        }
                        else if (Inputs.Keyboard.AnyPressed)
                        {
                            Keyboard.Right = pressed;
                        }
                        RebindState++;
                        break;
                    case Actions.Up:
                        if (Inputs.GetGamepad(0).AnyPressed)
                        {
                            Gamepad.Up = pressed;
                        }
                        else if (Inputs.Keyboard.AnyPressed)
                        {
                            Keyboard.Up = pressed;
                        }
                        RebindState++;
                        break;
                    case Actions.Down:
                        if (Inputs.GetGamepad(0).AnyPressed)
                        {
                            Gamepad.Down = pressed;
                        }
                        else if (Inputs.Keyboard.AnyPressed)
                        {
                            Keyboard.Down = pressed;
                        }
                        RebindState++;
                        break;
                    case Actions.Launch:
                        if (Inputs.GetGamepad(0).AnyPressed)
                        {
                            Gamepad.Launch = pressed;
                        }
                        else if (Inputs.Keyboard.AnyPressed)
                        {
                            Keyboard.Launch = pressed;
                        }
                        RebindState++;
                        break;
                    case Actions.Start:
                        if (Inputs.GetGamepad(0).AnyPressed)
                        {
                            Gamepad.Start = pressed;
                        }
                        else if (Inputs.Keyboard.AnyPressed)
                        {
                            Keyboard.Start = pressed;
                        }
                        RebindState++;
                        break;
                    case Actions.Dash:
                        if (Inputs.GetGamepad(0).AnyPressed)
                        {
                            Gamepad.Dash = pressed;
                        }
                        else if (Inputs.Keyboard.AnyPressed)
                        {
                            Keyboard.Dash = pressed;
                        }
                        RebindState++;
                        break;
                    default:
                        break;
                }
            }

            if (RebindState > Actions.Dash)
            {
                RebindState = 0;
                Set(GetSingletonEntity<RebindControls>(), new RebindControls(false));

                Set(display,
                    new Text(
                        Fonts.HeaderFont,
                        Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
                        Stores.TextStorage.GetID("rebind controls"),
                        MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                        MoonWorks.Graphics.Font.VerticalAlignment.Middle));
            }
        }
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
