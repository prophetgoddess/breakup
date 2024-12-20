using MoonTools.ECS;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

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
}

public class SaveGame : Manipulator
{
    static string FilePath = Path.Join(AppContext.BaseDirectory, "save");

    static JsonSerializerOptions saveSerializerOptions = new JsonSerializerOptions
    {
        IncludeFields = true,
        WriteIndented = true
    };

    static SaveDataContext saveDataContext = new SaveDataContext(saveSerializerOptions);

    public SaveGame(World world) : base(world)
    {
    }

    public void Save()
    {
        var existing = Load();

        var saveData = new SaveData
        {
            HighScore = Some<HighScore>() ? GetSingleton<HighScore>().Value : existing.HighScore,
            MusicVolume = GetSingleton<MusicVolume>().Value,
            SFXVolume = GetSingleton<SFXVolume>().Value
        };

        var jsonString = JsonSerializer.Serialize(saveData, typeof(SaveData), saveDataContext);

        File.WriteAllText(FilePath, jsonString);
    }

    public SaveData Load()
    {
        if (File.Exists(FilePath))
        {
            return (SaveData)JsonSerializer.Deserialize(File.ReadAllText(FilePath), typeof(SaveData), saveDataContext);
        }
        else
        {
            return new SaveData
            {
                HighScore = 0,
                MusicVolume = 0.5f,
                SFXVolume = 0.5f
            };
        }

    }
}