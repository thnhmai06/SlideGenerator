using SlideGenerator.Settings.Entities;
using SlideGenerator.Settings.Interfaces;
using SlideGenerator.Settings.Rules;
using SlideGenerator.Settings.Settings;

namespace SlideGenerator.Settings.Services;

/// <summary>
///     Manages the loading, saving, and state of the application's <see cref="Setting" /> configuration.
/// </summary>
/// <param name="serializer">The serializer used to persist settings to disk.</param>
public sealed class SettingManager(Serializer serializer) : ISettingProvider
{
    /// <summary>
    ///     Gets the full file path where settings are stored.
    /// </summary>
    private string FilePath => NameAndPathRules.Setting.GetFilePath(serializer.FileExtension);

    /// <inheritdoc />
    public Setting Current { get; private set; } = new();

    /// <summary>
    ///     Asynchronously loads settings from the disk.
    /// </summary>
    /// <returns>True if the settings were successfully loaded; false if the file does not exist.</returns>
    public async Task<bool> Load()
    {
        if (!File.Exists(FilePath)) return false;
        
        try
        {
            var source = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            var loaded = serializer.Deserialize<Setting>(source);
            Current = loaded;
        }
        catch (Exception e)
        {
            // TODO: Log e
            await ResetToDefaults().ConfigureAwait(false);
            return false;
        }
        
        return true;
    }

    /// <summary>
    ///     Asynchronously saves the current settings to disk.
    /// </summary>
    /// <returns>True if the operation completed successfully.</returns>
    public async Task Save()
    {
        var content = serializer.Serialize(Current);
        await File.WriteAllTextAsync(FilePath, content).ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Resets the settings to their default values and persists them to disk.
    /// </summary>
    /// <returns>A task representing the reset and save operation.</returns>
    public async Task ResetToDefaults()
    {
        await Update(new Setting()).ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates the current settings state and persists it to disk.
    /// </summary>
    /// <param name="newSetting">The new settings object to apply.</param>
    /// <returns>A task representing the save operation.</returns>
    public async Task Update(Setting newSetting)
    {
        Current = newSetting;
        await Save().ConfigureAwait(false);
    }
}