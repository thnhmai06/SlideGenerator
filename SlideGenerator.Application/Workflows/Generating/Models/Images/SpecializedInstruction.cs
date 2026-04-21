using SlideGenerator.Application.Workflows.Generating.Rules;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Models.Images;

public record SpecializedInstruction(ShapeIdentifier Target, ColumnIdentifier Source, EditOptions Edit)
    : Instruction(Target, Edit)
{
    public string GetDownloadPath(string rootFolder, int rowIndex)
    {
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex));

        return Path.Combine(GetSaveFolder(rootFolder), ImagePathRules.DownloadedFolder, rowIndex.ToString());
    }

    public string GetEditPath(string rootFolder, int rowIndex)
    {
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.", nameof(rowIndex));

        var fileName = $"{rowIndex}.png";
        return Path.Combine(GetSaveFolder(rootFolder), ImagePathRules.EditedFolder, fileName);
    }

    private string GetSaveFolder(string rootFolder)
    {
        return Path.Combine(
            Path.GetFullPath(rootFolder),
            Utilities.NormalizeFileName(Source.Worksheet.Workbook.Name, NamingRules.DefaultWorkbookName),
            Utilities.NormalizeFileName(Source.Worksheet.Name, NamingRules.DefaultWorksheetName),
            Source.Name);
    }
}
