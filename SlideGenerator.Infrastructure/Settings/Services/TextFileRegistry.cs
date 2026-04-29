using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Settings.Abstractions;
using SlideGenerator.Infrastructure.Settings.Adapters;

namespace SlideGenerator.Infrastructure.Settings.Services;

/// <summary>
///     Manages opened text files backed by the file system.
///     Read acquires are shared; write acquires are exclusive.
/// </summary>
/// <param name="locker">The reader-writer locker used to coordinate access to files based on their paths.</param>
public sealed class TextFileRegistry(FileLocker locker)
    : FileRegistry<ITextFile>(locker)
{
    /// <inheritdoc />
    protected override ITextFile CreateInstance(string normalizedPath, bool isEditable)
    {
        return new StreamTextFile(normalizedPath, isEditable);
    }
}