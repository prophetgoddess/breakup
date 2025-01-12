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
    Dash,
#if DEBUG
    Restart
#endif
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

public class Input : MoonTools.ECS.System
{
    static Inputs Inputs { get; set; }

    Actions RebindState = (Actions)0;

    SaveGame SaveGame;

    public static Dictionary<Actions, KeyboardButton> Keyboard;
    public static Dictionary<Actions, GamepadButton> Gamepad;

    public Input(World world, Inputs inputs) : base(world)
    {
        Inputs = inputs;

        SaveGame = new SaveGame(world);
        var save = SaveGame.Load();
        Keyboard = new();


        if (save.Keyboard == null)
        {
            Keyboard[Actions.Up] = Inputs.Keyboard.Button(KeyCode.Up);
            Keyboard[Actions.Down] = Inputs.Keyboard.Button(KeyCode.Down);
            Keyboard[Actions.Left] = Inputs.Keyboard.Button(KeyCode.Left);
            Keyboard[Actions.Right] = Inputs.Keyboard.Button(KeyCode.Right);
            Keyboard[Actions.Launch] = Inputs.Keyboard.Button(KeyCode.Space);
            Keyboard[Actions.Start] = Inputs.Keyboard.Button(KeyCode.Return);
#if DEBUG
            Keyboard[Actions.Restart] = Inputs.Keyboard.Button(KeyCode.R);
#endif
            Keyboard[Actions.Dash] = Inputs.Keyboard.Button(KeyCode.LeftShift);
        }
        else
        {
            foreach (var (action, code) in save.Keyboard)
            {
                Keyboard[action] = Inputs.Keyboard.Button(code);
            }
        }

        Gamepad = new();

        if (save.Gamepad == null)
        {
            Gamepad[Actions.Up] = Inputs.GetGamepad(0).DpadUp;
            Gamepad[Actions.Down] = Inputs.GetGamepad(0).DpadDown;
            Gamepad[Actions.Left] = Inputs.GetGamepad(0).DpadLeft;
            Gamepad[Actions.Right] = Inputs.GetGamepad(0).DpadRight;
            Gamepad[Actions.Launch] = Inputs.GetGamepad(0).A;
            Gamepad[Actions.Start] = Inputs.GetGamepad(0).Start;
#if DEBUG
            Gamepad[Actions.Restart] = Inputs.GetGamepad(0).Guide;
#endif
            Gamepad[Actions.Dash] = Inputs.GetGamepad(0).RightShoulder;
        }
        else
        {
            foreach (var (action, code) in save.Gamepad)
            {
                Gamepad[action] = Inputs.GetGamepad(0).Button(code);
            }
        }

        var inputEntity = CreateEntity();
        Set(inputEntity, InputState());
    }

    public override void Update(TimeSpan delta)
    {
        InputState inputState = InputState();
        Set(GetSingletonEntity<InputState>(), inputState);

        var rebinding = Some<RebindControls>() && GetSingleton<RebindControls>().Rebinding == true;

        if (rebinding)
        {
            var display = InRelationSingleton<SettingControls>(GetSingletonEntity<RebindControls>());

            Set(display,
                new Text(
                    Fonts.HeaderFont,
                    Some<Player>() ? Fonts.PromptSize : Fonts.BodySize,
                    Stores.TextStorage.GetID($"Press {RebindState} (Current: {Keyboard[RebindState].KeyCode} | {Gamepad[RebindState].Code})"),
                    MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                    MoonWorks.Graphics.Font.VerticalAlignment.Middle));

            if (Inputs.AnyPressed)
            {
                if (Inputs.Keyboard.AnyPressed)
                {
                    Keyboard[RebindState] = Inputs.Keyboard.AnyPressedButton;
                }
                if (Inputs.GetGamepad(0).AnyPressed)
                {
                    Gamepad[RebindState] = (GamepadButton)Inputs.GetGamepad(0).AnyPressedButton;
                }
                RebindState++;
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

                SaveGame.Save();
            }
        }
    }

    private static InputState InputState()
    {
        return new InputState
        {
            Left = Keyboard[Actions.Left].State | Gamepad[Actions.Left].State,
            Right = Keyboard[Actions.Right].State | Gamepad[Actions.Right].State,
            Up = Keyboard[Actions.Up].State | Gamepad[Actions.Up].State,
            Down = Keyboard[Actions.Down].State | Gamepad[Actions.Down].State,
            Launch = Keyboard[Actions.Launch].State | Gamepad[Actions.Launch].State,
            Start = Keyboard[Actions.Start].State | Gamepad[Actions.Start].State,
#if DEBUG
            Restart = Keyboard[Actions.Restart].State | Gamepad[Actions.Restart].State,
#endif
            Dash = Keyboard[Actions.Dash].State | Gamepad[Actions.Dash].State,
        };
    }
}
