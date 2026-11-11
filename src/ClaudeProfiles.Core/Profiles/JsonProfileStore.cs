using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeProfiles.Core.Profiles;

public sealed class JsonProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public JsonProfileStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<ClaudeProfile>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using FileStream stream = File.OpenRead(_filePath);

        try
        {
            StoredProfiles? stored = await JsonSerializer.DeserializeAsync<StoredProfiles>(stream, SerializerOptions, cancellationToken);
            return stored?.Profiles ?? [];
        }
        catch (JsonException ex)
        {
            throw new ProfileStoreCorruptedException(_filePath, ex);
        }
    }

    public async Task SaveAsync(IReadOnlyList<ClaudeProfile> profiles, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = _filePath + ".tmp";

        await using (FileStream stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, new StoredProfiles(profiles), SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private sealed record StoredProfiles(IReadOnlyList<ClaudeProfile>? Profiles);
}
