using SlideGenerator.Application.Settings.Abstractions;
using SlideGenerator.Domain.Settings.Abstractions;
using SlideGenerator.Domain.Settings.Entities;
using SlideGenerator.Domain.Settings.Rules;

namespace SlideGenerator.Application.Settings.Services;

public sealed class SettingRegistry(IRepository repository, ISerializer serializer)
    : ISettingRegistry
{
    public string FilePath =>
        Path.Combine(AppContext.BaseDirectory, NamingRules.SettingFileName + serializer.FileExtension);

    public Setting Current { get; private set; } = new();

    public bool Load()
    {
        try
        {
            var source = repository.Read(FilePath);
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
            var content = serializer.Serialize(Current);
            repository.Write(FilePath, content);
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