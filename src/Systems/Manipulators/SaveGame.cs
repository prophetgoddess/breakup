using MoonTools.ECS;
using System.Text.Json;
using System.Text.Json.Serialization;
using Steamworks;
using MoonWorks.Input;

namespace Ball;

[JsonSerializable(typeof(SaveData))]
internal partial class SaveDataContext : JsonSerializerContext
{
}
public struct SaveData
{
    public int HighScore { get; set; }
    public float MusicVolume { get; set; }
    public float SFXVolume { get; set; }
    public bool Fullscreen { get; set; }
    public Dictionary<Actions, KeyCode> Keyboard { get; set; }
    public Dictionary<Actions, GamepadButtonCode> Gamepad { get; set; }
}

public class SaveGame : Manipulator
{
    static JsonSerializerOptions saveSerializerOptions = new JsonSerializerOptions
    {
        IncludeFields = true,
        WriteIndented = true
    };

    Dictionary<Actions, KeyCode> Keyboard = new();
    Dictionary<Actions, GamepadButtonCode> Gamepad = new();

    static SaveDataContext saveDataContext = new SaveDataContext(saveSerializerOptions);

    public SaveGame(World world) : base(world)
    {
    }

    public void Save()
    {
        var existing = Load();

        Keyboard.Clear();
        foreach (var (action, button) in Input.Keyboard)
        {
            Keyboard[action] = button.KeyCode;
        }

        Gamepad.Clear();
        foreach (var (action, button) in Input.Gamepad)
        {
            Gamepad[action] = button.Code;
        }

        var saveData = new SaveData
        {
            HighScore = Some<HighScore>() ? GetSingleton<HighScore>().Value : existing.HighScore,
            MusicVolume = GetSingleton<MusicVolume>().Value,
            SFXVolume = GetSingleton<SFXVolume>().Value,
            Fullscreen = GetSingleton<Fullscreen>().Value,
            Keyboard = Keyboard,
            Gamepad = Gamepad
        };

        var jsonString = JsonSerializer.Serialize(saveData, typeof(SaveData), saveDataContext);

        File.WriteAllText(Path.Join(AppContext.BaseDirectory, $"{SteamUser.GetSteamID()}.save"), jsonString);
    }

    public SaveData Load()
    {
        if (File.Exists(Path.Join(AppContext.BaseDirectory, $"{SteamUser.GetSteamID()}.save")))
        {
            return (SaveData)JsonSerializer.Deserialize(File.ReadAllText(Path.Join(AppContext.BaseDirectory, $"{SteamUser.GetSteamID()}.save")), typeof(SaveData), saveDataContext);
        }
        else
        {
            return new SaveData
            {
                HighScore = 0,
                MusicVolume = 0.5f,
                SFXVolume = 0.5f,
                Fullscreen = false
            };
        }

    }
}