namespace SlideGenerator.Domain.Settings.Abstractions;

public interface ISettingRegistry : ISettingProvider
{
    bool Load();
    bool Save();
    bool ResetToDefaults();
}