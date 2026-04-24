using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

/// <summary>
///     Represents a specialized image instruction containing the actual URI to be used for replacement.
/// </summary>
/// <param name="Target">The target shape to replace.</param>
/// <param name="Value">The normalized URI of the image, or <see langword="null" /> if the source was invalid.</param>
/// <param name="Edit">The editing options for the image.</param>
public record SpecializedInstruction(ShapeIdentifier Target, Uri? Value, EditOptions Edit)
    : Instruction(Target, Edit)
{
    /// <summary>
    ///     Gets the physical file path where the downloaded image will be temporarily saved.
    /// </summary>
    /// <param name="rootFolder">The root directory for generation.</param>
    /// <param name="rowIndex">The 1-based index of the current row being processed.</param>
    /// <returns>The absolute path for the downloaded image.</returns>
    /// <exception cref="ArgumentException">Thrown when the row index is not greater than 0.</exception>
    public string GetDownloadPath(string rootFolder, int rowIndex)
    {
        return rowIndex <= 0
            ? throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex))
            : Path.Combine(GetSaveFolder(rootFolder), ImagePathRules.DownloadedFolder, rowIndex.ToString());
    }

    /// <summary>
    ///     Gets the physical file path where the edited image will be saved before replacement.
    /// </summary>
    /// <param name="rootFolder">The root directory for generation.</param>
    /// <param name="rowIndex">The 1-based index of the current row being processed.</param>
    /// <returns>The absolute path for the edited image.</returns>
    /// <exception cref="ArgumentException">Thrown when the row index is not greater than 0.</exception>
    public string GetEditPath(string rootFolder, int rowIndex)
    {
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex));

        var fileName = $"{rowIndex}.png";
        return Path.Combine(GetSaveFolder(rootFolder), ImagePathRules.EditedFolder, fileName);
    }

    /// <summary>
    ///     Resolves the dedicated folder for this instruction based on naming rules.
    /// </summary>
    /// <param name="rootFolder">The root directory for generation.</param>
    /// <returns>The absolute directory path.</returns>
    private string GetSaveFolder(string rootFolder)
    {
        var presentationName = Path.GetFileNameWithoutExtension(Target.Slide.Presentation.FilePath);
        var slideName = Target.Slide.Index.ToString(); // Use index as slide identifier for folder

        return Path.Combine(
            Path.GetFullPath(rootFolder),
            NamingRules.NormalizeFileName(presentationName, NamingRules.DefaultWorkbookName),
            NamingRules.NormalizeFileName(slideName, NamingRules.DefaultWorksheetName),
            Target.Id.ToString()); // Use shape ID as the final subfolder
    }
}