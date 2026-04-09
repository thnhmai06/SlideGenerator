using SlideGenerator.Application.Common;
using SlideGenerator.Application.Settings.Abstractions;
using SlideGenerator.Infrastructure.Settings.Adapters;

namespace SlideGenerator.Infrastructure.Settings.Services;

/// <summary>
///     Manages opened text files backed by the file system.
/// </summary>
public sealed class TextFileRegistry : Registry<ITextFile>
{
    /// <summary>
    ///     Opens a text file adapter for the normalized file path.
    /// </summary>
    /// <param name="normalizedPath">The normalized text file path.</param>
    /// <param name="isEditable">A value indicating whether the file should be opened for editing.</param>
    /// <returns>A new text file adapter instance.</returns>
    protected override ITextFile OpenResource(string normalizedPath, bool isEditable)
    {
        return new StreamTextFile(normalizedPath, isEditable);
    }

    /// <summary>
    ///     Allows a cached read-only text file to be replaced when an editable lease is requested.
    /// </summary>
    /// <param name="existing">The current cached entry.</param>
    /// <param name="isEditable">A value indicating whether the caller requested editable access.</param>
    /// <returns><c>true</c> when the cached resource should be reopened in editable mode; otherwise, <c>false</c>.</returns>
    protected override bool ShouldReplace(Entry existing, bool isEditable)
    {
        return isEditable && existing.Resource is StreamTextFile { IsEditable: false };
    }
}