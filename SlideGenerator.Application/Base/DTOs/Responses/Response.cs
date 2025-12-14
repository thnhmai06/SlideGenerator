using SlideGenerator.Application.Base.DTOs.Enums;

namespace SlideGenerator.Application.Base.DTOs.Responses;

/// <summary>
///     Base response.
/// </summary>
/// <param name="RequestType">Type of the request.</param>
/// <param name="Success">Indicates if the request was successful.</param>
public abstract record Response(RequestType RequestType, bool Success);