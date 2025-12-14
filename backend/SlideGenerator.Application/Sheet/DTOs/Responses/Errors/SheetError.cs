using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Errors;

public record SheetError : ErrorResponse, IFilePathBased
{
    public SheetError(string filePath, Exception e) : base(RequestType.Sheet, e)
    {
        FilePath = filePath;
    }

    public string FilePath { get; init; }
}