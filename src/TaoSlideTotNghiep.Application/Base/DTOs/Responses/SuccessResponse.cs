using TaoSlideTotNghiep.Application.Base.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Base.DTOs.Responses;

/// <summary>
/// Success Response.
/// </summary>
public abstract record SuccessResponse(RequestType RequestType) : Response(RequestType, true);