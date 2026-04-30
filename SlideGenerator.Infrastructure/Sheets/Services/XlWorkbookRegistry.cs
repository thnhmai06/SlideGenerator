/* LEGACY-CLOSEDXML — replaced by SfWorkbookRegistry (Syncfusion.XlsIO.NET)
using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Infrastructure.Sheets.Adapters;

namespace SlideGenerator.Infrastructure.Sheets.Services;

public sealed class XlWorkbookRegistry(FileLocker locker)
    : FileRegistry<IReadOnlyWorkbook>(locker)
{
    protected override IReadOnlyWorkbook CreateInstance(string normalizedPath, bool isEditable)
    {
        return new XlReadOnlyWorkbook(normalizedPath);
    }
}
*/
