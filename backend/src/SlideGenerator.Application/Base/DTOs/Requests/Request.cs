using SlideGenerator.Application.Base.DTOs.Enums;

namespace SlideGenerator.Application.Base.DTOs.Requests;

/// <summary>
/// Base request class.
/// </summary>
public abstract record Request(RequestType RequestType);