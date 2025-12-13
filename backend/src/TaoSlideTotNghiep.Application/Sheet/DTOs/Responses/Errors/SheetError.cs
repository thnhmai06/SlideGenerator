using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Base.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Responses.Errors;

public record SheetError : Base.DTOs.Responses.ErrorResponse, IFilePathBased
{
    public string FilePath { get; init; }

    public SheetError(string filePath, Exception e) : base(RequestType.Sheet, e)
    {
        FilePath = filePath;
    }
}