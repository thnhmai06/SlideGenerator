using Application.DTOs.Requests;

namespace Application.DTOs.Responses;

/// <summary>
/// Base response.
/// </summary>
/// <param name="RequestType">Type of the request.</param>
/// <param name="Success">Indicates if the request was successful.</param>
public abstract record Response(RequestType RequestType, bool Success);

/// <summary>
/// Success Response.
/// </summary>
public abstract record SuccessResponse(RequestType RequestType) : Response(RequestType, true);

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