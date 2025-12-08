using TaoSlideTotNghiep.DTOs.Requests;

namespace TaoSlideTotNghiep.DTOs.Responses;

/// <summary>
/// Base response class.
/// </summary>
public abstract record Response(RequestType RequestType, bool Success);

/// <summary>
/// Error response.
/// </summary>
public record ErrorResponse : Response
{
    public string Kind { get; init; }
    public string Message { get; init; }
    public string? StackTrace { get; init; }

    public ErrorResponse(Exception exception, RequestType requestType) : base(requestType, false)
    {
        Success = false;
        RequestType = requestType;
        Kind = exception.GetType().Name;
        Message = exception.Message;
        StackTrace = exception.StackTrace;
    }
}
