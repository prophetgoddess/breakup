
using System.Numerics;
using MoonTools.ECS;
using MoonWorks;

namespace Ball;

public class Settings : MoonTools.ECS.System
{
    SaveGame SaveGame;
    SettingsMenuSpawner SettingsMenuSpawner;
    MainMenuSpawner MainMenuSpawner;
    PauseMenuSpawner PauseMenuSpawner;
    Window Window;

    public Settings(World world, Window window) : base(world)
    {
        Window = window;
        SaveGame = new SaveGame(world);
        SettingsMenuSpawner = new SettingsMenuSpawner(world);
        MainMenuSpawner = new MainMenuSpawner(world);
        PauseMenuSpawner = new PauseMenuSpawner(world);
    }

    Entity CreateSelector()
    {
        var selector = CreateEntity();
        Set(selector, new Position(Vector2.Zero));
        Set(selector, new Model(Content.Models.Triangle.ID));
        Set(selector, new Orientation(MathF.PI * -0.5f));
        Set(selector, new Depth(0.1f));
        Set(selector, new FollowsCamera(0f));
        Set(selector, new Selector());
        Set(selector, new KeepOpacityWhenPaused());
        Set(selector, new DestroyOnStateTransition());

        return selector;
    }

    void AdjustSetting(Entity e, int Amount)
    {
        var setting = OutRelationSingleton<SettingControls>(e);
        var display = OutRelationSingleton<SettingDisplay>(e);

        if (Has<SFXVolume>(setting) || Has<MusicVolume>(setting))
        {
            var intVolume = (int)((Has<SFXVolume>(setting) ? Get<SFXVolume>(setting).Value : Get<MusicVolume>(setting).Value) * Audio.MaxVolume);
            intVolume += Amount;
            intVolume = int.Clamp(intVolume, 0, Audio.MaxVolume);
            var volume = float.Clamp(intVolume / (float)Audio.MaxVolume, 0, 1f);

            if (Has<SFXVolume>(setting))
            {
                Set(setting, new SFXVolume(volume));
                Set(CreateEntity(), new PlayOnce(Stores.SFXStorage.GetID(Content.SFX.boing)));
            }
            else
            {
                Set(setting, new MusicVolume(volume));
            }

            Set(display,
             new Text(
                Fonts.HeaderFont,
                Fonts.PromptSize,
                Stores.TextStorage.GetID(new string('|', (int)MathF.Ceiling(intVolume))),
                MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        }
        if (Has<Fullscreen>(setting))
        {
            var fullscreen = GetSingleton<Fullscreen>().Value;
            fullscreen = !fullscreen;
            Set(GetSingletonEntity<Fullscreen>(), new Fullscreen(fullscreen));

            Window.SetScreenMode(fullscreen ? ScreenMode.Fullscreen : ScreenMode.Windowed);

            Set(display,
                new Text(
                    Fonts.HeaderFont,
                    Fonts.PromptSize,
                    Stores.TextStorage.GetID($"{fullscreen}"),
                    MoonWorks.Graphics.Font.HorizontalAlignment.Left,
                    MoonWorks.Graphics.Font.VerticalAlignment.Middle));
        }

        SaveGame.Save();
    }

    public override void Update(TimeSpan delta)
    {
        var inputState = GetSingleton<InputState>();

        if (!Some<Setting>() && !Some<UpgradeOption>())
        {
            if (!Some<Player>() || Some<Pause>())
            {
                if (inputState.Swing.IsPressed)
                {
                    SettingsMenuSpawner.OpenSettingsMenu();
                }
            }
        }
        else if (Some<Setting>())
        {
            if (inputState.Swing.IsPressed)
            {
                SettingsMenuSpawner.CloseSettingsMenu();

                if (!Some<Player>())
                {
                    MainMenuSpawner.OpenMainMenu();
                }
                else
                {
                    PauseMenuSpawner.OpenPauseMenu();
                }
            }

            if (Some<Selected>())
            {
                var selected = GetSingletonEntity<Selected>();
                var selector = Some<Selector>() ? GetSingletonEntity<Selector>() : CreateSelector();

                Set(selector, new Position(new Vector2(Get<Position>(selected).Value.X - 10f, Get<Position>(selected).Value.Y)));
                Set(selector, new FollowsCamera(Get<Position>(selected).Value.Y));

                if (inputState.Down.IsPressed)
                {
                    if (HasOutRelation<VerticalConnection>(selected))
                    {
                        Remove<Flicker>(selected);
                        Remove<Selected>(selected);
                        Set(OutRelationSingleton<VerticalConnection>(selected), new Selected());
                    }
                }
                if (inputState.Up.IsPressed)
                {
                    if (HasInRelation<VerticalConnection>(selected))
                    {
                        Remove<Flicker>(selected);
                        Remove<Selected>(selected);
                        Set(InRelationSingleton<VerticalConnection>(selected), new Selected());
                    }
                }

                if (inputState.Left.IsPressed)
                {
                    AdjustSetting(selected, -1);
                }
                if (inputState.Right.IsPressed)
                {
                    AdjustSetting(selected, 1);
                }
            }

        }
    }

}