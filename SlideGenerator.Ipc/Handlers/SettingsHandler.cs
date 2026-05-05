using SlideGenerator.Settings.Models;
using SlideGenerator.Settings.Services;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>settings.*</c> JSON-RPC methods: get, update, and resetToDefaults.
/// </summary>
public sealed class SettingsHandler(
    SettingManager settingManager,
    ISettingProvider settingProvider)
{
    /// <summary>
    ///     Returns the current application settings as a <see cref="Setting" />.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="Setting" /> reflecting the current configuration.</returns>
    public Task<Setting> GetAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(settingProvider.Current);
    }

    /// <summary>
    ///     Applies a settings update from the client and persists the new settings to disk.
    ///     The client must supply the complete settings object.
    /// </summary>
    /// <param name="setting">The full settings payload received from the client.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="bool" /> indicating whether the update succeeded.</returns>
    public async Task<bool> UpdateAsync(Setting setting, CancellationToken ct)
    {
        await settingManager.Update(setting).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Resets all settings to their factory defaults and persists the result to disk.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="bool" /> indicating success.</returns>
    public async Task<bool> ResetToDefaultsAsync(CancellationToken ct)
    {
        await settingManager.ResetToDefaults().ConfigureAwait(false);
        return true;
    }
}