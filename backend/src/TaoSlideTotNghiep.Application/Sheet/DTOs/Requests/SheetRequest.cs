using TaoSlideTotNghiep.Application.Base.DTOs;
using TaoSlideTotNghiep.Application.Base.DTOs.Enums;
using TaoSlideTotNghiep.Application.Sheet.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Sheet.DTOs.Requests;

/// <summary>
/// Base sheet request.
/// </summary>
public abstract record SheetRequest(SheetRequestType Type, string FilePath)
    : Base.DTOs.Requests.Request(RequestType.Sheet),
        IFilePathBased;