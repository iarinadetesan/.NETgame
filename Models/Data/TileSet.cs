using System.Text.Json.Serialization;

namespace TheAdventure.Models.Data;

public class TileSet
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("tilecount")]
    public int? TileCount { get; set; }

    [JsonPropertyName("tilewidth")]
    public int? TileWidth { get; set; }

    [JsonPropertyName("tileheight")]
    public int? TileHeight { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; } = "";

    [JsonPropertyName("imagewidth")]
    public int? ImageWidth { get; set; }

    [JsonPropertyName("imageheight")]
    public int? ImageHeight { get; set; }

    [JsonPropertyName("columns")]
    public int? Columns { get; set; }

    [JsonIgnore]
    public int TextureId { get; set; } = -1;
}

public class TileSetReference
{
    [JsonPropertyName("firstgid")]
    public int? FirstGID { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";
}