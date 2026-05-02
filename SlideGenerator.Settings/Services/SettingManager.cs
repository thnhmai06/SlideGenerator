using SlideGenerator.Settings.Entities;
using SlideGenerator.Settings.Interfaces;
using SlideGenerator.Settings.Rules;
using SlideGenerator.Settings.Settings;

namespace SlideGenerator.Settings.Services;

/// <summary>
///     Manages the loading, saving, and state of the application's <see cref="Setting" /> configuration.
/// </summary>
public sealed class SettingManager(Serializer serializer) : ISettingProvider
{
    private string FilePath => NameAndPathRules.Setting.GetFilePath(serializer.FileExtension);

    /// <inheritdoc />
    public Setting Current { get; private set; } = new();

    public async Task<bool> Load()
    {
        if (!File.Exists(FilePath)) return false;

        var source = await File.ReadAllTextAsync(FilePath);
        var loaded = serializer.Deserialize<Setting>(source);

        Current = loaded;
        return true;
    }

    public async Task<bool> Save()
    {
        var content = serializer.Serialize(Current);
        await File.WriteAllTextAsync(FilePath, content);
        return true;
    }

    public Task<bool> Update(Setting newSetting)
    {
        Current = newSetting;
        return Save();
    }

    public async Task<bool> ResetToDefaults()
    {
        Current = new Setting();
        return await Save();
    }
}