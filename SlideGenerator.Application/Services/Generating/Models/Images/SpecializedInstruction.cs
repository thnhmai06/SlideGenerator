using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

/// <summary>
///     Represents a specialized image instruction containing the actual URI to be used for replacement.
/// </summary>
/// <param name="Target">The target shape to replace.</param>
/// <param name="Value">The normalized URI of the image, or <see langword="null" /> if the source was invalid.</param>
/// <param name="Edit">The editing options for the image.</param>
/// <param name="SourceColumn">The name of the worksheet column that provided this URL.</param>
public record SpecializedInstruction(ShapeIdentifier Target, Uri? Value, EditOptions Edit, string? SourceColumn = null)
    : Instruction(Target, Edit)
{
    /// <summary>
    ///     Gets the physical file path where the downloaded image will be temporarily saved.
    /// </summary>
    /// <param name="rootFolder">The root directory for generation.</param>
    /// <param name="worksheet">The worksheet identifier supplying workbook and sheet names for the folder hierarchy.</param>
    /// <param name="rowIndex">The 1-based index of the current row being processed.</param>
    /// <returns>The absolute path for the downloaded image (no extension — downloader appends it).</returns>
    /// <exception cref="ArgumentException">Thrown when the row index is not greater than 0.</exception>
    public string GetDownloadPath(string rootFolder, WorksheetIdentifier worksheet, int rowIndex)
    {
        return rowIndex <= 0
            ? throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex))
            : Path.Combine(GetSaveFolder(rootFolder, worksheet), ImagePathRules.DownloadedFolder, rowIndex.ToString());
    }

    /// <summary>
    ///     Gets the physical file path where the edited image will be saved before replacement.
    /// </summary>
    /// <param name="rootFolder">The root directory for generation.</param>
    /// <param name="worksheet">The worksheet identifier supplying workbook and sheet names for the folder hierarchy.</param>
    /// <param name="rowIndex">The 1-based index of the current row being processed.</param>
    /// <returns>The absolute path for the edited image (<c>.png</c> extension).</returns>
    /// <exception cref="ArgumentException">Thrown when the row index is not greater than 0.</exception>
    public string GetEditPath(string rootFolder, WorksheetIdentifier worksheet, int rowIndex)
    {
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex));

        var fileName = $"{rowIndex}.png";
        return Path.Combine(GetSaveFolder(rootFolder, worksheet), ImagePathRules.EditedFolder, fileName);
    }

    /// <summary>
    ///     Builds the shared sub-directory hierarchy:
    ///     <c>{root}/{workbook_hash7}/{worksheet_hash7}/{column_hash7}</c>.
    /// </summary>
    private string GetSaveFolder(string rootFolder, WorksheetIdentifier worksheet)
    {
        var workbookName = Path.GetFileNameWithoutExtension(worksheet.Workbook.FilePath);
        var workbookFullPath = Path.GetFullPath(worksheet.Workbook.FilePath);

        return Path.Combine(
            Path.GetFullPath(rootFolder),
            NamingRules.BuildPathSegment(workbookName, workbookFullPath, NamingRules.DefaultWorkbookName),
            NamingRules.BuildPathSegment(worksheet.Name, NamingRules.DefaultWorksheetName),
            NamingRules.BuildPathSegment(SourceColumn, NamingRules.DefaultColumnName));
    }
}