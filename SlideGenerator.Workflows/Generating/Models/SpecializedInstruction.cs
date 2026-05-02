using SlideGenerator.Sheets.Models;

namespace SlideGenerator.Workflows.Generating.Models;

public record SpecializedInstruction(string TargetShapeName, Uri? Value, EditOptions Edit, string? SourceColumn = null)
{
    public string GetDownloadPath(string rootFolder, WorkbookIdentifier workbook, string worksheetName, int rowIndex)
    {
        return rowIndex <= 0
            ? throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex))
            : Path.Combine(GetSaveFolder(rootFolder, workbook, worksheetName), "Downloaded", rowIndex.ToString());
    }

    public string GetEditPath(string rootFolder, WorkbookIdentifier workbook, string worksheetName, int rowIndex)
    {
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex));

        var fileName = $"{rowIndex}.png";
        return Path.Combine(GetSaveFolder(rootFolder, workbook, worksheetName), "Edited", fileName);
    }

    private string GetSaveFolder(string rootFolder, WorkbookIdentifier workbook, string worksheetName)
    {
        var workbookName = Path.GetFileNameWithoutExtension(workbook.FilePath);
        var workbookFullPath = Path.GetFullPath(workbook.FilePath);

        return Path.Combine(
            Path.GetFullPath(rootFolder),
            Rules.NamingRules.BuildPathSegment(workbookName, workbookFullPath, "Workbook"),
            Rules.NamingRules.BuildPathSegment(worksheetName, "Worksheet"),
            Rules.NamingRules.BuildPathSegment(SourceColumn, "Column"));
    }
}