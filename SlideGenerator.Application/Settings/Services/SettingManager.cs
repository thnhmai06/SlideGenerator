using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Settings.Abstractions;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Settings.Rules;
using SlideGenerator.Domain.Settings.Entities;

namespace SlideGenerator.Application.Settings.Services;

public sealed class SettingManager(FileRegistry<ITextFile> textFileRegistry, ISerializer serializer)
    : ISettingProvider
{
    public string FilePath =>
        Path.Combine(AppContext.BaseDirectory, NamingRules.SettingFileName + serializer.FileExtension);

    public Setting Current { get; private set; } = new();

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

    public async Task<bool> ResetToDefaults()
    {
        Current = new Setting();
        return await Save();
    }
}
