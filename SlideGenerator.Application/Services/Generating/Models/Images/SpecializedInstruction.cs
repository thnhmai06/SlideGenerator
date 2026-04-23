using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

/// <summary>
///     Represents a specialized image instruction linking a specific target shape to a single column source.
/// </summary>
/// <param name="Target">The target shape to replace.</param>
/// <param name="Source">The specific column source providing the image URL.</param>
/// <param name="Edit">The editing options for the image.</param>
public record SpecializedInstruction(ShapeIdentifier Target, ColumnIdentifier Source, EditOptions Edit)
    : Instruction(Target, Edit)
{
    /// <summary>
    ///     Gets the physical file path where the downloaded image will be temporarily saved.
    /// </summary>
    /// <param name="rootFolder">The root directory for generation.</param>
    /// <param name="rowIndex">The 1-based index of the current row being processed.</param>
    /// <returns>The absolute path for the downloaded image.</returns>
    /// <exception cref="ArgumentException">Thrown when row index is not greater than 0.</exception>
    public string GetDownloadPath(string rootFolder, int rowIndex)
    {
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex));

        return Path.Combine(GetSaveFolder(rootFolder), ImagePathRules.DownloadedFolder, rowIndex.ToString());
    }

    /// <summary>
    ///     Gets the physical file path where the edited image will be saved before replacement.
    /// </summary>
    /// <param name="rootFolder">The root directory for generation.</param>
    /// <param name="rowIndex">The 1-based index of the current row being processed.</param>
    /// <returns>The absolute path for the edited image.</returns>
    /// <exception cref="ArgumentException">Thrown when row index is not greater than 0.</exception>
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
        return Path.Combine(
            Path.GetFullPath(rootFolder),
            NamingRules.NormalizeFileName(Source.Worksheet.Workbook.Name, NamingRules.DefaultWorkbookName),
            NamingRules.NormalizeFileName(Source.Worksheet.Name, NamingRules.DefaultWorksheetName),
            Source.Name);
    }
}