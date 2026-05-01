using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Settings.Services;

namespace SlideGenerator.Application.Services.Setting;

public interface ISettingManager : ISettingProvider
{
    Task<bool> Update(Domain.Settings.Entities.Setting newSetting);
    
    /// <summary>
    ///     Resets the <see cref="SettingManager.Current" /> configuration to its default values and saves it to disk.
    /// </summary>
    /// <returns><see langword="true" /> if the reset and save operations were successful; otherwise, <see langword="false" />.</returns>
    Task<bool> ResetToDefaults();
}