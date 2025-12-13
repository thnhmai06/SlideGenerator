using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Base.DTOs.Enums;
using TaoSlideTotNghiep.Application.Base.DTOs.Responses;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Responses.Successes;

public abstract record SheetSuccess(string FilePath, SheetRequestType Type) : SuccessResponse(RequestType.Sheet),
    IFilePathBased;