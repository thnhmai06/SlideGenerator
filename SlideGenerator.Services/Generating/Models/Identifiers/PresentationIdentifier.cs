namespace SlideGenerator.Services.Generating.Models.Identifiers;

/// <summary>
///     Uniquely identifies a PowerPoint presentation file.
/// </summary>
/// <param name="PresentationPath">The absolute or relative path to the presentation.</param>
/// <param name="PresentationPassword">Optional password if the presentation is encrypted.</param>
public record PresentationIdentifier(string PresentationPath, string? PresentationPassword = null)
{
    /// <summary>
    ///     Gets the normalized absolute path to the presentation.
    /// </summary>
    public string PresentationPath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = PresentationPath;
}