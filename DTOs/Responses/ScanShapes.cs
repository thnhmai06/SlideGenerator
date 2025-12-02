namespace presentation.DTOs.Responses;

public record ShapeData(uint Id, string Data); // Data: Base64

public record ScanShapesCreate(string Path) : Response.Create, IPathBased;

public record ScanShapesFinish(string Path, bool Success, ShapeData[]? Shapes = null) : Response.Finish(Success), IPathBased;

public record ScanShapesError : Response.Error, IPathBased
{
    public string Path { get; init; }
    public ScanShapesError(string filePath, Exception exception) : base(exception)
    {
        Path = filePath;
    }
}