using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudApka;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(PrototypeSnapshot))]
internal sealed partial class PrototypeJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Keeps the prototype state in a versioned JSON file in the app's private directory.
/// The file is replaced atomically, and the previous version is kept as a backup.
/// </summary>
internal sealed class LocalStateStore
{
    private readonly object _writeLock = new();
    private readonly string _statePath;
    private readonly string _backupPath;
    private readonly string _tempPath;
    private readonly string _photoDirectory;
    private long _lastSavedSequence;

    public LocalStateStore(string appDataDirectory)
    {
        _statePath = Path.Combine(appDataDirectory, "budapka-state.json");
        _backupPath = _statePath + ".bak";
        _tempPath = _statePath + ".tmp";
        _photoDirectory = Path.Combine(appDataDirectory, "photos");
    }

    public PrototypeSnapshot? Load()
    {
        TryDelete(_tempPath);
        return TryRead(_statePath) ?? TryRead(_backupPath);
    }

    /// <summary>Writes the snapshot unless a newer one has already been saved.</summary>
    public bool Save(PrototypeSnapshot snapshot, long sequence)
    {
        lock (_writeLock)
        {
            if (sequence <= _lastSavedSequence)
            {
                return true;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
                using (var stream = File.Create(_tempPath))
                {
                    JsonSerializer.Serialize(stream, snapshot,
                        PrototypeJsonContext.Default.PrototypeSnapshot);
                    stream.Flush(true);
                }

                if (File.Exists(_statePath))
                {
                    File.Replace(_tempPath, _statePath, _backupPath);
                }
                else
                {
                    File.Move(_tempPath, _statePath);
                }

                _lastSavedSequence = sequence;
                return true;
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException)
            {
                Debug.WriteLine($"BudApka state was not saved: {exception.Message}");
                TryDelete(_tempPath);
                return false;
            }
        }
    }

    /// <summary>
    /// Copies a picked photo into the private directory, because the picker's path
    /// may stop working after the app restarts.
    /// </summary>
    public async Task<string> ImportPhotoAsync(FileResult file)
    {
        Directory.CreateDirectory(_photoDirectory);
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension.Length == 0)
        {
            extension = ".jpg";
        }

        string path = Path.Combine(_photoDirectory, Guid.NewGuid().ToString("N") + extension);
        string tempPath = path + ".tmp";
        try
        {
            await using (var source = await file.OpenReadAsync())
            await using (var target = File.Create(tempPath))
            {
                await source.CopyToAsync(target);
            }

            File.Move(tempPath, path);
            return path;
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    public void DeleteUnusedPhotos(IEnumerable<string> usedPaths)
    {
        if (!Directory.Exists(_photoDirectory))
        {
            return;
        }

        var used = usedPaths.Select(Path.GetFullPath).ToHashSet(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(_photoDirectory))
        {
            if (!used.Contains(Path.GetFullPath(path)))
            {
                TryDelete(path);
            }
        }
    }

    private static PrototypeSnapshot? TryRead(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var snapshot = JsonSerializer.Deserialize(stream,
                PrototypeJsonContext.Default.PrototypeSnapshot);
            if (snapshot?.Version != PrototypeSnapshot.CurrentVersion)
            {
                Debug.WriteLine($"BudApka state {path} has an unsupported version.");
                return null;
            }

            return snapshot;
        }
        catch (Exception exception) when (exception is JsonException or IOException
            or UnauthorizedAccessException or NotSupportedException)
        {
            Debug.WriteLine($"BudApka state {path} was not read: {exception.Message}");
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException)
        {
            Debug.WriteLine($"BudApka file {path} was not deleted: {exception.Message}");
        }
    }
}
