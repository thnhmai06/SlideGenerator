using SlideGenerator.Application.Common;
using SlideGenerator.Application.Settings.Abstractions;
using SlideGenerator.Domain.Settings.Abstractions;
using SlideGenerator.Domain.Settings.Entities;
using SlideGenerator.Domain.Settings.Rules;

namespace SlideGenerator.Application.Settings.Services;

public sealed class SettingManager(IRegistry<ITextFile> textFileRegistry, ISerializer serializer)
    : ISettingManager
{
    public string FilePath =>
        Path.Combine(AppContext.BaseDirectory, NamingRules.SettingFileName + serializer.FileExtension);

    public Setting Current { get; private set; } = new();

    public bool Load()
    {
        try
        {
            var textFile = textFileRegistry.GetOrOpen(FilePath);
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

    public bool Save()
    {
        try
        {
            var textFile = textFileRegistry.GetOrOpen(FilePath);
            var content = serializer.Serialize(Current);
            textFile.Write(content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ResetToDefaults()
    {
        Current = new Setting();
        return Save();
    }
}