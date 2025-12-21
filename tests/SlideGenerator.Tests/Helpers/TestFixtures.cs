using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide;
using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Tests.Helpers;

internal sealed class TestSheet : ISheet
{
    private readonly List<Dictionary<string, string?>> _rows;

    public TestSheet(string name, int rowCount)
        : this(name, rowCount, [], null)
    {
    }

    public TestSheet(
        string name,
        int rowCount,
        IReadOnlyList<string?> headers,
        List<Dictionary<string, string?>>? rows)
    {
        Name = name;
        Headers = headers;
        _rows = rows ?? [];
        RowCount = rowCount;
    }

    public string Name { get; }
    public IReadOnlyList<string?> Headers { get; }
    public int RowCount { get; }

    public Dictionary<string, string?> GetRow(int rowNumber)
    {
        var index = rowNumber - 1;
        if (index < 0 || index >= _rows.Count)
            return new Dictionary<string, string?>();
        return new Dictionary<string, string?>(_rows[index]);
    }

    public List<Dictionary<string, string?>> GetAllRows()
    {
        return _rows.Select(row => new Dictionary<string, string?>(row)).ToList();
    }
}

internal sealed class TestSheetBook : ISheetBook
{
    public TestSheetBook(string filePath, params ISheet[] sheets)
    {
        FilePath = filePath;
        Name = Path.GetFileNameWithoutExtension(filePath);
        Worksheets = sheets.ToDictionary(sheet => sheet.Name, sheet => sheet);
    }

    public string FilePath { get; }
    public string? Name { get; }
    public IReadOnlyDictionary<string, ISheet> Worksheets { get; }

    public IReadOnlyDictionary<string, int> GetSheetsInfo()
    {
        return Worksheets.ToDictionary(kv => kv.Key, kv => kv.Value.RowCount);
    }

    public void Close()
    {
    }
}

internal sealed class TestTemplatePresentation : ITemplatePresentation
{
    private readonly Dictionary<uint, ImagePreview> _imageShapes;
    private readonly IReadOnlyList<ShapeInfo> _shapes;

    public TestTemplatePresentation(
        string filePath,
        int slideCount = 1,
        IReadOnlyList<ShapeInfo>? shapes = null,
        Dictionary<uint, ImagePreview>? imageShapes = null)
    {
        FilePath = filePath;
        SlideCount = slideCount;
        _shapes = shapes ?? [];
        _imageShapes = imageShapes ?? new Dictionary<uint, ImagePreview>();
    }

    public string FilePath { get; }
    public int SlideCount { get; }

    public Dictionary<uint, ImagePreview> GetAllImageShapes() => new(_imageShapes);
    public IReadOnlyList<ShapeInfo> GetAllShapes() => _shapes;
}
