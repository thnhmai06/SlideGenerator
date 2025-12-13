using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Base.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Errors;

public record SheetError : Base.DTOs.Responses.ErrorResponse, IFilePathBased
{
    public string FilePath { get; init; }

    public SheetError(string filePath, Exception e) : base(RequestType.Sheet, e)
    {
        FilePath = filePath;
    }
}