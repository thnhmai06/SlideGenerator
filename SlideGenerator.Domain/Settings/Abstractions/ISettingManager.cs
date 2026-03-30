using SlideGenerator.Domain.Settings.Interfaces;

namespace SlideGenerator.Domain.Settings.Abstractions;

public interface ISettingManager : ISettingProvider
{
    bool Load();
    bool Save();
    bool ResetToDefaults();
}