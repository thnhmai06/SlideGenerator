using ClosedXML.Excel;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;

namespace SlideGenerator.Infrastructure.Sheet.Adapter;

public class FileSystem : IRegistry<IReadOnlyWorkbook>
{
    public IReadOnlyWorkbook Read(string filePath)
    {
        var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var xlBook = new XLWorkbook(fs);
        return new ReadOnlyWorkbook(xlBook);
    }
}