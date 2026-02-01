using BetterAmongUs.Helpers;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Json;

/// <summary>
/// Represents a compressed JSON file for storing game settings with GZIP compression.
/// </summary>
internal sealed class BetterGameSettingsFile : AbstractJsonFile
{
    /// <summary>
    /// Gets the file path for the game settings file.
    /// </summary>
    internal override string FilePath => BetterDataManager.SettingsFile;

    /// <summary>
    /// Loads the settings file and converts JSON elements to their appropriate types.
    /// </summary>
    /// <returns>True if loading was successful, false otherwise.</returns>
    protected override bool Load()
    {
        var success = base.Load();
        if (success)
        {
            foreach (var kvp in Settings.ToArray())
            {
                if (kvp.Value is JsonElement jsonElement)
                {
                    try
                    {
                        Settings[kvp.Key] = jsonElement.ValueKind switch
                        {
                            JsonValueKind.Number when jsonElement.TryGetInt32(out int intValue) => intValue,
                            JsonValueKind.Number when jsonElement.TryGetSingle(out float floatValue) => floatValue,
                            JsonValueKind.Number when jsonElement.TryGetByte(out byte byteValue) => byteValue,
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => jsonElement.GetString(),
                            _ => throw new NotSupportedException($"Unsupported JSON type: {jsonElement.ValueKind}")
                        };
                    }
                    catch (Exception ex)
                    {
                        Logger_.Error($"Failed to convert JSON element for key {kvp.Key}: {ex.Message}");
                    }
                }
            }
        }
        return success;
    }

    /// <summary>
    /// Writes JSON data to the file with GZIP compression and base64 encoding.
    /// </summary>
    /// <param name="json">The JSON string to write to the file.</param>
    protected override void WriteToFile(string json)
    {
        var jsonDoc = JsonDocument.Parse(json);
        var settingsDict = jsonDoc.RootElement.GetProperty(nameof(Settings));
        var sb = new StringBuilder();

        foreach (var kvp in settingsDict.EnumerateObject())
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append(kvp.Name).Append('/').Append(kvp.Value.GetRawText());
        }

        byte[] flattenedData = Encoding.UTF8.GetBytes(sb.ToString());
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
        {
            gzip.Write(flattenedData, 0, flattenedData.Length);
        }
        ms.Position = 0;
        File.WriteAllText(FilePath, Convert.ToBase64String(ms.ToArray()));
    }

    /// <summary>
    /// Reads compressed JSON data from the file and decompresses it.
    /// </summary>
    /// <returns>The decompressed JSON string.</returns>
    protected override string ReadFromFile()
    {
        byte[] compressedBytes = Convert.FromBase64String(File.ReadAllText(FilePath).Trim());
        using var ms = new MemoryStream(compressedBytes);
        using var gzip = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        string flattenedData = reader.ReadToEnd();

        var settingsDict = new Dictionary<string, JsonElement>();
        foreach (var pair in flattenedData.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('/', 2);
            if (parts.Length == 2)
            {
                using var doc = JsonDocument.Parse(parts[1]);
                settingsDict[parts[0]] = doc.RootElement.Clone();
            }
        }

        var resultDict = new Dictionary<int, object?>();
        foreach (var kvp in settingsDict)
        {
            if (int.TryParse(kvp.Key, out int key))
            {
                resultDict[key] = kvp.Value.Deserialize<object?>();
            }
        }

        return JsonSerializer.Serialize(new { Settings = resultDict });
    }

    /// <summary>
    /// Gets the dictionary of game settings with integer keys and various value types.
    /// </summary>
    [JsonPropertyName(nameof(Settings))]
    public Dictionary<int, object?> Settings { get; set; } = [];
}