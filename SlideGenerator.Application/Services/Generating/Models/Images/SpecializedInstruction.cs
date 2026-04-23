using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

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
            NamingRules.NormalizeFileName(Source.Worksheet.Workbook.Name, NamingRules.DefaultWorkbookName),
            NamingRules.NormalizeFileName(Source.Worksheet.Name, NamingRules.DefaultWorksheetName),
            Source.Name);
    }
}
