using SlideGenerator.Application.Base.DTOs.Enums;

namespace SlideGenerator.Application.Base.DTOs.Responses;

/// <summary>
/// Error response.
/// </summary>
public abstract record ErrorResponse : Response
{
    public string Kind { get; init; }
    public string Message { get; init; }
    public string? StackTrace { get; init; }

    protected ErrorResponse(RequestType requestType, Exception e) : base(requestType, false)
    {
        Kind = e.GetType().Name;
        Message = e.Message;
        StackTrace = e.StackTrace;
    }
}