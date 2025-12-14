using SlideGenerator.Application.Base.DTOs.Enums;

namespace SlideGenerator.Application.Base.DTOs.Responses;

/// <summary>
///     Success Response.
/// </summary>
public abstract record SuccessResponse(RequestType RequestType) : Response(RequestType, true);