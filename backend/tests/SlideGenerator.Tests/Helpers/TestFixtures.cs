using SlideGenerator.Domain.Features.Sheets.Interfaces;
using SlideGenerator.Domain.Features.Slides;
using SlideGenerator.Domain.Features.Slides.Components;

namespace SlideGenerator.Tests.Helpers;

internal sealed class TestSheet(
    string name,
    int rowCount,
    IReadOnlyList<string?> headers,
    List<Dictionary<string, string?>>? rows)
    : ISheet
{
    private readonly List<Dictionary<string, string?>> _rows = rows ?? [];

    public TestSheet(string name, int rowCount)
        : this(name, rowCount, [], null)
    {
    }

    public string Name { get; } = name;
    public IReadOnlyList<string?> Headers { get; } = headers;
    public int RowCount { get; } = rowCount;

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

internal sealed class TestSheetBook(string filePath, params ISheet[] sheets) : ISheetBook
{
    public string FilePath { get; } = filePath;
    public string? Name { get; } = Path.GetFileNameWithoutExtension(filePath);

    public IReadOnlyDictionary<string, ISheet> Worksheets { get; } =
        sheets.ToDictionary(sheet => sheet.Name, sheet => sheet);

    public IReadOnlyDictionary<string, int> GetSheetsInfo()
    {
        return Worksheets.ToDictionary(kv => kv.Key, kv => kv.Value.RowCount);
    }

    public void Dispose()
    {
    }
}

internal sealed class TestTemplatePresentation(
    string filePath,
    int slideCount = 1,
    IReadOnlyList<ShapeInfo>? shapes = null,
    Dictionary<uint, ImagePreview>? imageShapes = null,
    IReadOnlyCollection<string>? placeholders = null)
    : ITemplatePresentation
{
    private readonly Dictionary<uint, ImagePreview> _imageShapes = imageShapes ?? new Dictionary<uint, ImagePreview>();
    private readonly IReadOnlyCollection<string> _placeholders = placeholders ?? Array.Empty<string>();
    private readonly IReadOnlyList<ShapeInfo> _shapes = shapes ?? [];

    public string FilePath { get; } = filePath;
    public int SlideCount { get; } = slideCount;

    public Dictionary<uint, ImagePreview> GetAllImageShapes()
    {
        return new Dictionary<uint, ImagePreview>(_imageShapes);
    }

    public IReadOnlyList<ShapeInfo> GetAllShapes()
    {
        return _shapes;
    }

    public IReadOnlyCollection<string> GetAllTextPlaceholders()
    {
        return _placeholders;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}