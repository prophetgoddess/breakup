
using MoonTools.ECS;

namespace Ball;

public class Settings : MoonTools.ECS.System
{
    SaveGame SaveGame;
    SettingsMenuSpawner SettingsMenuSpawner;

    public Settings(World world) : base(world)
    {
        SaveGame = new SaveGame(world);
        SettingsMenuSpawner = new SettingsMenuSpawner(world);
    }

    public override void Update(TimeSpan delta)
    {
        var inputState = GetSingleton<InputState>();

        if (inputState.Dash.IsPressed)
        {
            SettingsMenuSpawner.OpenSettingsMenu();
        }
    }

}