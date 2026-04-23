using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Settings.Abstractions;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Settings.Rules;
using SlideGenerator.Domain.Settings.Entities;

namespace SlideGenerator.Application.Settings.Services;

/// <summary>
///     Manages the loading, saving, and state of the application's <see cref="Setting" /> configuration.
/// </summary>
/// <param name="textFileRegistry">The registry for acquiring thread-safe access to text files.</param>
/// <param name="serializer">The serializer used to parse and format the configuration data.</param>
public sealed class SettingManager(FileRegistry<ITextFile> textFileRegistry, ISerializer serializer)
    : ISettingProvider
{
    /// <summary>
    ///     Gets the absolute file path where the settings are stored.
    /// </summary>
    public string FilePath =>
        Path.Combine(AppContext.BaseDirectory, NamingRules.SettingFileName + serializer.FileExtension);

    /// <inheritdoc />
    public Setting Current { get; private set; } = new();

    /// <summary>
    ///     Asynchronously loads the configuration from the physical file into <see cref="Current" />.
    /// </summary>
    /// <returns><see langword="true" /> if the settings were loaded successfully; otherwise, <see langword="false" />.</returns>
    public async Task<bool> Load()
    {
        try
        {
            // Acquire is safe here: TextFileRegistry uses maxCount = int.MaxValue (no blocking).
            using var textFileLease = await textFileRegistry.Acquire(FilePath, false);
            var textFile = textFileLease.Value;
            var source = textFile.Read();
            var loaded = serializer.Deserialize<Setting>(source);

            Current = loaded;
            return true;
        }
        catch
        {
            // TODO: log the error
        }

        return false;
    }

    /// <summary>
    ///     Asynchronously saves the <see cref="Current" /> configuration to the physical file.
    /// </summary>
    /// <returns><see langword="true" /> if the settings were saved successfully; otherwise, <see langword="false" />.</returns>
    public async Task<bool> Save()
    {
        try
        {
            using var textFileLease = await textFileRegistry.Acquire(FilePath, true);
            var textFile = textFileLease.Value;
            var content = serializer.Serialize(Current);
            textFile.Write(content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Resets the <see cref="Current" /> configuration to its default values and saves it to disk.
    /// </summary>
    /// <returns><see langword="true" /> if the reset and save operations were successful; otherwise, <see langword="false" />.</returns>
    public async Task<bool> ResetToDefaults()
    {
        Current = new Setting();
        return await Save();
    }
}
