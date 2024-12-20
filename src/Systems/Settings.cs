
using MoonTools.ECS;

namespace Ball;

public class Settings : MoonTools.ECS.System
{
    SaveGame SaveGame;
    SettingsMenuSpawner SettingsMenuSpawner;
    MainMenuSpawner MainMenuSpawner;
    PauseMenuSpawner PauseMenuSpawner;

    public Settings(World world) : base(world)
    {
        SaveGame = new SaveGame(world);
        SettingsMenuSpawner = new SettingsMenuSpawner(world);
        MainMenuSpawner = new MainMenuSpawner(world);
        PauseMenuSpawner = new PauseMenuSpawner(world);
    }

    public override void Update(TimeSpan delta)
    {
        var inputState = GetSingleton<InputState>();

        if (!Some<SettingsMenu>())
        {
            if (!Some<Player>() || Some<Pause>())
            {
                if (inputState.Swing.IsPressed)
                {
                    SettingsMenuSpawner.OpenSettingsMenu();
                }
            }
        }
        else if (Some<SettingsMenu>())
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
        }
    }

}