using SlideGenerator.Application.Resources.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Settings.Abstractions;
using SlideGenerator.Infrastructure.Settings.Adapters;

namespace SlideGenerator.Infrastructure.Settings.Services;

/// <summary>
///     Manages opened text files backed by the file system.
///     Concurrent reads are unrestricted (max-count = <see cref="int.MaxValue" />).
/// </summary>
/// <param name="locker">The locker used to coordinate access to files based on their paths.</param>
public sealed class TextFileRegistry(IAsyncKeyedLocker<string> locker)
    : FileRegistry<ITextFile>(locker)
{
    /// <inheritdoc />
    protected override ITextFile OpenResource(string normalizedPath, bool isEditable)
        => new StreamTextFile(normalizedPath, isEditable);
}
