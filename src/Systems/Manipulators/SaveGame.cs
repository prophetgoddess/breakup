using MoonTools.ECS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ball;

[JsonSerializable(typeof(SaveData))]
internal partial class SaveDataContext : JsonSerializerContext
{
}

struct SaveData
{
    public int HighScore { get; set; }
}

public class SaveGame : Manipulator
{
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
        var saveData = new SaveData
        {
            HighScore = GetSingleton<HighScore>().Value
        };

        var jsonString = JsonSerializer.Serialize(saveData, typeof(SaveData), saveDataContext);

        System.IO.File.WriteAllText(Path.Join(AppContext.BaseDirectory, "data.json"), jsonString);
    }

    public void Load()
    {

    }
}