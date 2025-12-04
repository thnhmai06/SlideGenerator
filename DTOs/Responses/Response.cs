using presentation.Models.Enum;

namespace presentation.DTOs.Responses;

public enum ResponseType
{
    Create,
    Control,
    Status,
    Finish,
    Error
}

public abstract record Response
{
    public ResponseType Type { get; init; }

    private Response(ResponseType type) { Type = type; }

    public abstract record Create() : Response(ResponseType.Create);

    public abstract record Control(ControlState State) : Response(ResponseType.Control);
    public abstract record Status(float Percent, string? Message = null) : Response(ResponseType.Status); // TODO: Make Status

    public abstract record Finish(bool Success) : Response(ResponseType.Finish);

    public abstract record Error(string Kind, string Message) : Response(ResponseType.Error)
    {
        protected Error(Exception exception) : this(exception.GetType().Name, exception.Message) { }
    };
};
