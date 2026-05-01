using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Modules.Settings.Abstractions;
using SlideGenerator.Infrastructure.Settings.Adapters;

namespace SlideGenerator.Infrastructure.Settings.Services;

/// <summary>
///     Manages opened text files backed by the file system.
///     Read acquires are shared; write acquires are exclusive.
/// </summary>
public sealed class TextFileRegistry : FileRegistry<ITextFile>
{
    /// <inheritdoc />
    protected override ITextFile CreateInstance(string normalizedPath, bool isEditable)
    {
        return new StreamTextFile(normalizedPath, isEditable);
    }
}