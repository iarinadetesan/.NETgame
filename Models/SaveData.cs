using System.Text.Json;

namespace TheAdventure.Models;

public class SaveData
{
    public int HighScore { get; set; }
    public int Money { get; set; }

    public static SaveData Load(string path)
    {
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<SaveData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new SaveData();
    }

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }
}
