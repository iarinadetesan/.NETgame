using System.Text.Json.Serialization;

namespace TheAdventure.Models.Data;

public class Layer
{
    [JsonPropertyName("data")]
    public List<int?> Data { get; set; } = new();

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("objects")]
    public List<TiledObject> Objects { get; set; } = new();

    // ... id, opacity, visible, x, y, type
}