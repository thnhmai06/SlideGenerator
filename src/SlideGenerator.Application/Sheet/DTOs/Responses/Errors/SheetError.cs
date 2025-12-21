using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Errors;

/// <summary>
///     Error response for worksheet operations.
/// </summary>
public sealed record SheetError(string FilePath, string Kind, string Message)
    : Response("error")
{
    public SheetError(string filePath, Exception exception)
        : this(filePath, exception.GetType().Name, exception.Message)
    {
    }
}